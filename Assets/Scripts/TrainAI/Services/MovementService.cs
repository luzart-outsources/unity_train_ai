using System;
using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class MovementService : IMovementService
    {
        readonly Dictionary<Transform, IMovementAgent> _agents = new();
        // Quarantine list: agents that threw on Tick get suspended so a single
        // bad agent can't crash the whole movement loop frame after frame.
        readonly HashSet<Transform> _quarantined = new();
        // Reused snapshot of agent transforms so a strategy Tick that ends up
        // calling Register/Unregister mid-iteration can't trigger
        // "Collection was modified" — caused freeze spikes when an NPC reached
        // its target and a strategy reassigned itself.
        readonly List<Transform> _tickScratch = new();
        readonly ISentisRuntime _sentis;
        float _accumulator;
        // Bumped from 0.2s to 0.5s — Sentis CPU inference per agent costs
        // ~10-30ms each call on GTX 1060 Max-Q hardware. At 5Hz that was 5
        // spikes/sec interrupting player input (perceived as control lag even
        // at 60 FPS render). 2Hz halves the spike rate. NPC reaction feels
        // identical to player (they don't perceive sub-second route updates).
        const float kTickIntervalSec = 0.5f;

        public MovementService(ISentisRuntime sentis) { _sentis = sentis; }

        public void RegisterAgent(Transform npc, MovementStrategySO strategy)
        {
            if (npc == null || strategy == null) return;
            _agents[npc] = strategy.Bind(npc);
            _quarantined.Remove(npc);
        }

        public void SetTarget(Transform npc, Vector3 target)
        {
            if (npc == null || !_agents.TryGetValue(npc, out var a)) return;
            a.Target = target;
        }

        public void Unregister(Transform npc)
        {
            if (npc == null) return;
            _agents.Remove(npc);
            _quarantined.Remove(npc);
        }

        public void Tick(float dt)
        {
            // Skip Tick while the scene transition is in flight. Dispatching
            // Sentis GPU compute on NPC transforms that are about to be
            // destroyed in the unload phase has corrupted the D3D12 command
            // buffer enough to make the NVIDIA driver kill the editor mid-
            // transition (crash dump points at DispatchComputeProgram).
            if (SceneRouter.IsTransitioning) return;

            _accumulator += dt;
            if (_accumulator < kTickIntervalSec) return;
            float tickDt = _accumulator;
            _accumulator = 0f;
            object payload = _sentis != null && _sentis.IsReady ? (object)_sentis : null;

            _tickScratch.Clear();
            foreach (var k in _agents.Keys) _tickScratch.Add(k);

            for (int i = 0; i < _tickScratch.Count; i++)
            {
                var key = _tickScratch[i];
                if (key == null) continue;
                if (_quarantined.Contains(key)) continue;
                if (!_agents.TryGetValue(key, out var agent)) continue;
                try
                {
                    agent?.Tick(payload, tickDt);
                }
                catch (Exception e)
                {
                    // Log once, suspend this agent so it doesn't spam exceptions
                    // and accumulate Sentis tensor allocations (which previously
                    // crashed the editor after ~45s when Onnx agents leaked).
                    Debug.LogWarning($"[MovementService] agent '{key.name}' threw, quarantining: {e.Message}");
                    _quarantined.Add(key);
                }
            }
        }
    }
}

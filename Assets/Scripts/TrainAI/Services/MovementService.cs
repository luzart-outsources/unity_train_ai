using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class MovementService : IMovementService
    {
        readonly Dictionary<Transform, IMovementAgent> _agents = new();
        readonly ISentisRuntime _sentis;
        float _accumulator;
        const float kTickIntervalSec = 0.2f;

        public MovementService(ISentisRuntime sentis) { _sentis = sentis; }

        public void RegisterAgent(Transform npc, MovementStrategySO strategy)
        {
            if (npc == null || strategy == null) return;
            _agents[npc] = strategy.Bind(npc);
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
        }

        public void Tick(float dt)
        {
            _accumulator += dt;
            if (_accumulator < kTickIntervalSec) return;
            float tickDt = _accumulator;
            _accumulator = 0f;
            object payload = _sentis != null && _sentis.IsReady ? (object)_sentis : null;
            foreach (var kvp in _agents)
            {
                if (kvp.Key == null) continue;
                kvp.Value?.Tick(payload, tickDt);
            }
        }
    }
}

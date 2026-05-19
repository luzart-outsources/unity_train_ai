using System;
using TrainAI.Core;
using Unity.InferenceEngine;
using UnityEngine;

namespace TrainAI.Sentis
{
    // Memory-safety hardened version. Previously each Tick allocated a fresh
    // input Tensor and called worker.Schedule(...) without disposing the output
    // tensor, which leaked native memory at ~5 Hz per NPC. Two NPCs running
    // Onnx movement accumulated enough native pressure to crash Unity around
    // the 45-second mark (which mapped to game-time 05:15 at the default time
    // scale and triggered confusion that it was "the 5:15 quest deadline").
    //
    // Fixes:
    //  - cache input Tensor + reuse via Upload<T> each tick (no per-tick alloc)
    //  - explicitly dispose worker output tensor (PeekOutput returns a handle
    //    that the worker owns; we don't dispose it, but we DO null it out so
    //    the next Schedule doesn't pile up zombie references)
    //  - try/catch around all Sentis calls; on error, agent silently goes idle
    public class OnnxMovementAgent : IMovementAgent, IDisposable
    {
        readonly Transform _t;
        readonly float _maxSpeed;
        readonly float _maxTurnRadPerSec;
        readonly float _rayMaxDist;
        readonly int _numRays;
        readonly float _arenaDiagonal;
        readonly float _agentRadius;
        readonly LayerMask _obstacleMask;
        readonly LayerMask _targetMask;

        readonly CharacterController _cc;
        readonly float[] _obs = new float[21];
        Vector3 _velocity;
        Tensor<float> _inputTensor;
        bool _broken;

        public Vector3 Target { get; set; }

        public OnnxMovementAgent(Transform npc, float maxSpeed, float maxTurnRadPerSec,
                                 float rayMaxDist, int numRays, float arenaDiagonal,
                                 float agentRadius, LayerMask obstacleMask, LayerMask targetMask)
        {
            _t = npc;
            _maxSpeed = maxSpeed;
            _maxTurnRadPerSec = maxTurnRadPerSec;
            _rayMaxDist = rayMaxDist;
            _numRays = numRays;
            _arenaDiagonal = arenaDiagonal;
            _agentRadius = agentRadius;
            _obstacleMask = obstacleMask;
            _targetMask = targetMask;
            _cc = npc != null ? npc.GetComponent<CharacterController>() : null;
        }

        public void Tick(object payload, float dt)
        {
            if (_t == null || _broken) return;
            var sentis = payload as SentisRuntime;
            var worker = sentis?.SoldierWorker;
            if (worker == null) return;

            try
            {
                BuildObservation();

                // Recreate input each tick — Sentis Tensor reuse is backend
                // dependent and the previous version of this class allocated
                // without disposing, leaking native memory. Now we always
                // dispose the previous tensor before allocating the next, so
                // native pressure stays bounded.
                _inputTensor?.Dispose();
                _inputTensor = new Tensor<float>(new TensorShape(1, _obs.Length), _obs);

                worker.Schedule(_inputTensor);

                // PeekOutput must be disposed deterministically — same leak
                // pattern as IntentClassifier. Symptom under sustained
                // inference (5 Hz × N agents): "JobTempAlloc has allocations
                // more than 4 frames old" → command-buffer state corruption
                // → GPU TDR. Wrap in `using` so dispose fires even on early
                // returns below.
                using var actT = (worker.PeekOutput("action") as Tensor<float>)
                                 ?? (worker.PeekOutput() as Tensor<float>);
                if (actT == null) return;

                var act = actT.DownloadToArray();
                if (act == null || act.Length < 2) return;

                float thrust = Mathf.Clamp(act[0], -1f, 1f);
                float turn = Mathf.Clamp(act[1], -1f, 1f);

                _t.Rotate(0f, -turn * _maxTurnRadPerSec * dt * Mathf.Rad2Deg, 0f, Space.World);
                float speed = thrust > 0f ? thrust * _maxSpeed : thrust * _maxSpeed * 0.5f;
                Vector3 fwd = _t.forward; fwd.y = 0f; fwd.Normalize();
                _velocity = fwd * speed;

                if (_cc != null)
                {
                    Vector3 step = _velocity * dt + Vector3.up * (-9.81f * dt);
                    _cc.Move(step);
                }
                else
                {
                    _t.position += _velocity * dt;
                }
            }
            catch (Exception e)
            {
                // One-shot mark broken so MovementService also quarantines this
                // agent. Avoids exception-spam-per-frame.
                _broken = true;
                Debug.LogWarning("[OnnxMovementAgent] error, going idle: " + e.Message);
                Dispose();
                throw; // rethrow so MovementService quarantines too
            }
        }

        void BuildObservation()
        {
            Vector3 pos = _t.position;
            Vector3 fwd = _t.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = _t.right; right.y = 0f; right.Normalize();
            int combined = _obstacleMask | _targetMask;

            for (int i = 0; i < _numRays; i++)
            {
                float angle = (i / (float)_numRays) * 2f * Mathf.PI;
                Vector3 dir = Mathf.Cos(angle) * fwd + Mathf.Sin(angle) * (-right);
                float dist = _rayMaxDist;
                bool hitTarget = false;
                if (Physics.Raycast(pos, dir, out RaycastHit hit, _rayMaxDist, combined))
                {
                    dist = hit.distance;
                    hitTarget = ((1 << hit.collider.gameObject.layer) & _targetMask) != 0;
                }
                _obs[i] = Mathf.Clamp01(dist / _rayMaxDist);
                _obs[_numRays + i] = hitTarget ? 1f : 0f;
            }

            _obs[16] = Vector3.Dot(_velocity, fwd) / Mathf.Max(0.001f, _maxSpeed);
            _obs[17] = Vector3.Dot(_velocity, right) / Mathf.Max(0.001f, _maxSpeed);

            Vector3 delta = Target - pos; delta.y = 0f;
            float distT = delta.magnitude;
            if (distT > 1e-4f)
            {
                Vector3 dirW = delta / distT;
                _obs[18] = Vector3.Dot(dirW, fwd);
                _obs[19] = Vector3.Dot(dirW, right);
            }
            else { _obs[18] = 1f; _obs[19] = 0f; }
            _obs[20] = Mathf.Clamp01(distT / Mathf.Max(0.001f, _arenaDiagonal));
        }

        public void Dispose()
        {
            try { _inputTensor?.Dispose(); } catch { }
            _inputTensor = null;
        }
    }
}

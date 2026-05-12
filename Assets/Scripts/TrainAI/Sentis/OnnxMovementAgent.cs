using TrainAI.Core;
using Unity.InferenceEngine;
using UnityEngine;

namespace TrainAI.Sentis
{
    public class OnnxMovementAgent : IMovementAgent
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
            if (_t == null) return;
            var sentis = payload as SentisRuntime;
            var worker = sentis?.SoldierWorker;
            if (worker == null) return;

            BuildObservation();
            using var input = new Tensor<float>(new TensorShape(1, _obs.Length), _obs);
            worker.Schedule(input);

            var actT = worker.PeekOutput("action") as Tensor<float>;
            if (actT == null) actT = worker.PeekOutput() as Tensor<float>;
            if (actT == null) return;

            var act = actT.DownloadToArray();
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
    }
}

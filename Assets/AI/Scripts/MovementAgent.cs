
using System;
using UnityEngine;
using Unity.InferenceEngine;

public class MovementAgent : MonoBehaviour
{
    [Header("Model")]
    public ModelAsset modelAsset;
    public BackendType backend = BackendType.GPUCompute;

    [Header("Scene refs")]
    public Transform target;
    public LayerMask obstacleLayer;          // assign "Obstacle" layer in Inspector
    public LayerMask targetLayer;            // assign "Target" layer

    [Header("Tuning — must match Python training")]
    public float maxSpeed = 3.5f;
    public float maxTurnRadPerSec = Mathf.PI;
    public float rayMaxDist = 10f;
    public int numRays = 8;
    public float arenaDiagonal = 35.36f;     // 25 * sqrt(2) — match arena_max in v3 random env training
    public float agentRadius = 0.5f;

    [Header("Stepping")]
    [Tooltip("Match the Python env dt (0.1s = 10 Hz). Inference fires this often.")]
    public float decisionDt = 0.1f;

    private Model _model;
    private Worker _worker;
    private float _accum = 0f;
    private Vector3 _velocity;               // world velocity for obs purposes
    private float[] _obsBuf = new float[21];

    void Awake()
    {
        if (modelAsset == null) { Debug.LogError("MovementAgent: missing model"); enabled = false; return; }
        _model = ModelLoader.Load(modelAsset);
        _worker = new Worker(_model, backend);
        Debug.Log("[MovementAgent] model loaded");
    }

    void OnDestroy()
    {
        _worker?.Dispose();
    }

    void FixedUpdate()
    {
        _accum += Time.fixedDeltaTime;
        if (_accum < decisionDt) return;
        _accum = 0f;
        StepOnce(decisionDt);
    }

    void StepOnce(float dt)
    {
        // 1) Build observation
        ComputeObservation(_obsBuf);

        // 2) Run inference
        using var input = new Tensor<float>(new TensorShape(1, _obsBuf.Length), _obsBuf);
        _worker.Schedule(input);
        var actT = _worker.PeekOutput("action") as Tensor<float>;
        var act = actT.DownloadToArray();         // [thrust, turn]
        float thrust = Mathf.Clamp(act[0], -1f, 1f);
        float turn = Mathf.Clamp(act[1], -1f, 1f);

        transform.Rotate(0f, -turn * maxTurnRadPerSec * dt * Mathf.Rad2Deg, 0f, Space.World);
        float speed = thrust > 0 ? thrust * maxSpeed : thrust * maxSpeed * 0.5f;
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        _velocity = fwd * speed;
        Vector3 next = transform.position + _velocity * dt;

        Collider[] hits = Physics.OverlapSphere(next, agentRadius, obstacleLayer);
        if (hits != null && hits.Length > 0)
        {
            // Slide ra canh gan nhat cua obstacle dau tien
            var ob = hits[0];
            Vector3 obCenter = ob.bounds.center;
            float halfX = ob.bounds.extents.x;
            float halfZ = ob.bounds.extents.z;
            float ox = next.x - obCenter.x;
            float oz = next.z - obCenter.z;
            if (Mathf.Abs(ox) > Mathf.Abs(oz))
                next.x = obCenter.x + Mathf.Sign(ox) * (halfX + agentRadius + 0.01f);
            else
                next.z = obCenter.z + Mathf.Sign(oz) * (halfZ + agentRadius + 0.01f);
        }
        transform.position = next;
    }

    void ComputeObservation(float[] outBuf)
    {
        Vector3 pos = transform.position;
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = transform.right; right.y = 0f; right.Normalize();

        // 8 rays, evenly spaced 360°, starting from forward, going CCW
        for (int i = 0; i < numRays; i++)
        {
            float angle = (i / (float)numRays) * 2f * Mathf.PI;
            // Counter-clockwise from forward in XZ (Unity Y-up)
            Vector3 dir = Mathf.Cos(angle) * fwd + Mathf.Sin(angle) * (-right);
            // (Note: -right makes positive sin = LEFT in Unity, matching numpy CCW.)

            float dist = rayMaxDist;
            bool hitTarget = false;
            // Cast against obstacles + target combined; choose closest
            int combined = obstacleLayer | targetLayer;
            if (Physics.Raycast(pos, dir, out RaycastHit hit, rayMaxDist, combined))
            {
                dist = hit.distance;
                hitTarget = ((1 << hit.collider.gameObject.layer) & targetLayer) != 0;
            }
            outBuf[i] = Mathf.Clamp01(dist / rayMaxDist);
            outBuf[numRays + i] = hitTarget ? 1f : 0f;
        }

        // Velocity in agent frame
        outBuf[16] = Vector3.Dot(_velocity, fwd) / maxSpeed;
        outBuf[17] = Vector3.Dot(_velocity, right) / maxSpeed;

        // Direction-to-target in agent frame
        Vector3 delta = target.position - pos; delta.y = 0f;
        float distT = delta.magnitude;
        if (distT > 1e-4f)
        {
            Vector3 dirW = delta / distT;
            outBuf[18] = Vector3.Dot(dirW, fwd);
            outBuf[19] = Vector3.Dot(dirW, right);
        }
        else
        {
            outBuf[18] = 1f; outBuf[19] = 0f;
        }
        outBuf[20] = Mathf.Clamp01(distT / arenaDiagonal);
    }
}

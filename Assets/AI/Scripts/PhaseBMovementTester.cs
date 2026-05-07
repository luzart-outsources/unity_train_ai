// PhaseBMovementTester.cs
//
// Test Phase B isolated: tự build 3D scene (plane + agent + target + obstacles)
// và run 1 episode movement. Có UI nhỏ hiển thị status realtime + nút "Reset" để
// test multiple episodes liên tục.

using System.Collections;
using UnityEngine;
using Unity.InferenceEngine;

public class PhaseBMovementTester : MonoBehaviour
{
    [Header("Phase B asset")]
    public ModelAsset movementModel;

    [Header("Episode config")]
    public float timeoutSeconds = 60f;
    public int numObstacles = 6;

    private GameObject _agent;
    private GameObject _target;
    private MovementAgent _moveAgent;
    private float _episodeStart;
    private bool _episodeRunning;
    private string _statusText = "Building scene...";
    private int _episodeCount = 0;
    private int _successCount = 0;

    void Start()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log(" PHASE B — Movement Test (auto-build + run)");
        Debug.Log("══════════════════════════════════════════════");

        if (movementModel == null)
        {
            Debug.LogError("[PhaseB] movementModel missing — assign trong Inspector");
            _statusText = "❌ Model thiếu";
            enabled = false;
            return;
        }

        BuildScene();
        StartEpisode();
    }

    void BuildScene()
    {
        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3, 1, 3);
        floor.GetComponent<Renderer>().material.color = new Color(0.7f, 0.7f, 0.7f);

        // Target — đỏ
        _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _target.name = "Target";
        _target.tag = "Target";
        int targetLayerId = LayerMask.NameToLayer("Target");
        if (targetLayerId >= 0) _target.layer = targetLayerId;
        _target.transform.position = new Vector3(8, 0.5f, 8);
        _target.GetComponent<Renderer>().material.color = Color.red;

        // 6 obstacles — nâu
        int obstacleLayerId = LayerMask.NameToLayer("Obstacle");
        var rng = new System.Random(42);
        for (int i = 0; i < numObstacles; i++)
        {
            var ob = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ob.name = $"Obstacle_{i}";
            ob.tag = "Obstacle";
            if (obstacleLayerId >= 0) ob.layer = obstacleLayerId;
            float x = (float)(rng.NextDouble() * 12 - 6);
            float z = (float)(rng.NextDouble() * 12 - 6);
            ob.transform.position = new Vector3(x, 0.5f, z);
            ob.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            ob.GetComponent<Renderer>().material.color = new Color(0.3f, 0.2f, 0.1f);
        }

        // Agent — xanh, SetActive(false) trước AddComponent
        _agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _agent.name = "Agent";
        _agent.SetActive(false);
        _agent.transform.position = new Vector3(-8, 0.5f, -8);
        _agent.GetComponent<Renderer>().material.color = Color.blue;
        var col = _agent.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        _moveAgent = _agent.AddComponent<MovementAgent>();
        _moveAgent.modelAsset = movementModel;
        _moveAgent.target = _target.transform;
        _moveAgent.backend = BackendType.CPU;
        if (obstacleLayerId >= 0) _moveAgent.obstacleLayer = 1 << obstacleLayerId;
        if (targetLayerId >= 0) _moveAgent.targetLayer = 1 << targetLayerId;

        _agent.SetActive(true);  // bây giờ Awake() chạy

        // Camera góc nhìn từ trên xuống
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 25, -15);
            Camera.main.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        Debug.Log($"[PhaseB] Scene built: 1 plane, 1 agent (xanh), 1 target (đỏ), {numObstacles} obstacles (nâu)");
    }

    void StartEpisode()
    {
        _episodeStart = Time.time;
        _episodeRunning = true;
        _episodeCount++;
        _statusText = $"Episode #{_episodeCount} đang chạy...";
        Debug.Log($"[PhaseB] Episode #{_episodeCount} START");
    }

    void Update()
    {
        if (!_episodeRunning) return;

        float elapsed = Time.time - _episodeStart;
        var delta = _target.transform.position - _agent.transform.position;
        delta.y = 0;
        float dist = delta.magnitude;

        _statusText = $"Episode #{_episodeCount} | t={elapsed:F1}s | dist={dist:F2} | success {_successCount}/{_episodeCount-1}";

        // Reach target
        if (dist < 1.2f)
        {
            _episodeRunning = false;
            _successCount++;
            _statusText = $"✅ REACHED sau {elapsed:F1}s, dist={dist:F2}";
            Debug.Log($"[PhaseB] Episode #{_episodeCount} ✅ REACHED at {elapsed:F1}s");
            return;
        }

        // Timeout
        if (elapsed >= timeoutSeconds)
        {
            _episodeRunning = false;
            _statusText = $"⚠️ TIMEOUT sau {elapsed:F1}s, dist={dist:F2}";
            Debug.LogWarning($"[PhaseB] Episode #{_episodeCount} ⚠️ TIMEOUT");
        }
    }

    void ResetEpisode()
    {
        if (_agent == null || _target == null) return;
        var rng = new System.Random(System.DateTime.Now.Millisecond);
        _agent.transform.position = new Vector3(
            (float)(rng.NextDouble() * 4 - 10), 0.5f,
            (float)(rng.NextDouble() * 4 - 10));
        _agent.transform.rotation = Quaternion.identity;
        _target.transform.position = new Vector3(
            (float)(rng.NextDouble() * 4 + 6), 0.5f,
            (float)(rng.NextDouble() * 4 + 6));
        StartEpisode();
    }

    void OnGUI()
    {
        var skin = GUI.skin;
        skin.label.fontSize = 16;
        skin.button.fontSize = 16;

        GUILayout.BeginArea(new Rect(20, 20, 600, 120), GUI.skin.box);
        GUILayout.Label("<b>Phase B — Movement Test</b>",
                        new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20 });
        GUILayout.Label(_statusText);
        if (GUILayout.Button(_episodeRunning ? "Stop" : "Reset & Run again",
                             GUILayout.Width(240), GUILayout.Height(35)))
        {
            if (_episodeRunning) _episodeRunning = false;
            else ResetEpisode();
        }
        GUILayout.EndArea();
    }
}

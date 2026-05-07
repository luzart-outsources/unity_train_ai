
using UnityEngine;

public class PhaseBMovementTester : MonoBehaviour
{
    [Header("Scene refs (set by editor builder)")]
    public GameObject agent;
    public GameObject target;
    public Vector3 agentStartPos = new Vector3(-8, 0.5f, -8);
    public Quaternion agentStartRot = Quaternion.identity;

    [Header("Episode config")]
    public float timeoutSeconds = 60f;
    public float reachThreshold = 1.2f;  // agent radius 0.5 + target 0.7

    private float _episodeStart;
    private bool _running;
    private string _status = "Ready — click Play đã rồi";
    private int _episodeCount = 0;
    private int _successCount = 0;

    void Start()
    {
        Debug.Log("");
        Debug.Log("PHASE B — Movement Test (scene đã pre-build)");
        Debug.Log("");
        if (agent == null || target == null)
        {
            Debug.LogError("[PhaseB] agent/target chưa assign. Rebuild scene qua menu AI.");
            _status = "fail Scene không có agent/target. Rebuild scene.";
            return;
        }
        StartEpisode();
    }

    void StartEpisode()
    {
        _episodeStart = Time.time;
        _running = true;
        _episodeCount++;
        _status = $"Episode #{_episodeCount} đang chạy...";
        Debug.Log($"[PhaseB] Episode #{_episodeCount} START");
    }

    void Update()
    {
        if (!_running || agent == null || target == null) return;

        float elapsed = Time.time - _episodeStart;
        var delta = target.transform.position - agent.transform.position;
        delta.y = 0;
        float dist = delta.magnitude;

        _status = $"Ep #{_episodeCount} | t={elapsed:F1}s | dist={dist:F2} | success {_successCount}/{_episodeCount-1}";

        if (dist < reachThreshold)
        {
            _running = false;
            _successCount++;
            _status = $"pass REACHED sau {elapsed:F1}s, dist={dist:F2}";
            Debug.Log($"[PhaseB] Episode #{_episodeCount} pass REACHED at {elapsed:F1}s");
            return;
        }

        if (elapsed >= timeoutSeconds)
        {
            _running = false;
            _status = $"warn TIMEOUT sau {elapsed:F1}s, dist={dist:F2}";
            Debug.LogWarning($"[PhaseB] Episode #{_episodeCount} warn TIMEOUT");
        }
    }

    void ResetEpisode()
    {
        if (agent == null) return;
        agent.transform.position = agentStartPos;
        agent.transform.rotation = agentStartRot;
        StartEpisode();
    }

    void OnGUI()
    {
        var skin = GUI.skin;
        skin.label.fontSize = 16;
        skin.button.fontSize = 16;

        GUILayout.BeginArea(new Rect(20, 20, 600, 130), GUI.skin.box);
        GUILayout.Label("<b>Phase B — Movement Test</b>",
                        new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20 });
        GUILayout.Label(_status);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(_running ? "Stop" : "Reset & Run again",
                             GUILayout.Width(220), GUILayout.Height(35)))
        {
            if (_running) _running = false;
            else ResetEpisode();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}

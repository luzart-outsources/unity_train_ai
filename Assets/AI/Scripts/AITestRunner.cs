
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;

public class AITestRunner : MonoBehaviour
{
    [Header("Phase A — Intent Classifier (auto-loaded)")]
    public ModelAsset intentModel;
    public TextAsset intentMeta;
    public TextAsset responsesJson;

    [Header("Phase B — Movement (auto-loaded)")]
    public ModelAsset movementModel;

    [Header("Test config")]
    public bool runPhaseATest = true;
    public bool runPhaseBTest = true;
    public float phaseBTimeout = 60f;

    private NPCDialogueBrain _brain;
    private GameObject _agent;
    private GameObject _target;
    private MovementAgent _moveAgent;

    // Phase A test sentences voi expected intent
    private static readonly (string text, string expected)[] PhaseATests = new[]
    {
        ("Mấy giờ thì ăn cơm",       "HOI_GIO_AN"),
        ("Phòng học ở đâu vậy",      "HOI_VI_TRI"),
        ("Em xin phép về quê",       "XIN_PHEP"),
        ("Wifi yếu quá",              "OUT_OF_SCOPE"),
        ("Súng AK47 dùng thế nào",   "HOI_KIEN_THUC"),
    };

    void Start()
    {
        Debug.Log("");
        Debug.Log("AI TEST RUNNER — auto test cả 2 phase");
        Debug.Log("");
        StartCoroutine(RunAllTests());
    }

    IEnumerator RunAllTests()
    {
        if (runPhaseATest)
        {
            yield return RunPhaseATest();
        }

        if (runPhaseBTest)
        {
            BuildPhaseBScene();
            yield return RunPhaseBTest();
        }

        Debug.Log("");
        Debug.Log("ALL TESTS DONE — đọc Console phía trên");
        Debug.Log("");
    }

    IEnumerator RunPhaseATest()
    {
        Debug.Log("PHASE A: Intent Classifier (5 câu test) -");
        if (intentModel == null || intentMeta == null || responsesJson == null)
        {
            Debug.LogError("| fail Phase A assets thiếu — check Inspector của AITestRunner");
            Debug.Log("");
            yield break;
        }

        var commander = new GameObject("Commander");
        commander.SetActive(false);
        _brain = commander.AddComponent<NPCDialogueBrain>();
        _brain.modelAsset = intentModel;
        _brain.metaJson = intentMeta;
        _brain.responsesJson = responsesJson;
        _brain.backend = BackendType.CPU;  // CPU stable cho test
        _brain.minConfidence = 0.40f;
        commander.SetActive(true);  // bay gio Awake() chay voi fields day du

        yield return null;  // doi 1 frame cho Awake hoan tat

        int correct = 0;
        for (int i = 0; i < PhaseATests.Length; i++)
        {
            var (text, expected) = PhaseATests[i];
            var (intent, conf) = _brain.Classify(text);
            bool ok = intent == expected;
            if (ok) correct++;
            string mark = ok ? "ok" : "fail";
            Debug.Log($"| {mark} \"{text}\" -> {intent} ({conf*100:F1}%, expect {expected})");
        }
        float acc = (float)correct / PhaseATests.Length;
        Debug.Log($"| -> Score: {correct}/{PhaseATests.Length} = {acc*100:F0}%");
        if (acc >= 0.8f)
            Debug.Log($"| pass PASS — Phase A model OK");
        else
            Debug.LogWarning($"| warn FAIL — accuracy thấp, check tokenization");
        Debug.Log("");
    }

    void BuildPhaseBScene()
    {
        Debug.Log("PHASE B: Movement (build scene + 1 episode) -");

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = new Vector3(0, 0, 0);
        floor.transform.localScale = new Vector3(3, 1, 3);
        var floorMat = floor.GetComponent<Renderer>().material;
        floorMat.color = new Color(0.7f, 0.7f, 0.7f);

        // Target — mau do
        _target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _target.name = "Target";
        _target.tag = "Target";   // tag must be defined — Editor script se tao
        int targetLayerId = LayerMask.NameToLayer("Target");
        if (targetLayerId >= 0) _target.layer = targetLayerId;
        _target.transform.position = new Vector3(8, 0.5f, 8);
        _target.GetComponent<Renderer>().material.color = Color.red;

        // Agent — mau xanh, SetActive(false) truoc de Awake khong fail
        _agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _agent.name = "Agent";
        _agent.SetActive(false);  // ngan Awake chay truoc khi assign properties
        _agent.transform.position = new Vector3(-8, 0.5f, -8);
        _agent.GetComponent<Renderer>().material.color = Color.blue;
        // Remove collider — agent la kinematic, khong can collide
        var col = _agent.GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;

        // Obstacles — 6 cube ngau nhien
        int obstacleLayerId = LayerMask.NameToLayer("Obstacle");
        var rng = new System.Random(42);
        for (int i = 0; i < 6; i++)
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

        // Add MovementAgent component (agent dang inactive nen Awake chua chay)
        _moveAgent = _agent.AddComponent<MovementAgent>();
        _moveAgent.modelAsset = movementModel;
        _moveAgent.target = _target.transform;
        _moveAgent.backend = BackendType.CPU;
        if (obstacleLayerId >= 0) _moveAgent.obstacleLayer = 1 << obstacleLayerId;
        if (targetLayerId >= 0) _moveAgent.targetLayer = 1 << targetLayerId;

        // Bay gio activate -> Awake() se chay voi fields day du
        _agent.SetActive(true);

        // Camera — dat o goc tren nhin xuong
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 25, -15);
            Camera.main.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        Debug.Log("| Scene built: 1 plane, 1 agent, 1 target, 6 obstacles");
    }

    IEnumerator RunPhaseBTest()
    {
        if (movementModel == null)
        {
            Debug.LogError("| fail Phase B model thiếu");
            Debug.Log("");
            yield break;
        }

        float startTime = Time.time;
        float lastDist = float.MaxValue;
        int ticksSinceImprove = 0;

        while (Time.time - startTime < phaseBTimeout)
        {
            yield return new WaitForSeconds(0.5f);
            if (_agent == null || _target == null) yield break;
            var delta = _target.transform.position - _agent.transform.position;
            delta.y = 0;
            float dist = delta.magnitude;

            if (dist < 1.2f)  // agent radius 0.5 + target 0.7
            {
                float elapsed = Time.time - startTime;
                Debug.Log($"| pass REACHED target sau {elapsed:F1}s, dist={dist:F2}");
                Debug.Log("");
                yield break;
            }

            if (dist < lastDist - 0.1f)
            {
                lastDist = dist;
                ticksSinceImprove = 0;
            }
            else
            {
                ticksSinceImprove++;
            }

            // Periodic progress
            if (Mathf.FloorToInt(Time.time - startTime) % 5 == 0 && ticksSinceImprove == 0)
                Debug.Log($"|   ... {Time.time-startTime:F0}s, dist={dist:F2}");
        }

        Debug.LogWarning($"| warn TIMEOUT sau {phaseBTimeout}s, agent không đến đích");
        Debug.Log("");
    }
}

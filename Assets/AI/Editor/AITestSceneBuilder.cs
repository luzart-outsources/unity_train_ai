// AITestSceneBuilder.cs — pre-build scenes với GameObjects sẵn trong Hierarchy
//
// Click menu → scene mở ra với mọi thứ visible trong Editor (chưa Play).
// User có thể inspect, di chuyển obstacle, đổi position trước khi Play.
//
// Menu items:
//   AI / 1. Phase A — Chat Test       → scene với Commander (brain + chat UI)
//   AI / 2. Phase B — Movement Test    → scene với Floor + Agent + Target + 6 Obstacles + Camera
//   AI / 3. Both — Combined            → scene với cả 2

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.InferenceEngine;

public static class AITestSceneBuilder
{
    [MenuItem("AI/1. Phase A — Chat Test", false, 100)]
    public static void BuildPhaseA()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var assets = LoadAssets();
        if (assets.intentModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Commander GameObject — both brain + chat UI on same GO
        var commander = new GameObject("Commander");
        var brain = commander.AddComponent<NPCDialogueBrain>();
        brain.modelAsset = assets.intentModel;
        brain.metaJson = assets.intentMeta;
        brain.responsesJson = assets.responsesJson;
        brain.backend = BackendType.CPU;
        brain.minConfidence = 0.40f;

        commander.AddComponent<PhaseAChatTester>();

        // Camera nhỏ phía sau (không quan trọng vì UI là IMGUI fullscreen)
        var cam = GameObject.Find("Main Camera");
        if (cam != null) cam.transform.position = new Vector3(0, 1, -10);

        SaveAndOpen(scene, "Assets/Scenes/PhaseA_ChatTest.unity");
        Debug.Log("[AISetup] Phase A scene built. Click Play để chạy.");
    }

    [MenuItem("AI/2. Phase B — Movement Test", false, 101)]
    public static void BuildPhaseB()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var assets = LoadAssets();
        if (assets.movementModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        int obstacleLayerId = LayerMask.NameToLayer("Obstacle");
        int targetLayerId = LayerMask.NameToLayer("Target");

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(3, 1, 3);
        SetColor(floor, new Color(0.7f, 0.7f, 0.7f));

        // Target — đỏ
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "Target";
        target.tag = "Target";
        if (targetLayerId >= 0) target.layer = targetLayerId;
        target.transform.position = new Vector3(8, 0.5f, 8);
        SetColor(target, Color.red);

        // 6 Obstacles — nâu
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
            SetColor(ob, new Color(0.4f, 0.25f, 0.1f));
        }

        // Agent — xanh, gắn MovementAgent (assets pre-assigned)
        var agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agent.name = "Agent";
        agent.transform.position = new Vector3(-8, 0.5f, -8);
        SetColor(agent, Color.blue);
        // Disable collider trigger để không bị physics
        var agentCol = agent.GetComponent<BoxCollider>();
        if (agentCol != null) agentCol.isTrigger = true;

        var moveAgent = agent.AddComponent<MovementAgent>();
        moveAgent.modelAsset = assets.movementModel;
        moveAgent.target = target.transform;
        moveAgent.backend = BackendType.CPU;
        if (obstacleLayerId >= 0) moveAgent.obstacleLayer = 1 << obstacleLayerId;
        if (targetLayerId >= 0) moveAgent.targetLayer = 1 << targetLayerId;

        // Camera — đặt góc trên xuống
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 25, -15);
            cam.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        // Status monitor
        var monitor = new GameObject("StatusMonitor");
        var mon = monitor.AddComponent<PhaseBMovementTester>();
        mon.agent = agent;
        mon.target = target;
        mon.agentStartPos = agent.transform.position;
        mon.agentStartRot = agent.transform.rotation;

        SaveAndOpen(scene, "Assets/Scenes/PhaseB_MovementTest.unity");
        Debug.Log($"[AISetup] Phase B scene built: 1 plane, 1 agent, 1 target, 6 obstacles. Hierarchy hiện đầy đủ. Click Play.");
    }

    [MenuItem("AI/3. Both — Combined", false, 102)]
    public static void BuildBoth()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var assets = LoadAssets();
        if (assets.intentModel == null || assets.movementModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Phase A: Commander
        var commander = new GameObject("Commander");
        var brain = commander.AddComponent<NPCDialogueBrain>();
        brain.modelAsset = assets.intentModel;
        brain.metaJson = assets.intentMeta;
        brain.responsesJson = assets.responsesJson;
        brain.backend = BackendType.CPU;
        commander.AddComponent<PhaseAChatTester>();

        // Phase B: scene 3D (như BuildPhaseB nhưng smaller arena để 2 system coexist)
        int obstacleLayerId = LayerMask.NameToLayer("Obstacle");
        int targetLayerId = LayerMask.NameToLayer("Target");

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(3, 1, 3);
        SetColor(floor, new Color(0.7f, 0.7f, 0.7f));

        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "Target";
        target.tag = "Target";
        if (targetLayerId >= 0) target.layer = targetLayerId;
        target.transform.position = new Vector3(8, 0.5f, 8);
        SetColor(target, Color.red);

        var rng = new System.Random(42);
        for (int i = 0; i < 6; i++)
        {
            var ob = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ob.name = $"Obstacle_{i}";
            ob.tag = "Obstacle";
            if (obstacleLayerId >= 0) ob.layer = obstacleLayerId;
            ob.transform.position = new Vector3(
                (float)(rng.NextDouble() * 12 - 6), 0.5f,
                (float)(rng.NextDouble() * 12 - 6));
            ob.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            SetColor(ob, new Color(0.4f, 0.25f, 0.1f));
        }

        var agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agent.name = "Agent";
        agent.transform.position = new Vector3(-8, 0.5f, -8);
        SetColor(agent, Color.blue);
        var agentCol = agent.GetComponent<BoxCollider>();
        if (agentCol != null) agentCol.isTrigger = true;

        var moveAgent = agent.AddComponent<MovementAgent>();
        moveAgent.modelAsset = assets.movementModel;
        moveAgent.target = target.transform;
        moveAgent.backend = BackendType.CPU;
        if (obstacleLayerId >= 0) moveAgent.obstacleLayer = 1 << obstacleLayerId;
        if (targetLayerId >= 0) moveAgent.targetLayer = 1 << targetLayerId;

        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 25, -15);
            cam.transform.rotation = Quaternion.Euler(60, 0, 0);
        }

        var monitor = new GameObject("StatusMonitor");
        var mon = monitor.AddComponent<PhaseBMovementTester>();
        mon.agent = agent;
        mon.target = target;
        mon.agentStartPos = agent.transform.position;
        mon.agentStartRot = agent.transform.rotation;

        SaveAndOpen(scene, "Assets/Scenes/AITest_Combined.unity");
        Debug.Log("[AISetup] Combined scene built. Click Play.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────
    static bool CheckNotPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[AISetup] Đang Play — Stop trước khi build scene mới");
            return false;
        }
        return true;
    }

    struct AssetBundle
    {
        public ModelAsset intentModel, movementModel;
        public TextAsset intentMeta, responsesJson;
    }

    static AssetBundle LoadAssets()
    {
        var b = new AssetBundle
        {
            intentModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier.onnx"),
            movementModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/soldier.onnx"),
            intentMeta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_meta.json"),
            responsesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses.json"),
        };
        if (b.intentModel == null || b.movementModel == null || b.intentMeta == null || b.responsesJson == null)
        {
            Debug.LogError("[AISetup] Một số asset không load được — refresh Project rồi thử lại");
            Debug.LogError($"  intentModel: {(b.intentModel != null)}, movementModel: {(b.movementModel != null)}, intentMeta: {(b.intentMeta != null)}, responsesJson: {(b.responsesJson != null)}");
        }
        return b;
    }

    static void SaveAndOpen(Scene scene, string path)
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        // Open the saved scene to ensure user sees Hierarchy populated
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log($"[AISetup] Scene saved + opened: {path}. Hierarchy hiện các GameObject. Click Play khi sẵn sàng.");
    }

    static void SetColor(GameObject go, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null && r.sharedMaterial != null)
        {
            // Use sharedMaterial to avoid runtime material instances during edit
            var mat = new Material(r.sharedMaterial);
            mat.color = c;
            r.sharedMaterial = mat;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Layer + Tag setup
    // ─────────────────────────────────────────────────────────────────────
    static void EnsureLayersAndTags()
    {
        var tagAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tagAssets == null || tagAssets.Length == 0)
        {
            Debug.LogError("[AISetup] Không tìm thấy ProjectSettings/TagManager.asset");
            return;
        }
        var tm = new SerializedObject(tagAssets[0]);
        EnsureTag(tm, "Obstacle");
        EnsureTag(tm, "Target");
        var layers = tm.FindProperty("layers");
        SetLayerIfEmpty(layers, 6, "Obstacle");
        SetLayerIfEmpty(layers, 7, "Target");
        tm.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[AISetup] Layers + Tags ready: Obstacle (6), Target (7)");
    }

    static void EnsureTag(SerializedObject tm, string tag)
    {
        var tagsProp = tm.FindProperty("tags");
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
    }

    static void SetLayerIfEmpty(SerializedProperty layers, int idx, string name)
    {
        var slot = layers.GetArrayElementAtIndex(idx);
        if (string.IsNullOrEmpty(slot.stringValue) || slot.stringValue == name)
            slot.stringValue = name;
        else
            Debug.LogWarning($"[AISetup] Layer {idx} đã được dùng cho '{slot.stringValue}', skip");
    }
}

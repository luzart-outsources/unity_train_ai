
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.InferenceEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class AITestSceneBuilder
{
    [MenuItem("AI/1. Phase A — Chat Test (V2)", false, 100)]
    public static void BuildPhaseA()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var assets = LoadAssets();
        if (assets.intentV2Model == null) { Debug.LogError("[AISetup] V2 model thiếu — check Assets/AI/Models/intent_classifier_v2.onnx"); return; }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Commander voi brain V2 + ChatUI script + V2 stack (EntityExtractor + SmartContext)
        var commander = new GameObject("Commander");
        var brain = commander.AddComponent<NPCDialogueBrain>();
        brain.modelAsset = assets.intentV2Model;        // * V2 model (LSTM v5, 707K params, 96.8% hard test)
        brain.metaJson = assets.intentV2Meta;            // * V2 meta (vocab 10k, max_len=40)
        brain.responsesJson = assets.responsesV2Json;    // * V2 responses (5-7 templates, slot-aware)
        brain.backend = BackendType.CPU;
        brain.minConfidence = 0.40f;
        var chatUI = commander.AddComponent<PhaseAChatUI>();
        chatUI.slotVocabJson = assets.slotVocabJson;     // * EntityExtractor input

        var canvasGo = new GameObject("ChatCanvas", typeof(RectTransform));
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Background (full screen dark)
        var bg = MakeUIChild(canvasGo.transform, "Background");
        StretchAll(bg);
        bg.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.12f, 0.16f, 1f);

        // Header panel (top, height 100)
        var header = MakeUIChild(canvasGo.transform, "Header");
        AnchorTop(header, height: 100, leftRight: 40);
        var titleText = MakeText(header, "Title", "Phase A — NPC Chỉ Huy Chat", 32, FontStyle.Bold, Color.white,
                                 TextAnchor.UpperLeft);
        AnchorAll(titleText.rectTransform, leftRight: 0, top: 10, bottom: 50);
        var statusText = MakeText(header, "Status", "Đang khởi tạo...", 18, FontStyle.Normal,
                                  new Color(0.75f, 0.85f, 0.95f), TextAnchor.UpperLeft);
        AnchorAll(statusText.rectTransform, leftRight: 0, top: 55, bottom: 0);

        // ScrollView (middle)
        var scrollGo = MakeUIChild(canvasGo.transform, "ScrollView");
        AnchorMiddle(scrollGo, top: 110, bottom: 110, leftRight: 40);
        scrollGo.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.08f, 0.10f, 1f);
        var scrollRect = scrollGo.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

        var viewport = MakeUIChild(scrollGo, "Viewport");
        StretchAll(viewport);
        viewport.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.01f);
        viewport.gameObject.AddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

        var content = MakeUIChild(viewport, "Content");
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(0, 0);
        var contentLayout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 15, 15);
        contentLayout.spacing = 8;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        var contentFitter = content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        contentFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = content;

        // Input row (bottom)
        var inputRow = MakeUIChild(canvasGo.transform, "InputRow");
        AnchorBottom(inputRow, height: 80, leftRight: 40, bottom: 20);

        var inputBg = MakeUIChild(inputRow, "InputField");
        inputBg.anchorMin = new Vector2(0, 0);
        inputBg.anchorMax = new Vector2(1, 1);
        inputBg.offsetMin = new Vector2(0, 0);
        inputBg.offsetMax = new Vector2(-150, 0);   // leave 150 for button
        var inputImage = inputBg.gameObject.AddComponent<UnityEngine.UI.Image>();
        inputImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        var input = inputBg.gameObject.AddComponent<UnityEngine.UI.InputField>();

        var inputTextRT = MakeUIChild(inputBg, "Text");
        StretchAll(inputTextRT);
        inputTextRT.offsetMin = new Vector2(15, 5);
        inputTextRT.offsetMax = new Vector2(-15, -5);
        var inputText = inputTextRT.gameObject.AddComponent<UnityEngine.UI.Text>();
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 22;
        inputText.color = new Color(0.1f, 0.1f, 0.1f);
        inputText.alignment = TextAnchor.MiddleLeft;
        inputText.supportRichText = false;

        var placeholderRT = MakeUIChild(inputBg, "Placeholder");
        StretchAll(placeholderRT);
        placeholderRT.offsetMin = new Vector2(15, 5);
        placeholderRT.offsetMax = new Vector2(-15, -5);
        var placeholder = placeholderRT.gameObject.AddComponent<UnityEngine.UI.Text>();
        placeholder.font = inputText.font;
        placeholder.fontSize = 22;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.fontStyle = FontStyle.Italic;
        placeholder.text = "Gõ tiếng Việt rồi nhấn Enter...";

        input.targetGraphic = inputImage;
        input.textComponent = inputText;
        input.placeholder = placeholder;
        input.lineType = UnityEngine.UI.InputField.LineType.SingleLine;

        // Send button
        var btnRT = MakeUIChild(inputRow, "SendButton");
        btnRT.anchorMin = new Vector2(1, 0);
        btnRT.anchorMax = new Vector2(1, 1);
        btnRT.pivot = new Vector2(1, 0.5f);
        btnRT.anchoredPosition = Vector2.zero;
        btnRT.sizeDelta = new Vector2(140, 0);
        var btnImage = btnRT.gameObject.AddComponent<UnityEngine.UI.Image>();
        btnImage.color = new Color(0.2f, 0.55f, 0.85f, 1f);
        var btn = btnRT.gameObject.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = btnImage;
        var btnTextRT = MakeUIChild(btnRT, "Text");
        StretchAll(btnTextRT);
        var btnText = btnTextRT.gameObject.AddComponent<UnityEngine.UI.Text>();
        btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        btnText.fontSize = 22;
        btnText.fontStyle = FontStyle.Bold;
        btnText.color = Color.white;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.text = "Gửi";

        // EventSystem — Unity 6 Input System dung InputSystemUIInputModule
        if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        // Wire references vao ChatUI
        chatUI.titleText = titleText;
        chatUI.statusText = statusText;
        chatUI.contentParent = content;
        chatUI.scrollRect = scrollRect;
        chatUI.inputField = input;
        chatUI.sendButton = btn;

        // Camera dat sang cho khong quan trong (UI overlay khong can camera angle)
        var cam = GameObject.Find("Main Camera");
        if (cam != null) cam.transform.position = new Vector3(0, 1, -10);

        SaveAndOpen(scene, "Assets/Scenes/PhaseA_ChatTest.unity");
        Debug.Log("[AISetup] Phase A scene built — Hierarchy có Canvas + Background + Header + ScrollView + InputRow + Commander. Click Play.");
    }

    static RectTransform MakeUIChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static UnityEngine.UI.Text MakeText(RectTransform parent, string name, string content, int size,
                                         FontStyle style, Color color, TextAnchor align)
    {
        var rt = MakeUIChild(parent, name);
        var txt = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    static void StretchAll(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AnchorAll(RectTransform rt, float leftRight, float top, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(leftRight, bottom);
        rt.offsetMax = new Vector2(-leftRight, -top);
    }

    static void AnchorTop(RectTransform rt, float height, float leftRight)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(-2 * leftRight, height);
    }

    static void AnchorBottom(RectTransform rt, float height, float leftRight, float bottom)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, bottom);
        rt.sizeDelta = new Vector2(-2 * leftRight, height);
    }

    static void AnchorMiddle(RectTransform rt, float top, float bottom, float leftRight)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(leftRight, bottom);
        rt.offsetMax = new Vector2(-leftRight, -top);
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

        // Target — do
        var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        target.name = "Target";
        target.tag = "Target";
        if (targetLayerId >= 0) target.layer = targetLayerId;
        target.transform.position = new Vector3(8, 0.5f, 8);
        SetColor(target, Color.red);

        // 6 Obstacles — nau
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

        // Agent — xanh, gan MovementAgent (assets pre-assigned)
        var agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        agent.name = "Agent";
        agent.transform.position = new Vector3(-8, 0.5f, -8);
        SetColor(agent, Color.blue);
        // Disable collider trigger de khong bi physics
        var agentCol = agent.GetComponent<BoxCollider>();
        if (agentCol != null) agentCol.isTrigger = true;

        var moveAgent = agent.AddComponent<MovementAgent>();
        moveAgent.modelAsset = assets.movementModel;
        moveAgent.target = target.transform;
        moveAgent.backend = BackendType.CPU;
        if (obstacleLayerId >= 0) moveAgent.obstacleLayer = 1 << obstacleLayerId;
        if (targetLayerId >= 0) moveAgent.targetLayer = 1 << targetLayerId;

        // Camera — dat goc tren xuong
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

    [MenuItem("AI/3. Both — Combined (V2 Phase A + Phase B)", false, 102)]
    public static void BuildBoth()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var assets = LoadAssets();
        if (assets.intentV2Model == null || assets.movementModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Phase A: Commander dung V2 (PhaseAChatTester IMGUI fallback, khong Canvas vi 3D scene chiem view)
        var commander = new GameObject("Commander");
        var brain = commander.AddComponent<NPCDialogueBrain>();
        brain.modelAsset = assets.intentV2Model;          // * V2
        brain.metaJson = assets.intentV2Meta;              // * V2
        brain.responsesJson = assets.responsesV2Json;      // * V2
        brain.backend = BackendType.CPU;
        var tester = commander.AddComponent<PhaseAChatTester>();

        // Phase B: scene 3D (nhu BuildPhaseB nhung smaller arena de 2 system coexist)
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
        // V1 (legacy — giu cho menu compare/both)
        public ModelAsset intentModel;
        public TextAsset intentMeta, responsesJson;
        // V2 (canonical — dung cho menu 1)
        public ModelAsset intentV2Model;
        public TextAsset intentV2Meta, responsesV2Json, slotVocabJson;
        // Phase B
        public ModelAsset movementModel;
    }

    static AssetBundle LoadAssets()
    {
        var b = new AssetBundle
        {
            intentModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier.onnx"),
            intentMeta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_meta.json"),
            responsesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses.json"),

            intentV2Model = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier_v2.onnx"),
            intentV2Meta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_v2_meta.json"),
            responsesV2Json = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses_v2.json"),
            slotVocabJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/slot_vocab.json"),

            movementModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/soldier.onnx"),
        };
        // Verify required assets per menu — log warnings cho missing
        if (b.movementModel == null)
            Debug.LogWarning("[AISetup] Phase B model missing: Assets/AI/Models/soldier.onnx");
        if (b.intentV2Model == null)
            Debug.LogWarning("[AISetup] V2 intent model missing — menu 1 sẽ fail. Re-train hoặc download.");
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

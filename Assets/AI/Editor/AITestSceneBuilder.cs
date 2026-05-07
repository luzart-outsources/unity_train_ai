// AITestSceneBuilder.cs
//
// 1-CLICK SETUP: tạo Scene test với AITestRunner đã configure sẵn assets.
//
// Menu: AI > Build & Run Test Scene
//
// Workflow:
//   1. Đảm bảo Layers "Obstacle" + "Target" + Tags tồn tại (tự create)
//   2. Tạo scene mới với AITestRunner GameObject
//   3. Auto-assign các ModelAsset, TextAsset vào AITestRunner inspector
//   4. Save scene Assets/Scenes/AITest.unity
//   5. Mở scene và switch to Play mode

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.InferenceEngine;

public static class AITestSceneBuilder
{
    private const string SCENE_PATH = "Assets/Scenes/AITest.unity";

    [MenuItem("AI/Build && Run Test Scene", false, 100)]
    public static void BuildAndRun()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[AISetup] Đang Play — Stop trước khi build scene mới");
            return;
        }

        EnsureLayersAndTags();
        BuildScene();
        EnterPlayMode();
    }

    [MenuItem("AI/Just Build Scene (no Play)", false, 101)]
    public static void BuildOnly()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[AISetup] Đang Play — Stop trước");
            return;
        }
        EnsureLayersAndTags();
        BuildScene();
        Debug.Log("[AISetup] Scene đã build. Click Play khi sẵn sàng.");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Layer + Tag setup (programmatic — modify ProjectSettings/TagManager.asset)
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

        // Tags
        EnsureTag(tm, "Obstacle");
        EnsureTag(tm, "Target");

        // Layers — slot 6 và 7 (user layers)
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
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
    }

    static void SetLayerIfEmpty(SerializedProperty layers, int idx, string name)
    {
        var slot = layers.GetArrayElementAtIndex(idx);
        if (string.IsNullOrEmpty(slot.stringValue) || slot.stringValue == name)
        {
            slot.stringValue = name;
        }
        else
        {
            Debug.LogWarning($"[AISetup] Layer {idx} đã được dùng cho '{slot.stringValue}', skip");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Scene creation
    // ─────────────────────────────────────────────────────────────────────
    static void BuildScene()
    {
        // Load assets
        var intentModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier.onnx");
        var movementModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/soldier.onnx");
        var intentMeta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_meta.json");
        var responsesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses.json");

        if (intentModel == null || movementModel == null || intentMeta == null || responsesJson == null)
        {
            Debug.LogError("[AISetup] Một số asset không load được — refresh Project rồi thử lại");
            Debug.LogError($"  intentModel: {(intentModel != null)}, movementModel: {(movementModel != null)}, intentMeta: {(intentMeta != null)}, responsesJson: {(responsesJson != null)}");
            return;
        }

        // Create scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Add AITestRunner GameObject
        var runner = new GameObject("AITestRunner");
        var runnerComp = runner.AddComponent<AITestRunner>();
        runnerComp.intentModel = intentModel;
        runnerComp.intentMeta = intentMeta;
        runnerComp.responsesJson = responsesJson;
        runnerComp.movementModel = movementModel;

        // Save scene
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        AssetDatabase.Refresh();
        Debug.Log($"[AISetup] Scene saved: {SCENE_PATH}");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Play mode entry
    // ─────────────────────────────────────────────────────────────────────
    static void EnterPlayMode()
    {
        EditorApplication.delayCall += () =>
        {
            EditorApplication.EnterPlaymode();
            Debug.Log("[AISetup] ▶ Entering Play mode — đợi Console hiện kết quả");
        };
    }
}

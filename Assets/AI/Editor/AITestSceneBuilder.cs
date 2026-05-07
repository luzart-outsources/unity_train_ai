// AITestSceneBuilder.cs
//
// 1-CLICK SETUP — 3 menu items:
//   AI / 1. Phase A — Chat Test       → scene riêng test NPC chat (UI input)
//   AI / 2. Phase B — Movement Test    → scene riêng test agent movement
//   AI / 3. Both (combined)            → 1 scene chạy cả 2 phase tự động

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
        var (intentModel, _, intentMeta, responsesJson) = LoadAssets();
        if (intentModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var runner = new GameObject("PhaseAChatTester");
        var comp = runner.AddComponent<PhaseAChatTester>();
        comp.intentModel = intentModel;
        comp.intentMeta = intentMeta;
        comp.responsesJson = responsesJson;

        SaveAndPlay(scene, "Assets/Scenes/PhaseA_ChatTest.unity");
    }

    [MenuItem("AI/2. Phase B — Movement Test", false, 101)]
    public static void BuildPhaseB()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var (_, movementModel, _, _) = LoadAssets();
        if (movementModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var runner = new GameObject("PhaseBMovementTester");
        var comp = runner.AddComponent<PhaseBMovementTester>();
        comp.movementModel = movementModel;

        SaveAndPlay(scene, "Assets/Scenes/PhaseB_MovementTest.unity");
    }

    [MenuItem("AI/3. Both — Combined Auto Test", false, 102)]
    public static void BuildBoth()
    {
        if (!CheckNotPlaying()) return;
        EnsureLayersAndTags();
        var (intentModel, movementModel, intentMeta, responsesJson) = LoadAssets();
        if (intentModel == null || movementModel == null) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var runner = new GameObject("AITestRunner");
        var comp = runner.AddComponent<AITestRunner>();
        comp.intentModel = intentModel;
        comp.intentMeta = intentMeta;
        comp.responsesJson = responsesJson;
        comp.movementModel = movementModel;

        SaveAndPlay(scene, "Assets/Scenes/AITest.unity");
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

    static (ModelAsset intentModel, ModelAsset movementModel, TextAsset intentMeta, TextAsset responsesJson) LoadAssets()
    {
        var intentModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier.onnx");
        var movementModel = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/soldier.onnx");
        var intentMeta = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_meta.json");
        var responsesJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses.json");

        if (intentModel == null || movementModel == null || intentMeta == null || responsesJson == null)
        {
            Debug.LogError("[AISetup] Một số asset không load được — refresh Project rồi thử lại");
            Debug.LogError($"  intentModel: {(intentModel != null)}, movementModel: {(movementModel != null)}, intentMeta: {(intentMeta != null)}, responsesJson: {(responsesJson != null)}");
        }
        return (intentModel, movementModel, intentMeta, responsesJson);
    }

    static void SaveAndPlay(UnityEngine.SceneManagement.Scene scene, string path)
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        Debug.Log($"[AISetup] Scene saved: {path}");
        EditorApplication.delayCall += () =>
        {
            EditorApplication.EnterPlaymode();
            Debug.Log("[AISetup] ▶ Entering Play mode...");
        };
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
}

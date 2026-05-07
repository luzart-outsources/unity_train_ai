
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.InferenceEngine;

public static class PhaseACompareSceneBuilder
{
    [MenuItem("AI/4. Phase A — Compare V1 (Old) vs V2 (New)", false, 103)]
    public static void BuildCompareScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Compare] Đang Play — Stop trước khi build scene mới");
            return;
        }

        // V1 assets
        var v1Model     = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier.onnx");
        var v1Meta      = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_meta.json");
        var v1Responses = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses.json");

        // V2 assets - may not exist yet if train chua xong
        var v2Model     = AssetDatabase.LoadAssetAtPath<ModelAsset>("Assets/AI/Models/intent_classifier_v2.onnx");
        var v2Meta      = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/intent_classifier_v2_meta.json");
        var v2Responses = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/responses_v2.json");
        var slotVocab   = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/AI/Resources/slot_vocab.json");

        if (v1Model == null || v1Meta == null || v1Responses == null)
        {
            Debug.LogError("[Compare] V1 assets missing — cần intent_classifier.onnx + meta + responses.json");
            return;
        }
        if (v2Model == null) Debug.LogWarning("[Compare] V2 model chưa có (intent_classifier_v2.onnx) — V2 column sẽ dùng tạm V1 model + slot extractor");
        if (slotVocab == null) Debug.LogWarning("[Compare] slot_vocab.json missing — V2 sẽ không trích slot");
        if (v2Responses == null) Debug.LogWarning("[Compare] responses_v2.json missing — V2 sẽ dùng responses.json cũ");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var go = new GameObject("PhaseACompare");
        var tester = go.AddComponent<PhaseACompareTester>();
        tester.v1Model = v1Model;
        tester.v1Meta = v1Meta;
        tester.v1Responses = v1Responses;
        tester.v2Model = v2Model;
        tester.v2Meta = v2Meta;
        tester.v2Responses = v2Responses;
        tester.slotVocabJson = slotVocab;
        tester.backend = BackendType.CPU;
        tester.minConfidence = 0.40f;

        // Camera background dark - match IMGUI bg
        var cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            cam.transform.position = new Vector3(0, 1, -10);
            var c = cam.GetComponent<Camera>();
            if (c != null) c.backgroundColor = new Color(0.07f, 0.10f, 0.13f);
        }

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        const string path = "Assets/Scenes/PhaseA_Compare.unity";
        EditorSceneManager.SaveScene(scene, path);
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        Debug.Log($"[Compare] Scene built + opened: {path}. Click Play để so sánh V1 vs V2.");
    }
}

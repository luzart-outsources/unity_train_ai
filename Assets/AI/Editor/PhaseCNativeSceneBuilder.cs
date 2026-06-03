// Phase C Native — Editor menu builder for the OFFLINE ONNX embedding chat.
//
// AI > 6. Phase C Native — ONNX Chat Test
//
// Loads the student encoder ONNX + bank assets that
// build_bank_for_unity.py + export_student_onnx.py mirrored into
// Assets/AI/Models/ and Assets/AI/Resources/. No Python server needed.

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.InferenceEngine;
using TrainAI.AI;

public static class PhaseCNativeSceneBuilder
{
    private const string ModelPath  = "Assets/AI/Models/student_encoder.onnx";
    private const string VocabPath  = "Assets/AI/Resources/vocab_phase_c.json";
    private const string BankPath   = "Assets/AI/Resources/student_bank.bytes";
    private const string MetaPath   = "Assets/AI/Resources/student_bank.json";

    [MenuItem("AI/6. Phase C Native — ONNX Chat Test", false, 401)]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[PhaseCNative] Stop Play mode first.");
            return;
        }

        // Verify all 4 deliverable assets exist.
        if (!File.Exists(ModelPath))
        {
            Debug.LogError($"[PhaseCNative] missing {ModelPath}. " +
                           "Run: python AI_Training/phase_c_chat/scripts/export_student_onnx.py");
            return;
        }
        if (!File.Exists(VocabPath))
        {
            Debug.LogError($"[PhaseCNative] missing {VocabPath}. " +
                           "Run: python AI_Training/phase_c_chat/scripts/build_bank_for_unity.py");
            return;
        }
        if (!File.Exists(BankPath))
        {
            Debug.LogError($"[PhaseCNative] missing {BankPath}. " +
                           "Run: python AI_Training/phase_c_chat/scripts/build_bank_for_unity.py");
            return;
        }
        if (!File.Exists(MetaPath))
        {
            Debug.LogError($"[PhaseCNative] missing {MetaPath}. " +
                           "Run: python AI_Training/phase_c_chat/scripts/build_bank_for_unity.py");
            return;
        }

        AssetDatabase.Refresh();

        var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
        var vocab      = AssetDatabase.LoadAssetAtPath<TextAsset>(VocabPath);
        var bank       = AssetDatabase.LoadAssetAtPath<TextAsset>(BankPath);
        var meta       = AssetDatabase.LoadAssetAtPath<TextAsset>(MetaPath);

        if (modelAsset == null)
        {
            Debug.LogError($"[PhaseCNative] Could not load {ModelPath} as ModelAsset — open it once in Unity to import.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var root = new GameObject("PhaseC_Native_Chat");

        var brain = root.AddComponent<EmbeddingChatBrain>();
        brain.encoderModel  = modelAsset;
        brain.vocabJson     = vocab;
        brain.bankBytes     = bank;
        brain.bankMetaJson  = meta;
        brain.backend       = BackendType.CPU;        // safe default — switch in Inspector
        brain.maxLen        = 40;
        brain.highThreshold = 0.88f;
        brain.lowThreshold  = 0.72f;

        var ctx     = root.AddComponent<GameStateContext>();
        // Leave SO refs empty — falls back to day=1, time="07:30", area="doanh trại".

        var tester  = root.AddComponent<PhaseCNativeTester>();
        tester.brain = brain;
        tester.gameStateContextComponent = ctx;

        const string outPath = "Assets/AI/Scenes/PhaseC_Native_Chat.unity";
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        EditorSceneManager.SaveScene(scene, outPath);
        Debug.Log($"[PhaseCNative] Scene built at {outPath}.");
        Debug.Log("[PhaseCNative] Hit Play. No Python server needed.");
    }
}

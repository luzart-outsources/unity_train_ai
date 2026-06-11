// Phase C Native — Unity Editor batch smoke test for EmbeddingChatBrain.
//
// AI > 7. Phase C Native — Run Smoke Test (no Play mode)
//
// Builds a hidden GameObject, calls EmbeddingChatBrain.Awake() to load
// ONNX + vocab + bank, runs a curated set of Vietnamese queries that
// covers all hardset categories, prints route/score/intent/entity/snippet
// to the Console, and reports pass/fail. No Play mode entry required —
// runs entirely in Edit mode.
//
// Why this exists:
//   - Unity Inference Engine is the runtime that actually matters in
//     production; running the real Worker through ONNX catches asset
//     binding bugs, .meta import issues, and tensor-shape mismatches
//     that Python (onnxruntime) cannot.
//   - Quick (under 2 seconds total) so iterating on threshold/data is
//     easy: Edit → Save → run from menu → Console results.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Unity.InferenceEngine;
using TrainAI.AI;

public static class PhaseCNativeSmokeTest
{
    private const string ModelPath = "Assets/AI/Models/student_encoder.onnx";
    private const string VocabPath = "Assets/AI/Resources/vocab_phase_c.json";
    private const string BankPath  = "Assets/AI/Resources/student_bank.bytes";
    private const string MetaPath  = "Assets/AI/Resources/student_bank.json";

    private struct Case
    {
        public string Query;
        public string ExpectedIntent;   // null = ORIGINAL_BUG/OOS sentinel
        public string ExpectedEntity;   // optional
        public string Category;
    }

    private static readonly Case[] Cases = new[]
    {
        // --- CLEAN ---
        N("Sân vận động ở đâu",     "ASK_LOCATION",       "SanVanDong",    "CLEAN"),
        N("Nhà ăn ở đâu",           "ASK_LOCATION",       "NhaAn_Door",    "CLEAN"),
        N("Lớp học ở đâu",          "ASK_LOCATION",       "LopHoc_Door",   "CLEAN"),
        N("Ký túc xá ở đâu",        "ASK_LOCATION",       "KTX_Door",      "CLEAN"),
        N("Khu tự do ở đâu",        "ASK_LOCATION",       "FreeArea",      "CLEAN"),
        N("Khu dọn vệ sinh ở đâu",  "ASK_LOCATION",       "DonVeSinh",     "CLEAN"),
        N("Mấy giờ tập thể dục",    "ASK_TIME",           "slot_1",        "CLEAN"),
        N("Hôm nay học môn gì",     "ASK_SCHEDULE_TODAY", null,            "CLEAN"),
        N("Đại đội trưởng là ai",   "ASK_NPC",            "DaiDoiTruong",  "CLEAN"),
        N("Lịch sử là môn gì",      "ASK_SUBJECT_INFO",   "LichSu",        "CLEAN"),
        N("Em chào thủ trưởng",     "GREETING",           null,            "CLEAN"),
        N("Em xin phép",            "GOODBYE",            null,            "CLEAN"),
        N("Cảm ơn anh",             "THANKS",             null,            "CLEAN"),

        // --- NO_ACCENT ---
        N("san van dong o dau",     "ASK_LOCATION",       "SanVanDong",    "NO_ACCENT"),
        N("nha an o dau",           "ASK_LOCATION",       "NhaAn_Door",    "NO_ACCENT"),
        N("ky tuc xa o dau",        "ASK_LOCATION",       "KTX_Door",      "NO_ACCENT"),
        N("dai doi truong la ai",   "ASK_NPC",            "DaiDoiTruong",  "NO_ACCENT"),

        // --- CODE_MIX ---
        N("Where is canteen",       "ASK_LOCATION",       "NhaAn_Door",    "CODE_MIX"),
        N("Today học gì",           "ASK_SCHEDULE_TODAY", null,            "CODE_MIX"),

        // --- SLANG ---
        N("svd đâu v",              "ASK_LOCATION",       "SanVanDong",    "SLANG"),
        N("ktx đâu nhỉ",            "ASK_LOCATION",       "KTX_Door",      "SLANG"),
        N("ddt la ai v",            "ASK_NPC",            "DaiDoiTruong",  "SLANG"),

        // --- ELLIPSIS ---
        N("nhà ăn?",                "ASK_LOCATION",       "NhaAn_Door",    "ELLIPSIS"),
        N("KTX đâu",                "ASK_LOCATION",       "KTX_Door",      "ELLIPSIS"),

        // --- OOS ---
        N("Crypto giảm sốc",        "OUT_OF_SCOPE",       null,            "OOS"),
        N("Anh có người yêu chưa",  "OUT_OF_SCOPE",       null,            "OOS"),

        // --- ORIGINAL_BUG (expect non-template route) ---
        N("khu A1 ở đâu",           null,                 null,            "ORIGINAL_BUG"),
        N("khu A2 ở đâu",           null,                 null,            "ORIGINAL_BUG"),
        N("khu A3 ở đâu",           null,                 null,            "ORIGINAL_BUG"),
        N("khu B ở đâu",            null,                 null,            "ORIGINAL_BUG"),
    };

    private static Case N(string q, string ei, string ee, string cat) =>
        new Case { Query = q, ExpectedIntent = ei, ExpectedEntity = ee, Category = cat };

    [MenuItem("AI/7. Phase C Native — Run Smoke Test (no Play mode)", false, 402)]
    public static void Run()
    {
        // 1. Verify deliverables present.
        if (!RequireAsset(ModelPath, "ONNX")) return;
        if (!RequireAsset(VocabPath, "vocab"))  return;
        if (!RequireAsset(BankPath,  "bank"))   return;
        if (!RequireAsset(MetaPath,  "meta"))   return;

        var modelAsset = AssetDatabase.LoadAssetAtPath<ModelAsset>(ModelPath);
        var vocab      = AssetDatabase.LoadAssetAtPath<TextAsset>(VocabPath);
        var bank       = AssetDatabase.LoadAssetAtPath<TextAsset>(BankPath);
        var meta       = AssetDatabase.LoadAssetAtPath<TextAsset>(MetaPath);

        if (modelAsset == null)
        {
            Debug.LogError("[SmokeTest] ModelAsset not yet imported. Open the .onnx file in Inspector once or re-import.");
            return;
        }

        // 2. Spin up an ephemeral GameObject with the brain.
        var host = new GameObject("__PhaseC_SmokeTest__") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var brain = host.AddComponent<EmbeddingChatBrain>();
            brain.encoderModel  = modelAsset;
            brain.vocabJson     = vocab;
            brain.bankBytes     = bank;
            brain.bankMetaJson  = meta;
            brain.backend       = BackendType.CPU;   // safest in edit mode
            brain.maxLen        = 40;
            // Use the same thresholds as the production scene.

            // Manually trigger Awake (Unity only calls it on enable in scenes).
            var awakeMethod = typeof(EmbeddingChatBrain).GetMethod(
                "Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awakeMethod?.Invoke(brain, null);

            if (!brain.enabled)
            {
                Debug.LogError("[SmokeTest] Brain failed to initialize — see earlier errors.");
                return;
            }

            // 3. Run the suite.
            var perCat = new Dictionary<string, (int pass, int total)>();
            int pass = 0;
            double totalMs = 0;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"Phase C Native — Smoke Test  ({Cases.Length} cases, backend CPU)");
            sb.AppendLine($"high={brain.highThreshold:F2}  low={brain.lowThreshold:F2}");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"{"STATUS",-6} {"ROUTE",-16} {"SCORE",6} {"INTENT",-18} {"ENTITY",-16} {"CAT",-12} QUERY");
            sb.AppendLine(new string('-', 110));

            foreach (var c in Cases)
            {
                var t0 = DateTime.UtcNow;
                var r  = brain.Ask(c.Query, null);
                totalMs += (DateTime.UtcNow - t0).TotalMilliseconds;

                bool ok;
                if (c.ExpectedIntent == null)
                {
                    // ORIGINAL_BUG: success = NOT a confident template hit
                    ok = r.route != "template";
                }
                else
                {
                    ok = r.topIntent == c.ExpectedIntent;
                    if (ok && c.ExpectedEntity != null)
                        ok = r.topEntity == c.ExpectedEntity;
                }

                if (ok) pass++;
                var prev = perCat.TryGetValue(c.Category, out var v) ? v : (pass: 0, total: 0);
                perCat[c.Category] = ok ? (prev.pass + 1, prev.total + 1) : (prev.pass, prev.total + 1);

                sb.AppendLine($"{(ok ? "OK" : "FAIL"),-6} {r.route,-16} {r.score,6:F2} {r.topIntent,-18} {r.topEntity,-16} {c.Category,-12} {c.Query}");
                if (!ok)
                    sb.AppendLine($"      → expected intent={c.ExpectedIntent} entity={c.ExpectedEntity}");
            }

            sb.AppendLine(new string('-', 110));
            sb.AppendLine($"OVERALL: {pass}/{Cases.Length} = {(pass * 100.0 / Cases.Length):F1}%");
            sb.AppendLine($"Average latency: {(totalMs / Cases.Length):F1} ms/query (Unity Worker, CPU)");
            sb.AppendLine();
            sb.AppendLine("Per-category:");
            foreach (var kv in perCat)
                sb.AppendLine($"  {kv.Key,-14} {kv.Value.pass}/{kv.Value.total} = {(kv.Value.pass * 100.0 / kv.Value.total):F1}%");

            if (pass == Cases.Length)
                Debug.Log(sb.ToString());
            else
                Debug.LogWarning(sb.ToString());

            // Persist transcript on disk for diffing later.
            var transcriptDir = "AI_Training/phase_c_chat/models";
            Directory.CreateDirectory(transcriptDir);
            File.WriteAllText(Path.Combine(transcriptDir, "unity_smoke_transcript.txt"), sb.ToString());
            Debug.Log($"[SmokeTest] transcript saved to {transcriptDir}/unity_smoke_transcript.txt");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }

    private static bool RequireAsset(string path, string label)
    {
        if (File.Exists(path)) return true;
        Debug.LogError($"[SmokeTest] missing {label} at {path}.\n" +
                       "Run the Python pipeline:\n" +
                       "  python AI_Training/phase_c_chat/scripts/train_student_encoder.py\n" +
                       "  python AI_Training/phase_c_chat/scripts/export_student_onnx.py\n" +
                       "  python AI_Training/phase_c_chat/scripts/build_bank_for_unity.py");
        return false;
    }
}

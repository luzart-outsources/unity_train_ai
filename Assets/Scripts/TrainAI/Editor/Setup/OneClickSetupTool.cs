using System;
using System.IO;
using System.Text;
using TrainAI.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    public static class OneClickSetupTool
    {
        private const string MenuRoot = "Tools/TrainAI/";

        [MenuItem(MenuRoot + "1-Click Full Setup", priority = 0)]
        public static void RunFullSetup()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TrainAI 1-Click Setup START ===");
            try
            {
                EnsureTagsAndLayers();
                sb.AppendLine("[1/6] Tags + Layers OK.");

                var db = SOInstanceCreator.CreateAll();
                sb.AppendLine($"[2/6] ScriptableObjects created: {db.name}");

                var prefabs = PrefabBuilder.CreateAll();
                sb.AppendLine($"[3/6] Prefabs built: Player, NPC, Interactable, 11 UI prefabs.");

                var registry = UIRegistryBuilder.Build(prefabs);
                sb.AppendLine($"[4/6] UIRegistrySO built: {registry.Entries.Count} entries.");

                SceneBuilder.BuildAll(db, registry, prefabs);
                sb.AppendLine("[5/6] 6 scenes built + Build Settings updated.");

                int errors = DatabaseValidator.Validate(db, registry);
                sb.AppendLine($"[6/6] Validate: {errors} errors.");

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                sb.AppendLine("=== SETUP DONE. Open scene Assets/Scenes/TrainAI/_Boot.unity to play. ===");
                Debug.Log(sb.ToString());
                EditorUtility.DisplayDialog("TrainAI Setup",
                    sb.ToString() + "\n\nNext: open _Boot.unity then press Play.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[TrainAI Setup] FAILED: {e}");
                EditorUtility.DisplayDialog("TrainAI Setup ERROR",
                    $"Setup failed: {e.Message}\nXem Console de biet chi tiet.", "OK");
            }
        }

        [MenuItem(MenuRoot + "Validate Database", priority = 10)]
        public static void RunValidate()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabaseSO>("Assets/Configs/TrainAI/_GameDatabase.asset");
            var registry = AssetDatabase.LoadAssetAtPath<Luzart.UIRegistrySO>("Assets/Configs/TrainAI/UIRegistry.asset");
            int err = DatabaseValidator.Validate(db, registry);
            EditorUtility.DisplayDialog("Validate", $"Errors: {err}. Xem Console.", "OK");
        }

        [MenuItem(MenuRoot + "Open Boot Scene", priority = 11)]
        public static void OpenBootScene()
        {
            var path = "Assets/Scenes/TrainAI/_Boot.unity";
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("Open Boot", "Chua run Setup. Bam '1-Click Full Setup' truoc.", "OK");
                return;
            }
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(path);
        }

        [MenuItem(MenuRoot + "Reset Setup (DELETE all TrainAI assets)", priority = 100)]
        public static void RunReset()
        {
            if (!EditorUtility.DisplayDialog("Reset",
                "Se XOA toan bo Assets/Configs/TrainAI, Assets/Prefabs/TrainAI, Assets/Scenes/TrainAI.\nTiep tuc?",
                "XOA", "Huy")) return;
            AssetDatabase.DeleteAsset("Assets/Configs/TrainAI");
            AssetDatabase.DeleteAsset("Assets/Prefabs/TrainAI");
            AssetDatabase.DeleteAsset("Assets/Scenes/TrainAI");
            AssetDatabase.Refresh();
            Debug.Log("[TrainAI] Reset done.");
        }

        private static void EnsureTagsAndLayers()
        {
            // Tag Player.
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;
            var tagManager = new SerializedObject(assets[0]);
            var tagsProp = tagManager.FindProperty("tags");
            EnsureTag(tagsProp, "Player");
            EnsureTag(tagsProp, "NPC");
            EnsureTag(tagsProp, "Interactable");
            tagManager.ApplyModifiedProperties();
        }

        private static void EnsureTag(SerializedProperty tagsProp, string tag)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var p = tagsProp.GetArrayElementAtIndex(i);
                if (p.stringValue == tag) return;
            }
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        }
    }

    public static class DatabaseValidator
    {
        public static int Validate(GameDatabaseSO db, Luzart.UIRegistrySO registry)
        {
            int errors = 0;
            if (db == null)
            {
                Debug.LogError("[Validate] GameDatabaseSO null."); return 1;
            }
            if (!db.IsValid(out var err))
            {
                Debug.LogError($"[Validate] Database invalid: {err}"); errors++;
            }
            if (db.dayCycle != null)
            {
                for (int d = 0; d < db.dayCycle.dayPlans.Count; d++)
                {
                    var plan = db.dayCycle.dayPlans[d];
                    if (plan == null) { Debug.LogError($"[Validate] DayPlan {d} null."); errors++; continue; }
                    if (plan.quests == null || plan.quests.Count == 0)
                    { Debug.LogWarning($"[Validate] DayPlan {plan.name} co 0 quest."); }
                }
            }
            if (registry == null)
            {
                Debug.LogError("[Validate] UIRegistrySO null."); errors++;
            }
            else
            {
                var entries = registry.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    if (!entries[i].IsValid)
                    { Debug.LogError($"[Validate] UIConfig at index {i} invalid."); errors++; }
                }
            }
            Debug.Log($"[Validate] Done. Errors: {errors}.");
            return errors;
        }
    }
}

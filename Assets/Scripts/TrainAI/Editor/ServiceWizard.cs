using System.IO;
using TrainAI.Services;
using TrainAI.SO.Base;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class ServiceWizard
    {
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";
        const string ConfigFolder = "Assets/_Data/Config";
        const string RuntimeFolder = "Assets/_Data/Runtime";
        const string RulesFolder = "Assets/_Data/Rules";
        const string ModelsFolder = "Assets/_Models";

        [MenuItem("Tools/Build Game/3. Build Service Locator", false, 103)]
        public static void BuildServiceLocator()
        {
            EnsureFolders();
            var locator = BlueprintLoader.CreateOrLoad<ServiceLocatorSO>(LocatorPath);

            locator.gameConfig = EnsureRuntime<GameConfigSO>($"{ConfigFolder}/GameConfig.asset");
            locator.timeConfig = EnsureRuntime<TimeConfigSO>($"{ConfigFolder}/TimeConfig.asset");

            locator.playerState = EnsureRuntime<PlayerStateRSO>($"{RuntimeFolder}/PlayerState.asset");
            locator.clock = EnsureRuntime<GameClockRSO>($"{RuntimeFolder}/GameClock.asset");
            locator.activeQuest = EnsureRuntime<ActiveQuestRSO>($"{RuntimeFolder}/ActiveQuest.asset");
            locator.dayProgress = EnsureRuntime<DayProgressRSO>($"{RuntimeFolder}/DayProgress.asset");

            locator.questDB = LoadDB<QuestDB>();
            locator.npcDB = LoadDB<NPCDB>();
            locator.interactDB = LoadDB<InteractableDB>();
            locator.dayDB = LoadDB<DayDB>();
            locator.quizDB = LoadDB<QuizDB>();
            locator.areaDB = LoadDB<AreaDB>();
            locator.subjectDB = LoadDB<SubjectDB>();

            locator.intentModel = LoadModel("intent_classifier");
            locator.soldierModel = LoadModel("soldier");
            locator.intentMetaJson = LoadText("intent_classifier_meta");
            locator.responsesJson = LoadText("responses");

            if (locator.scoreRules == null) locator.scoreRules = new();
            locator.scoreRules.Clear();
            var lateRule = AssetDatabase.LoadAssetAtPath<ScoreRuleSO>($"{RulesFolder}/Rule_LatePenalty.asset");
            if (lateRule != null) locator.scoreRules.Add(lateRule);

            locator.endingRule = AssetDatabase.LoadAssetAtPath<EndingRuleSO>($"{RulesFolder}/Rule_Ending_Academy.asset");

            EditorUtility.SetDirty(locator);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ServiceWizard] ServiceLocator wired -> {LocatorPath}");
        }

        static T EnsureRuntime<T>(string path) where T : ScriptableObject
            => BlueprintLoader.CreateOrLoad<T>(path);

        static T LoadDB<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var g in guids)
            {
                var p = AssetDatabase.GUIDToAssetPath(g);
                var a = AssetDatabase.LoadAssetAtPath<T>(p);
                if (a != null) return a;
            }
            return null;
        }

        static ModelAsset LoadModel(string fileStem)
        {
            return AssetDatabase.LoadAssetAtPath<ModelAsset>($"{ModelsFolder}/{fileStem}.onnx");
        }

        static TextAsset LoadText(string fileStem)
        {
            return AssetDatabase.LoadAssetAtPath<TextAsset>($"{ModelsFolder}/{fileStem}.json");
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { ConfigFolder, RuntimeFolder })
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = Path.GetDirectoryName(f).Replace('\\', '/');
                    string leaf = Path.GetFileName(f);
                    if (!AssetDatabase.IsValidFolder(parent))
                        AssetDatabase.CreateFolder(Path.GetDirectoryName(parent).Replace('\\', '/'), Path.GetFileName(parent));
                    AssetDatabase.CreateFolder(parent, leaf);
                }
        }
    }
}

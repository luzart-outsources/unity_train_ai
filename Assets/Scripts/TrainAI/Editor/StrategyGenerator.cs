using System.Collections.Generic;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class StrategyGenerator
    {
        const string StrategiesFolder = "Assets/_Data/Strategies";
        const string RulesFolder = "Assets/_Data/Rules";

        [MenuItem("Tools/Build Game/2. Generate Strategy SO", false, 102)]
        public static void GenerateAll()
        {
            EnsureFolders();
            BlueprintLoader.CreateOrLoad<IdleMovementSO>($"{StrategiesFolder}/Movement_Idle.asset");
            BlueprintLoader.CreateOrLoad<OnnxMovementSO>($"{StrategiesFolder}/Movement_Onnx.asset");
            BlueprintLoader.CreateOrLoad<NavMeshMovementSO>($"{StrategiesFolder}/Movement_NavMesh.asset");
            BlueprintLoader.CreateOrLoad<MockDialogueSO>($"{StrategiesFolder}/Dialogue_Mock.asset");
            BlueprintLoader.CreateOrLoad<OnnxDialogueSO>($"{StrategiesFolder}/Dialogue_Onnx.asset");
            BlueprintLoader.CreateOrLoad<OpenConfirmInteractionSO>($"{StrategiesFolder}/Interact_Confirm_Default.asset");

            BlueprintLoader.CreateOrLoad<LatePenaltyRuleSO>($"{RulesFolder}/Rule_LatePenalty.asset");
            BlueprintLoader.CreateOrLoad<MilitaryAcademyEndingSO>($"{RulesFolder}/Rule_Ending_Academy.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StrategyGenerator] strategy + rule presets generated");
        }

        static void EnsureFolders()
        {
            foreach (var f in new[] { StrategiesFolder, RulesFolder })
                if (!AssetDatabase.IsValidFolder(f))
                {
                    string parent = System.IO.Path.GetDirectoryName(f).Replace('\\', '/');
                    string leaf = System.IO.Path.GetFileName(f);
                    AssetDatabase.CreateFolder(parent, leaf);
                }
        }
    }
}

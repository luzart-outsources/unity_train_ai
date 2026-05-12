using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class MasterBuilder
    {
        [MenuItem("Tools/Build Game/Generate All (Master) - TrainAI v2", false, 200)]
        public static void GenerateAll()
        {
            Debug.Log("[MasterBuilder] === START ===");
            StrategyGenerator.GenerateAll();
            SOGenerator.GenerateAll();
            ServiceWizard.BuildServiceLocator();
            PrefabBuilder.BuildAll();
            SceneBuilder.BuildAll();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Validator.ValidateAll();
            SmokeTest.Run();
            Debug.Log("[MasterBuilder] === DONE ===");
        }
    }
}

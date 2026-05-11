using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    // Menu de re-add QuestArrow vao Player prefab da co (khong can chay lai Full Setup).
    public static class PlayerPrefabRebuilder
    {
        private const string PlayerPath = "Assets/Prefabs/TrainAI/Player.prefab";

        [MenuItem("Tools/TrainAI/Rebuild Player (add Quest Arrow)", priority = 13)]
        public static void AddQuestArrowToPlayer()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath);
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Player prefab missing",
                    "Khong tim thay " + PlayerPath + ". Chay '1-Click Full Setup' truoc.", "OK");
                return;
            }

            // Open prefab to edit, add arrow, save.
            var contents = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                PrefabBuilder.BuildQuestArrowChild(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, PlayerPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Quest Arrow",
                "Da add QuestArrow vao Player.prefab.\n" +
                "Open scene World/Classroom/... -> Player instance trong scene se KHONG auto-update " +
                "(scene da save player cu). Nen: ban hay xoa Player trong scene + drag prefab tu Assets/Prefabs/TrainAI/Player.prefab vao.\n" +
                "Hoac chay menu 'Reset Setup' + '1-Click Full Setup' lai tu dau de scenes co Player moi.",
                "OK");
        }
    }
}

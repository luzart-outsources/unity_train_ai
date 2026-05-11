using System.Collections.Generic;
using TrainAI.Player;
using TrainAI.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    // Tool moi (KHONG sua menu cu) - wire reference cross-prefab trong scene da co.
    // Vd: PlayerController.joystick chi exist runtime sau khi UI show, KHONG the
    // wire trong PrefabBuilder. Tool nay duyet scene + try wire neu co the.
    // Note: da co runtime auto-find (Awake/Update lazy find) -> tool nay la extra
    // de wire ngay tu editor neu user muon thay reference trong inspector.
    public static class SceneReferenceWirer
    {
        [MenuItem("Tools/TrainAI/Wire Scene References (post-setup)", priority = 14)]
        public static void WireAllScenes()
        {
            string[] paths =
            {
                "Assets/Scenes/TrainAI/_Boot.unity",
                "Assets/Scenes/TrainAI/World.unity",
                "Assets/Scenes/TrainAI/Classroom.unity",
                "Assets/Scenes/TrainAI/Dormitory.unity",
                "Assets/Scenes/TrainAI/Cafeteria.unity",
            };

            var report = new List<string>();
            foreach (var p in paths)
            {
                if (!System.IO.File.Exists(p)) continue;
                var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                int wired = WireCurrentScene();
                if (wired > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    report.Add($"{p}: {wired} ref wired");
                }
            }

            string msg = report.Count == 0
                ? "Khong ref nao can wire (runtime auto-find handle moi thu)."
                : string.Join("\n", report);
            EditorUtility.DisplayDialog("Wire Scene References", msg, "OK");
            Debug.Log("[SceneReferenceWirer] " + msg);
        }

        // Wire 1 lan cho scene hien tai. Tra ve so ref da wire.
        private static int WireCurrentScene()
        {
            int count = 0;
            var player = GameObject.FindGameObjectWithTag("Player");

            // PlayerCameraFollow.target = Player.transform.
            foreach (var follow in Object.FindObjectsByType<PlayerCameraFollow>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(follow);
                var prop = so.FindProperty("target");
                if (prop != null && prop.objectReferenceValue == null && player != null)
                {
                    prop.objectReferenceValue = player.transform;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    count++;
                }
            }

            // MiniMapHUD.playerWorld = Player.transform.
            foreach (var mini in Object.FindObjectsByType<MiniMapHUD>(FindObjectsSortMode.None))
            {
                var so = new SerializedObject(mini);
                var prop = so.FindProperty("playerWorld");
                if (prop != null && prop.objectReferenceValue == null && player != null)
                {
                    prop.objectReferenceValue = player.transform;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    count++;
                }
            }

            return count;
        }
    }
}

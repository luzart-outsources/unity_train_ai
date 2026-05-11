using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TrainAI.Editor.Setup
{
    // Fix scene da co StandaloneInputModule cu (legacy Input) -> swap sang
    // InputSystemUIInputModule de khong throw InvalidOperationException.
    public static class InputModuleFixer
    {
        [MenuItem("Tools/TrainAI/Fix Input Modules (all TrainAI scenes)", priority = 12)]
        public static void FixAllScenes()
        {
            string[] paths =
            {
                "Assets/Scenes/TrainAI/_Boot.unity",
                "Assets/Scenes/TrainAI/Title.unity",
                "Assets/Scenes/TrainAI/World.unity",
                "Assets/Scenes/TrainAI/Classroom.unity",
                "Assets/Scenes/TrainAI/Dormitory.unity",
                "Assets/Scenes/TrainAI/Cafeteria.unity",
            };

            var fixedScenes = new List<string>();
            foreach (var p in paths)
            {
                if (!System.IO.File.Exists(p)) continue;
                var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Single);
                bool changed = false;
                foreach (var es in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
                {
                    var legacy = es.GetComponent<StandaloneInputModule>();
                    if (legacy != null)
                    {
                        Object.DestroyImmediate(legacy);
                        changed = true;
                    }
                    if (es.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        es.gameObject.AddComponent<InputSystemUIInputModule>();
                        changed = true;
                    }
                }
                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    fixedScenes.Add(p);
                }
            }
            EditorUtility.DisplayDialog("Fix Input Modules",
                fixedScenes.Count == 0
                    ? "Khong scene nao can fix (da dung InputSystemUIInputModule)."
                    : $"Da fix {fixedScenes.Count} scene:\n  " + string.Join("\n  ", fixedScenes),
                "OK");
            Debug.Log($"[InputModuleFixer] Fixed {fixedScenes.Count} scenes.");
        }
    }
}

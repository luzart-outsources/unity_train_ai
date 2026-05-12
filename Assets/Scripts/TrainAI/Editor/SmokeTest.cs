using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TrainAI.Presentation;
using TrainAI.Services;
using TrainAI.SO.Base;
using TrainAI.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TrainAI.Editor
{
    public static class SmokeTest
    {
        const string ReportPath = "Library/smoke_test_report.md";
        const string SceneFolder = "Assets/Scenes/TrainAI";

        [MenuItem("Tools/Build Game/7. Smoke Test", false, 107)]
        public static void Run()
        {
            var report = new StringBuilder();
            report.AppendLine("# TrainAI smoke test report");
            report.AppendLine($"_generated: {System.DateTime.Now:O}_");
            report.AppendLine();

            int fails = 0;
            string prevScene = SceneManager.GetActiveScene().path;

            fails += TestScene("00_Bootstrap", report, scene =>
            {
                int local = 0;
                local += ExpectComponent<BootstrapEntry>("Bootstrap", report);
                local += ExpectComponent<GameLoopDriver>("Bootstrap", report);
                local += ExpectComponent<EventSystem>("EventSystem", report);
                local += ExpectSerializedFieldNonNull<BootstrapEntry>("Bootstrap", "services", report);
                local += ExpectSerializedFieldNonNull<GameLoopDriver>("Bootstrap", "services", report);

                local += ExpectComponent<UIRouterMono>("UICanvas", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "services", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "confirm", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "dialogue", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "loading", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "quiz", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "ending", report);
                local += ExpectSerializedFieldNonNull<UIRouterMono>("UICanvas", "expel", report);
                return local;
            });

            fails += TestScene("01_MainMenu", report, scene =>
            {
                int local = 0;
                local += ExpectComponent<UIMainMenuController>("MainMenuCanvas", report);
                local += ExpectSerializedFieldNonNull<UIMainMenuController>("MainMenuCanvas", "services", report);
                local += ExpectSerializedFieldNonNull<UIMainMenuController>("MainMenuCanvas", "newGameButton", report);
                local += ExpectSerializedFieldNonNull<UIMainMenuController>("MainMenuCanvas", "createCharScene", report);
                local += ExpectSerializedFieldNonNull<UIMainMenuController>("MainMenuCanvas", "worldScene", report);
                return local;
            });

            fails += TestScene("03_CreateChar", report, scene =>
            {
                int local = 0;
                local += ExpectComponent<UICreateCharController>("CreateCharCanvas", report);
                local += ExpectSerializedFieldNonNull<UICreateCharController>("CreateCharCanvas", "services", report);
                local += ExpectSerializedFieldNonNull<UICreateCharController>("CreateCharCanvas", "playerState", report);
                local += ExpectSerializedFieldNonNull<UICreateCharController>("CreateCharCanvas", "nameInput", report);
                local += ExpectSerializedFieldNonNull<UICreateCharController>("CreateCharCanvas", "worldScene", report);
                return local;
            });

            fails += TestScene("10_World", report, scene =>
            {
                int local = 0;
                local += ExpectGameObject("Player", report);
                local += ExpectGameObject("Ground", report);
                local += ExpectGameObject("CameraRig", report);
                local += ExpectComponent<InteractionRouterBridge>("InteractionBridge", report);
                local += ExpectGameObject("Area_SanVanDong", report);
                local += ExpectGameObject("Area_LopHoc_Door", report);
                local += ExpectGameObject("NPC_DaiDoiTruong", report);

                var marker = FindAnyComponent<InteractableMarker>();
                if (marker == null)
                {
                    report.AppendLine("- FAIL InteractableMarker missing in scene");
                    local++;
                }
                else
                {
                    var so = new SerializedObject(marker);
                    var prop = so.FindProperty("interactable");
                    if (prop == null || prop.objectReferenceValue == null)
                    {
                        report.AppendLine($"- FAIL InteractableMarker on '{marker.gameObject.name}' has null interactable");
                        local++;
                    }
                    else
                    {
                        report.AppendLine($"- OK InteractableMarker on '{marker.gameObject.name}' -> {prop.objectReferenceValue.name}");
                    }
                }
                return local;
            });

            fails += TestScene("99_Ending", report, scene =>
            {
                int local = 0;
                local += ExpectGameObject("EndingCanvas", report);
                return local;
            });

            report.AppendLine();
            report.AppendLine(fails == 0 ? "## RESULT: PASS" : $"## RESULT: FAIL ({fails} issues)");
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log($"[SmokeTest] wrote {ReportPath}, fails={fails}");

            if (!string.IsNullOrEmpty(prevScene) && File.Exists(prevScene))
                EditorSceneManager.OpenScene(prevScene);
        }

        static int TestScene(string sceneName, StringBuilder report, System.Func<Scene, int> body)
        {
            report.AppendLine($"## Scene `{sceneName}`");
            string path = $"{SceneFolder}/{sceneName}.unity";
            if (!File.Exists(path))
            {
                report.AppendLine($"- FAIL scene file missing: {path}");
                return 1;
            }
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int n = body(scene);
            if (n == 0) report.AppendLine($"- OK all checks passed");
            return n;
        }

        static int ExpectGameObject(string name, StringBuilder report)
        {
            var go = GameObject.Find(name);
            if (go == null) { report.AppendLine($"- FAIL GameObject `{name}` missing"); return 1; }
            return 0;
        }

        static int ExpectComponent<T>(string goName, StringBuilder report) where T : Component
        {
            var go = GameObject.Find(goName);
            if (go == null) { report.AppendLine($"- FAIL GameObject `{goName}` missing"); return 1; }
            var c = go.GetComponent<T>();
            if (c == null) { report.AppendLine($"- FAIL `{goName}` missing {typeof(T).Name}"); return 1; }
            return 0;
        }

        static int ExpectSerializedFieldNonNull<T>(string goName, string fieldName, StringBuilder report) where T : Component
        {
            var go = GameObject.Find(goName);
            if (go == null) return 0;
            var c = go.GetComponent<T>();
            if (c == null) return 0;
            var so = new SerializedObject(c);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { report.AppendLine($"- FAIL `{goName}.{typeof(T).Name}.{fieldName}` property not found"); return 1; }
            if (prop.objectReferenceValue == null) { report.AppendLine($"- FAIL `{goName}.{typeof(T).Name}.{fieldName}` is null"); return 1; }
            return 0;
        }

        static T FindAnyComponent<T>() where T : Component
        {
            var all = Object.FindObjectsByType<T>(FindObjectsSortMode.None);
            return all != null && all.Length > 0 ? all[0] : null;
        }
    }
}

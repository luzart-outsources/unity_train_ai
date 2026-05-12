using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrainAI.Presentation;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Editor
{
    public static class SceneBuilder
    {
        const string SceneFolder = "Assets/Scenes/TrainAI";

        static readonly string[] Scenes = {
            "00_Bootstrap", "01_MainMenu", "02_CutScene", "03_CreateChar",
            "10_World", "11_LopHoc", "12_NhaAn", "13_KyTucXa", "99_Ending"
        };

        [MenuItem("Tools/Build Game/5. Build Scenes", false, 105)]
        public static void BuildAll()
        {
            EnsureFolder(SceneFolder);
            var bp = BlueprintLoader.Load();
            var locator = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>("Assets/_Data/Config/ServiceLocator.asset");

            foreach (var sn in Scenes)
            {
                string path = $"{SceneFolder}/{sn}.unity";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuildSceneContent(sn, locator, bp);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log($"[SceneBuilder] saved {path}");
            }

            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] all scenes built");
        }

        static void BuildSceneContent(string sceneName, ServiceLocatorSO locator, WorldBlueprint bp)
        {
            new GameObject("DirectionalLight").AddComponent<Light>().type = LightType.Directional;
            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0, 5, -10);

            if (sceneName == "00_Bootstrap")
            {
                var bootGo = new GameObject("Bootstrap");
                var entry = bootGo.AddComponent<BootstrapEntry>();
                bootGo.AddComponent<GameLoopDriver>();
                AssignSerialized(entry, "services", locator);
            }
            else if (sceneName == "10_World" && bp != null)
            {
                BuildWorldCubes(bp);
            }
            else if (sceneName == "11_LopHoc" || sceneName == "12_NhaAn" || sceneName == "13_KyTucXa")
            {
                BuildSubScene(sceneName);
            }
            else if (sceneName == "99_Ending")
            {
                BuildEndingCanvas();
            }
        }

        static void BuildWorldCubes(WorldBlueprint bp)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);

            foreach (var a in bp.areas)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = $"Area_{a.id}";
                cube.transform.position = new Vector3(a.pos.x, a.pos.y + (a.size.y * 0.5f), a.pos.z);
                cube.transform.localScale = new Vector3(a.size.x, a.size.y, a.size.z);
                var box = cube.GetComponent<BoxCollider>();
                box.isTrigger = true;
                cube.AddComponent<InteractableMarker>();
            }
        }

        static void BuildSubScene(string name)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = $"Floor_{name}";
            floor.transform.localScale = new Vector3(2, 1, 2);
        }

        static void BuildEndingCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        static void RegisterBuildSettings()
        {
            var existing = EditorBuildSettings.scenes
                .Where(s => s != null && !string.IsNullOrEmpty(s.path)
                            && !s.path.StartsWith($"{SceneFolder}/"))
                .ToList();
            foreach (var sn in Scenes)
                existing.Add(new EditorBuildSettingsScene($"{SceneFolder}/{sn}.unity", true));
            EditorBuildSettings.scenes = existing.ToArray();
        }

        static void AssignSerialized(Component comp, string fieldName, Object value)
        {
            if (comp == null || value == null) return;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedProperties();
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(parent).Replace('\\', '/'),
                                           Path.GetFileName(parent));
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

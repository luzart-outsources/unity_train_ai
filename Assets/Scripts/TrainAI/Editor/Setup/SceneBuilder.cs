using System.Collections.Generic;
using Luzart;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Player;
using TrainAI.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace TrainAI.Editor.Setup
{
    public static class SceneBuilder
    {
        private const string ScenesRoot = "Assets/Scenes/TrainAI";

        public static void BuildAll(GameDatabaseSO db, UIRegistrySO registry, PrefabBuilder.BuiltPrefabs prefabs)
        {
            AssetScanner.EnsureFolder(ScenesRoot);

            BuildBootScene(db, registry, prefabs);
            BuildTitleScene(prefabs);
            BuildWorldScene(db, prefabs);
            BuildClassroomScene(prefabs);
            BuildDormitoryScene(prefabs);
            BuildCafeteriaScene(prefabs);

            // Add to Build Settings.
            AddScenesToBuildSettings(new[]
            {
                $"{ScenesRoot}/_Boot.unity",
                $"{ScenesRoot}/Title.unity",
                $"{ScenesRoot}/World.unity",
                $"{ScenesRoot}/Classroom.unity",
                $"{ScenesRoot}/Dormitory.unity",
                $"{ScenesRoot}/Cafeteria.unity",
            });
        }

        private static void BuildBootScene(GameDatabaseSO db, UIRegistrySO registry, PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootGo = new GameObject("[GameBootstrap]");
            var bootstrap = bootGo.AddComponent<GameBootstrap>();
            new SerializedObject(bootstrap).Apply(("database", db));

            // UIRoot canvas.
            var uiRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.UIRootCanvas, scene);
            uiRoot.transform.SetParent(null);

            // Add UIManager components on UIRoot.
            var uiManager = uiRoot.AddComponent<UIManager>();
            // Wire fields.
            var so = new SerializedObject(uiManager);
            so.FindProperty("registry").objectReferenceValue = registry;
            so.FindProperty("worldOverlayRoot").objectReferenceValue = uiRoot.transform.Find("0_WorldOverlay");
            so.FindProperty("screenRoot").objectReferenceValue = uiRoot.transform.Find("1_Screen");
            so.FindProperty("hudRoot").objectReferenceValue = uiRoot.transform.Find("2_Hud");
            so.FindProperty("popupRoot").objectReferenceValue = uiRoot.transform.Find("3_Popup");
            so.FindProperty("systemRoot").objectReferenceValue = uiRoot.transform.Find("4_System");
            so.FindProperty("toastRoot").objectReferenceValue = uiRoot.transform.Find("5_Toast");
            so.FindProperty("preloadOnStart").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            // EventSystem.
            EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/_Boot.unity");
        }

        private static void BuildTitleScene(PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 1, -10);
            camGo.AddComponent<AudioListener>();

            EnsureEventSystem();

            // Auto show MainMenu helper.
            var helper = new GameObject("[ShowMainMenuOnStart]");
            helper.AddComponent<TrainAI.UI.ShowOnStart>().UIIdInt = (int)UIId.MainMenu;

            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/Title.unity");
        }

        private static void BuildWorldScene(GameDatabaseSO db, PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Ground 50x50.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            var groundRen = ground.GetComponent<MeshRenderer>();
            if (groundRen != null && groundRen.sharedMaterial != null)
            {
                var mat = new Material(groundRen.sharedMaterial);
                mat.color = new Color(0.5f, 0.7f, 0.4f);
                groundRen.material = mat;
            }

            // Light.
            var lightGo = new GameObject("Directional Light");
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Camera follow.
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<PlayerCameraFollow>();
            cam.transform.position = new Vector3(0, 12, -8);
            cam.transform.LookAt(Vector3.zero);

            // Player spawn.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player, scene);
            player.transform.position = new Vector3(0, 1, 0);
            new SerializedObject(follow).Apply(("target", player.transform));

            // Joystick widget exist trong GameplayHud, PlayerController.joystick wire trong scene runtime.
            // Hard to wire here vi joystick prefab khac scene -> can scene runtime helper. Skip, joystick = null.

            // Spawn interactables.
            SpawnInteractable(scene, prefabs.Interactable, db, "SanVanDong", new Vector3(-15, 0, 5));
            SpawnInteractable(scene, prefabs.Interactable, db, "DonVeSinh", new Vector3(15, 0, 5));
            SpawnInteractable(scene, prefabs.Interactable, db, "CuaLopHoc", new Vector3(0, 0, 15));
            SpawnInteractable(scene, prefabs.Interactable, db, "CuaKyTucXa", new Vector3(-15, 0, -10));
            SpawnInteractable(scene, prefabs.Interactable, db, "CuaNhaAn", new Vector3(15, 0, -10));

            // NPC dai doi truong.
            var npc = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.NPC, scene);
            npc.transform.position = new Vector3(0, 1, -10);
            var trigger = npc.GetComponentInChildren<InteractableTrigger>();
            if (trigger != null)
            {
                var npcInter = FindInteractableByKey(db, "NPC_DaiDoiTruong");
                if (npcInter != null) trigger.SetData(npcInter);
            }
            var npcCtl = npc.GetComponent<NPCController>();
            if (npcCtl != null && db != null && db.npcs != null && db.npcs.Count > 0)
                new SerializedObject(npcCtl).Apply(("profile", db.npcs[0]));

            EnsureEventSystem();

            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/World.unity");
        }

        private static void BuildClassroomScene(PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBasicScene("Lop hoc", new Color(0.6f, 0.5f, 0.4f), prefabs, scene, db: null,
                interactSpots: new[] {
                    ("BanHoc", new Vector3(-3, 0, 2)),
                    ("BanHoc", new Vector3(3, 0, 2)),
                    ("BanHoc", new Vector3(-3, 0, -2)),
                    ("BanHoc", new Vector3(3, 0, -2)),
                });
            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/Classroom.unity");
        }

        private static void BuildDormitoryScene(PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBasicScene("Ky tuc xa", new Color(0.4f, 0.4f, 0.6f), prefabs, scene, db: null,
                interactSpots: new[] {
                    ("Giuong", new Vector3(-3, 0, 0)),
                    ("Giuong", new Vector3(3, 0, 0)),
                });
            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/Dormitory.unity");
        }

        private static void BuildCafeteriaScene(PrefabBuilder.BuiltPrefabs prefabs)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateBasicScene("Nha an", new Color(0.7f, 0.6f, 0.4f), prefabs, scene, db: null,
                interactSpots: new[] {
                    ("BanAn", new Vector3(-3, 0, 0)),
                    ("BanAn", new Vector3(3, 0, 0)),
                });
            EditorSceneManager.SaveScene(scene, $"{ScenesRoot}/Cafeteria.unity");
        }

        private static void CreateBasicScene(string label, Color groundColor, PrefabBuilder.BuiltPrefabs prefabs,
            UnityEngine.SceneManagement.Scene scene, GameDatabaseSO db, (string key, Vector3 pos)[] interactSpots)
        {
            // Ground 20x20.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(2, 1, 2);
            var groundRen = ground.GetComponent<MeshRenderer>();
            if (groundRen != null && groundRen.sharedMaterial != null)
            {
                var mat = new Material(groundRen.sharedMaterial);
                mat.color = groundColor;
                groundRen.material = mat;
            }

            // Light.
            var lightGo = new GameObject("Directional Light");
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Camera.
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            cam.transform.position = new Vector3(0, 8, -6);
            cam.transform.LookAt(Vector3.zero);
            var follow = camGo.AddComponent<PlayerCameraFollow>();

            // Player.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(prefabs.Player, scene);
            player.transform.position = new Vector3(0, 1, -5);
            new SerializedObject(follow).Apply(("target", player.transform));

            // Interactables.
            var dbInst = GameDatabaseSORuntimeFinder.Find();
            foreach (var (key, pos) in interactSpots)
            {
                SpawnInteractable(scene, prefabs.Interactable, dbInst, key, pos);
            }

            // Door back to World.
            SpawnInteractable(scene, prefabs.Interactable, dbInst, "BackToWorld", new Vector3(0, 0, -8), backToWorldFallback: true);

            EnsureEventSystem();
        }

        private static void SpawnInteractable(UnityEngine.SceneManagement.Scene scene, GameObject prefab,
            GameDatabaseSO db, string locationKey, Vector3 pos, bool backToWorldFallback = false)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.position = pos;
            go.name = $"Interactable_{locationKey}";

            var trigger = go.GetComponent<InteractableTrigger>();
            if (trigger == null) return;

            InteractableSO so = FindInteractableByKey(db, locationKey);
            if (so == null && backToWorldFallback)
            {
                // Tao 1 InteractableSO inline cho cua ve World - se KHONG persist (in-memory).
                // Tot hon: tao asset trong setup, nhung de don gian skip - log warning.
                Debug.LogWarning($"[SceneBuilder] No InteractableSO for key '{locationKey}', spawning placeholder.");
                return;
            }
            if (so != null) trigger.SetData(so);
        }

        private static InteractableSO FindInteractableByKey(GameDatabaseSO db, string key)
        {
            if (db == null || db.interactables == null) return null;
            for (int i = 0; i < db.interactables.Count; i++)
                if (db.interactables[i] != null && db.interactables[i].key == key)
                    return db.interactables[i];
            return null;
        }

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static void AddScenesToBuildSettings(string[] scenePaths)
        {
            var existing = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var p in scenePaths)
            {
                bool found = false;
                for (int i = 0; i < existing.Count; i++)
                {
                    if (existing[i].path == p) { found = true; existing[i] = new EditorBuildSettingsScene(p, true); break; }
                }
                if (!found) existing.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = existing.ToArray();
        }
    }

    // Helper to lookup database from asset path.
    internal static class GameDatabaseSORuntimeFinder
    {
        public static GameDatabaseSO Find()
        {
            return AssetDatabase.LoadAssetAtPath<GameDatabaseSO>("Assets/Configs/TrainAI/_GameDatabase.asset");
        }
    }
}

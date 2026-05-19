using System.IO;
using System.Linq;
using TMPro;
using TrainAI.Presentation;
using TrainAI.Services;
using TrainAI.SO.Base;
using TrainAI.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainAI.Editor
{
    public static class SceneBuilder
    {
        const string SceneFolder = "Assets/Scenes/TrainAI";
        const string PrefabFolder = "Assets/Prefabs/TrainAI";
        const string ConfigFolder = "Assets/_Data/Config";
        const string MaterialFolder = "Assets/_Data/Materials";
        const string RTPath = "Assets/_Data/Config/MinimapRT.renderTexture";
        const string CameraConfigPath = "Assets/_Data/Config/ThirdPersonCameraConfig.asset";

        // Pack prefabs that we instantiate by path (no compile-time dependency).
        // Fixed Joystick: always visible at its anchor (vs Floating which hides until tap).
        const string FloatingJoystickPath = "Assets/Joystick Pack/Prefabs/Fixed Joystick.prefab";
        const string ArtFarmhouse = "Assets/Imported Asset/PolygonFarm/Prefabs/Buildings/SM_Bld_Farmhouse_01.prefab";
        const string ArtBarn = "Assets/Imported Asset/PolygonFarm/Prefabs/Buildings/SM_Bld_Barn_01.prefab";
        const string ArtGreenhouse = "Assets/Imported Asset/PolygonFarm/Prefabs/Buildings/SM_Bld_Greenhouse_01.prefab";

        // Canvas sortingOrder layers — gives a deterministic vertical stack
        // so HUD never floats over menus and modals always sit on top.
        const int SortOrder_HUD = 50;
        const int SortOrder_SceneMenu = 100;
        const int SortOrder_Modal = 200;

        static readonly string[] Scenes = {
            "00_Bootstrap", "01_MainMenu", "02_CutScene", "03_CreateChar",
            "10_World", "11_LopHoc", "12_NhaAn", "13_KyTucXa", "99_Ending"
        };

        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";

        [MenuItem("Tools/Build Game/5. Build Scenes", false, 105)]
        public static void BuildAll()
        {
            EnsureFolder(SceneFolder);
            EnsureFolder(ConfigFolder);
            EnsureFolder(MaterialFolder);
            MaterialPalette.EnsureAll(MaterialFolder);
            EnsureCameraConfig();
            EnsureMinimapRT();
            AssetDatabase.Refresh();
            var bp = BlueprintLoader.Load();

            foreach (var sn in Scenes)
            {
                string path = $"{SceneFolder}/{sn}.unity";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var locator = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
                if (locator == null)
                    Debug.LogError($"[SceneBuilder] locator null for scene {sn}");
                BuildSceneContent(sn, locator, bp);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, path);
                Debug.Log($"[SceneBuilder] saved {path}");
            }

            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SceneBuilder] all scenes built");
        }

        // ====================================================================
        // shared assets
        // ====================================================================
        static ThirdPersonCameraConfigSO EnsureCameraConfig()
        {
            var cfg = AssetDatabase.LoadAssetAtPath<ThirdPersonCameraConfigSO>(CameraConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<ThirdPersonCameraConfigSO>();
                AssetDatabase.CreateAsset(cfg, CameraConfigPath);
            }
            cfg.distance = 9f;
            cfg.height = 4f;
            cfg.yawSpeed = 140f;
            cfg.pitchMin = -5f;
            cfg.pitchMax = 55f;
            cfg.smoothTime = 0.08f;
            cfg.invertY = false;
            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        static RenderTexture EnsureMinimapRT()
        {
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
            if (rt == null)
            {
                rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
                {
                    name = "MinimapRT",
                    antiAliasing = 1,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                AssetDatabase.CreateAsset(rt, RTPath);
            }
            EditorUtility.SetDirty(rt);
            return rt;
        }

        static void BuildSceneContent(string sceneName, ServiceLocatorSO locator, WorldBlueprint bp)
        {
            if (sceneName != "00_Bootstrap") BuildLightCamera();
            BuildEventSystem();

            switch (sceneName)
            {
                case "00_Bootstrap": BuildBootstrap(locator); break;
                case "01_MainMenu":  BuildMainMenu(locator); break;
                case "02_CutScene":  /* placeholder */ break;
                case "03_CreateChar": BuildCreateChar(locator); break;
                case "10_World":     BuildWorld(locator, bp); break;
                case "11_LopHoc":
                case "12_NhaAn":
                case "13_KyTucXa":   BuildSubScene(sceneName, locator); break;
                case "99_Ending":    BuildEnding(); break;
            }
        }

        static void BuildLightCamera()
        {
            var lightGo = new GameObject("DirectionalLight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45, -25, 0);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;

            var camGo = new GameObject("MainCamera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 5, -10);
            camGo.transform.LookAt(Vector3.zero);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.70f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.25f, 0.20f);
        }

        static void BuildEventSystem()
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // ====================================================================
        // 00_Bootstrap
        // ====================================================================
        static void BuildBootstrap(ServiceLocatorSO locator)
        {
            var bootGo = new GameObject("Bootstrap");
            var entry = bootGo.AddComponent<BootstrapEntry>();
            var loop = bootGo.AddComponent<GameLoopDriver>();
            AssignSerialized(entry, "services", locator);
            AssignSerialized(loop, "services", locator);
            AssignSerialized(entry, "firstScene",
                LoadAsset<SceneRefSO>("Assets/_Data/Scenes/SceneRef_01_MainMenu.asset"));

            BuildPersistentUI(locator);
        }

        // Layout:
        //   UICanvas (Canvas sortingOrder=0, holds UIRouterMono + DontDestroyOnLoad)
        //     HUDCanvas   (Canvas sortingOrder=50,  enabled only on gameplay scenes)
        //     ModalCanvas (Canvas sortingOrder=200, always available, panels hidden until shown)
        static void BuildPersistentUI(ServiceLocatorSO locator)
        {
            var uiRoot = BuildCanvas("UICanvas", sortOrder: 0);

            var router = uiRoot.AddComponent<UIRouterMono>();
            AssignSerialized(router, "services", locator);

            var hudRoot = BuildSubCanvas(uiRoot.transform, "HUDCanvas", SortOrder_HUD);
            var hudCanvas = hudRoot.GetComponent<Canvas>();
            var hudGroup = hudRoot.AddComponent<CanvasGroup>();
            var hudGate = hudRoot.AddComponent<SceneAwareHUDRoot>();
            AssignSerialized(hudGate, "hudCanvas", hudCanvas);
            AssignSerialized(hudGate, "hudGroup", hudGroup);

            var modalRoot = BuildSubCanvas(uiRoot.transform, "ModalCanvas", SortOrder_Modal);

            // Modals — full-screen dim + centered popup, hidden by default (UIScreenBase.Awake).
            var confirm = BuildConfirmScreen(modalRoot.transform);
            var dialogue = BuildDialogueScreen(modalRoot.transform);
            var loading = BuildLoadingScreen(modalRoot.transform);
            var quiz = BuildQuizScreen(modalRoot.transform);
            var ending = BuildEndingScreen(modalRoot.transform);
            var expel = BuildExpelScreen(modalRoot.transform);

            // HUD chips — anchored to corners of HUDCanvas, sci-fi chip background.
            BuildQuestHUD(hudRoot.transform);
            BuildClockHUD(hudRoot.transform, locator);
            BuildScoreHUD(hudRoot.transform, locator);
            BuildInteractPrompt(hudRoot.transform);
            BuildInteractButton(hudRoot.transform);
            BuildMiniMap(hudRoot.transform, locator);
            BuildJoystick(hudRoot.transform);

            AssignSerialized(router, "confirm", confirm);
            AssignSerialized(router, "dialogue", dialogue);
            AssignSerialized(router, "loading", loading);
            AssignSerialized(router, "quiz", quiz);
            AssignSerialized(router, "ending", ending);
            AssignSerialized(router, "expel", expel);

            // Hide modal hosts in the editor preview without breaking the runtime
            // Show()/Hide() cycle. We must keep gameObject.activeSelf=true at save
            // time so UIScreenBase.Awake() can run; we just zero the CanvasGroup so
            // nothing is drawn or clickable until Show() flips it back to 1.
            foreach (var modal in new MonoBehaviour[] { confirm, dialogue, loading, quiz, ending, expel })
            {
                if (modal == null) continue;
                var cg = modal.GetComponent<CanvasGroup>();
                if (cg != null) { cg.alpha = 0f; cg.blocksRaycasts = false; cg.interactable = false; }
            }
        }

        // ====================================================================
        // 01_MainMenu
        // ====================================================================
        static void BuildMainMenu(ServiceLocatorSO locator)
        {
            var canvas = BuildCanvas("MainMenuCanvas", sortOrder: SortOrder_SceneMenu);
            SciFiTheme.AddSceneBackdrop(canvas.transform);

            var card = BuildCard(canvas.transform, "MainMenuCard", new Vector2(720, 720));

            BuildHeaderText(card.transform, "Title", "TRAIN AI",
                            new Vector2(0, 260), new Vector2(640, 90), 56);
            BuildBodyText(card.transform, "Subtitle", "Hoc ky quan su",
                          new Vector2(0, 190), new Vector2(640, 50), 24, SciFiTheme.TextMuted);

            var newBtn = BuildPrimaryButton(card.transform, "NewGameButton", "Bat dau moi",
                                            new Vector2(0, 40), new Vector2(360, 78));
            var contBtn = BuildPrimaryButton(card.transform, "ContinueButton", "Tiep tuc",
                                             new Vector2(0, -50), new Vector2(360, 78));
            var exitBtn = BuildSecondaryButton(card.transform, "ExitButton", "Thoat",
                                               new Vector2(0, -140), new Vector2(360, 78));

            BuildBodyText(card.transform, "Hint", "Hoc tap + Ren luyen | Ban di chuyen bang WASD / phim mui ten",
                          new Vector2(0, -260), new Vector2(640, 40), 16, SciFiTheme.TextMuted);

            var ctrl = canvas.AddComponent<UIMainMenuController>();
            AssignSerialized(ctrl, "services", locator);
            AssignSerialized(ctrl, "newGameButton", newBtn);
            AssignSerialized(ctrl, "continueButton", contBtn);
            AssignSerialized(ctrl, "exitButton", exitBtn);
            AssignSerialized(ctrl, "createCharScene",
                LoadAsset<SceneRefSO>("Assets/_Data/Scenes/SceneRef_03_CreateChar.asset"));
            AssignSerialized(ctrl, "worldScene",
                LoadAsset<SceneRefSO>("Assets/_Data/Scenes/SceneRef_10_World.asset"));
        }

        // ====================================================================
        // 03_CreateChar
        // ====================================================================
        static void BuildCreateChar(ServiceLocatorSO locator)
        {
            var canvas = BuildCanvas("CreateCharCanvas", sortOrder: SortOrder_SceneMenu);
            SciFiTheme.AddSceneBackdrop(canvas.transform);

            var card = BuildCard(canvas.transform, "CreateCharCard", new Vector2(800, 480));

            BuildHeaderText(card.transform, "Header", "TAO HO SO HOC VIEN",
                            new Vector2(0, 150), new Vector2(700, 80), 38);
            BuildBodyText(card.transform, "Sub", "Nhap ten cua ban truoc khi nhap hoc",
                          new Vector2(0, 90), new Vector2(700, 40), 18, SciFiTheme.TextMuted);

            var input = BuildInputFieldChild(card.transform, "NameInput",
                                              new Vector2(0, 10), new Vector2(520, 64));
            var confirmBtn = BuildPrimaryButton(card.transform, "ConfirmButton", "Xac nhan",
                                                 new Vector2(0, -90), new Vector2(280, 72));

            var ctrl = canvas.AddComponent<UICreateCharController>();
            AssignSerialized(ctrl, "services", locator);
            AssignSerialized(ctrl, "playerState", locator != null ? locator.playerState : null);
            AssignSerialized(ctrl, "clock", locator != null ? locator.clock : null);
            AssignSerialized(ctrl, "nameInput", input);
            AssignSerialized(ctrl, "confirmButton", confirmBtn);
            AssignSerialized(ctrl, "worldScene",
                LoadAsset<SceneRefSO>("Assets/_Data/Scenes/SceneRef_10_World.asset"));
        }

        // ====================================================================
        // 10_World
        // ====================================================================
        static void BuildWorld(ServiceLocatorSO locator, WorldBlueprint bp)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(12, 1, 12);
            ApplyMaterial(ground, MaterialPalette.Ground(MaterialFolder));

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Player.prefab");
            GameObject player = null;
            if (playerPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = "Player";
                player.transform.position = new Vector3(0, 0.05f, 0);
                if (locator != null)
                {
                    var pc = player.GetComponent<PlayerController>();
                    if (pc != null) AssignSerialized(pc, "playerState", locator.playerState);
                }
            }

            var existingCam = GameObject.Find("MainCamera");
            if (existingCam != null)
            {
                var rigGo = new GameObject("CameraRig");
                rigGo.transform.position = player != null ? player.transform.position : Vector3.zero;
                existingCam.transform.SetParent(rigGo.transform, false);
                existingCam.transform.localPosition = Vector3.zero;
                existingCam.transform.localRotation = Quaternion.identity;
                var rig = rigGo.AddComponent<ThirdPersonCameraRig>();
                AssignSerialized(rig, "target", player != null ? player.transform : null);
                AssignSerialized(rig, "cam", existingCam.GetComponent<Camera>());
                AssignSerialized(rig, "config", EnsureCameraConfig());

                if (player != null)
                {
                    var pc = player.GetComponent<PlayerController>();
                    if (pc != null) AssignSerialized(pc, "cameraRig", rigGo.transform);
                }
            }

            if (bp != null)
            {
                foreach (var a in bp.areas)
                {
                    var inter = AssetDatabase.LoadAssetAtPath<InteractableSO>(
                        $"Assets/_Data/Interactables/Interactable_{a.id}.asset");
                    if (inter == null) continue;
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.name = $"Area_{a.id}";
                    cube.transform.position = new Vector3(a.pos.x, a.pos.y + a.size.y * 0.5f, a.pos.z);
                    cube.transform.localScale = new Vector3(a.size.x, a.size.y, a.size.z);
                    var box = cube.GetComponent<BoxCollider>();
                    box.isTrigger = true;
                    var marker = cube.AddComponent<InteractableMarker>();
                    AssignSerialized(marker, "interactable", inter);

                    if (IsDoor(a.id))
                        BuildDoorBuilding(a);

                    Material areaMat = IsDoor(a.id) ? MaterialPalette.Door(MaterialFolder)
                                     : IsFreeArea(a.id) ? MaterialPalette.FreeArea(MaterialFolder)
                                     : MaterialPalette.Area(MaterialFolder);
                    ApplyMaterial(cube, areaMat);
                }
            }

            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/NPC.prefab");
            if (locator != null && locator.npcDB != null && npcPrefab != null)
            {
                foreach (var npcSo in locator.npcDB.all)
                {
                    if (npcSo == null) continue;
                    var npcGo = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
                    npcGo.name = $"NPC_{npcSo.id}";
                    var sp = npcSo.spawnPos;
                    if (sp.y < 0.01f) sp.y = 0.05f;
                    npcGo.transform.position = sp;
                    var view = npcGo.GetComponent<NpcView>();
                    if (view != null)
                    {
                        AssignSerialized(view, "npcDef", npcSo);
                        AssignSerialized(view, "services", locator);
                    }
                }
            }

            var bridgeGo = new GameObject("InteractionBridge");
            var bridge = bridgeGo.AddComponent<InteractionRouterBridge>();
            AssignSerialized(bridge, "services", locator);

            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/QuestArrow.prefab");
            if (arrowPrefab != null && player != null)
            {
                var arrow = (GameObject)PrefabUtility.InstantiatePrefab(arrowPrefab);
                arrow.transform.SetParent(player.transform, false);
                arrow.transform.localPosition = Vector3.zero;
                arrow.transform.localRotation = Quaternion.identity;
            }

            BuildMinimapCamera(player);
        }

        static void BuildMinimapCamera(GameObject player)
        {
            var rt = EnsureMinimapRT();
            var camGo = new GameObject("MinimapCamera");
            if (player != null)
            {
                camGo.transform.SetParent(player.transform, false);
                camGo.transform.localPosition = new Vector3(0, 40f, 0);
            }
            else
            {
                camGo.transform.position = new Vector3(0, 40f, 0);
            }
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 30f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.18f, 0.10f, 1f);
            cam.targetTexture = rt;
            cam.depth = -10;
        }

        static bool IsDoor(string id) => !string.IsNullOrEmpty(id) && id.EndsWith("_Door");
        static bool IsFreeArea(string id) => id == "FreeArea";

        // World-space label floating above areas/NPCs. Kept small enough not to dominate
        // the camera frame — the previous 0.35 scale + fontSize 1.2 read as a giant
        // billboard at distance 6.
        static void BuildSignLabel(Transform parent, string text, float topY)
        {
            var labelGo = new GameObject("Sign");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0, topY + 0.4f, 0);
            labelGo.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

            var canvas = labelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            labelGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            labelGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var rect = labelGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(3f, 0.6f);
            rect.localScale = Vector3.one * 0.08f;

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(labelGo.transform, false);
            var tRect = tGo.AddComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(3f, 0.6f);
            tRect.anchoredPosition = Vector2.zero;
            var tmp = tGo.AddComponent<TextMeshProUGUI>();
            tmp.text = HumanizeId(text);
            tmp.fontSize = 0.45f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 1f);
        }

        static string HumanizeId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return id.Replace('_', ' ');
        }

        static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        // ====================================================================
        // sub-scenes
        // ====================================================================
        static void BuildSubScene(string sceneName, ServiceLocatorSO locator)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = $"Floor_{sceneName}";
            floor.transform.localScale = new Vector3(2, 1, 2);
            ApplyMaterial(floor, MaterialPalette.Ground(MaterialFolder));

            BuildWall(new Vector3(0, 1.5f,  10), new Vector3(20, 3, 0.4f), "Wall_N");
            BuildWall(new Vector3(0, 1.5f, -10), new Vector3(20, 3, 0.4f), "Wall_S");
            BuildWall(new Vector3( 10, 1.5f, 0), new Vector3(0.4f, 3, 20), "Wall_E");
            BuildWall(new Vector3(-10, 1.5f, 0), new Vector3(0.4f, 3, 20), "Wall_W");

            var spawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawn.name = "PlayerSpawn";
            spawn.transform.position = new Vector3(0, -0.5f, -8);
            spawn.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            GameObject.DestroyImmediate(spawn.GetComponent<Collider>());

            switch (sceneName)
            {
                case "11_LopHoc": BuildLopHoc(); break;
                case "12_NhaAn":  BuildNhaAn(); break;
                case "13_KyTucXa": BuildKyTucXa(); break;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Interactable_Center";
            cube.transform.position = new Vector3(0, 0.5f, 2);
            cube.transform.localScale = new Vector3(2, 1, 2);
            var box = cube.GetComponent<BoxCollider>();
            box.isTrigger = true;
            cube.AddComponent<InteractableMarker>();
            ApplyMaterial(cube, MaterialPalette.Area(MaterialFolder));
        }

        static void BuildWall(Vector3 pos, Vector3 scale, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            ApplyMaterial(wall, MaterialPalette.Door(MaterialFolder));
        }

        static void BuildBox(Vector3 pos, Vector3 scale, string name, Material mat)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = pos;
            box.transform.localScale = scale;
            if (mat != null) ApplyMaterial(box, mat);
        }

        static void BuildLopHoc()
        {
            BuildBox(new Vector3(0, 2f, 9.5f), new Vector3(6f, 2f, 0.1f), "Blackboard", MaterialPalette.NpcHat(MaterialFolder));
            BuildBox(new Vector3(0, 0.5f, 6.5f), new Vector3(3f, 1f, 1.2f), "TeacherDesk", MaterialPalette.Door(MaterialFolder));
            for (int row = 0; row < 4; row++)
            for (int col = 0; col < 3; col++)
            {
                float x = (col - 1) * 3.0f;
                float z = 2.0f - row * 2.0f;
                BuildBox(new Vector3(x, 0.45f, z), new Vector3(1.6f, 0.9f, 0.8f), $"Desk_{row}_{col}", MaterialPalette.Door(MaterialFolder));
                BuildBox(new Vector3(x, 0.25f, z - 1.0f), new Vector3(0.7f, 0.5f, 0.7f), $"Chair_{row}_{col}", MaterialPalette.NpcHat(MaterialFolder));
            }
        }

        static void BuildNhaAn()
        {
            BuildBox(new Vector3(0, 0.6f, 7f), new Vector3(8f, 1.2f, 1.5f), "FoodCounter", MaterialPalette.Door(MaterialFolder));
            BuildBox(new Vector3(0, 1.4f, 8f), new Vector3(8f, 0.4f, 0.2f), "CounterBackboard", MaterialPalette.NpcHat(MaterialFolder));
            for (int i = 0; i < 4; i++)
                BuildBox(new Vector3(-3f + i * 2f, 1.3f, 6.6f), new Vector3(1.2f, 0.05f, 0.8f), $"Tray_{i}", MaterialPalette.Arrow(MaterialFolder));
            for (int row = 0; row < 3; row++)
            {
                float z = 2.0f - row * 3.0f;
                BuildBox(new Vector3(0, 0.45f, z), new Vector3(12f, 0.9f, 1.2f), $"DiningTable_{row}", MaterialPalette.Door(MaterialFolder));
                BuildBox(new Vector3(0, 0.22f, z - 1.0f), new Vector3(12f, 0.45f, 0.5f), $"Bench_N_{row}", MaterialPalette.NpcHat(MaterialFolder));
                BuildBox(new Vector3(0, 0.22f, z + 1.0f), new Vector3(12f, 0.45f, 0.5f), $"Bench_S_{row}", MaterialPalette.NpcHat(MaterialFolder));
            }
        }

        static void BuildKyTucXa()
        {
            for (int i = 0; i < 4; i++)
            {
                float z = 6f - i * 4f;
                BuildBox(new Vector3(-7f, 0.5f, z), new Vector3(2.5f, 1f, 3.5f), $"BunkLower_W_{i}", MaterialPalette.Door(MaterialFolder));
                BuildBox(new Vector3(-7f, 0.95f, z), new Vector3(2.3f, 0.2f, 3.3f), $"PillowSheet_W_{i}", MaterialPalette.Player(MaterialFolder));
                BuildBox(new Vector3(-7f, 2.0f, z), new Vector3(2.5f, 1f, 3.5f), $"BunkUpper_W_{i}", MaterialPalette.Door(MaterialFolder));
                BuildBox(new Vector3( 7f, 0.5f, z), new Vector3(2.5f, 1f, 3.5f), $"BunkLower_E_{i}", MaterialPalette.Door(MaterialFolder));
                BuildBox(new Vector3( 7f, 0.95f, z), new Vector3(2.3f, 0.2f, 3.3f), $"PillowSheet_E_{i}", MaterialPalette.Player(MaterialFolder));
                BuildBox(new Vector3( 7f, 2.0f, z), new Vector3(2.5f, 1f, 3.5f), $"BunkUpper_E_{i}", MaterialPalette.Door(MaterialFolder));
            }
            for (int i = 0; i < 6; i++)
                BuildBox(new Vector3(-6f + i * 2.4f, 1.5f, 9f), new Vector3(2.0f, 3f, 0.6f), $"Locker_{i}", MaterialPalette.NpcHat(MaterialFolder));
        }

        // Pick a PolygonFarm building per door so each sub-scene reads as a distinct place
        // (Farmhouse=LopHoc, Greenhouse=NhaAn, Barn=KyTucXa). Falls back to a tinted cube
        // if the artist pack is missing, so blueprint changes never break scene build.
        static void BuildDoorBuilding(AreaBlueprint a)
        {
            string artistPath = PickBuildingForDoor(a.id);
            Vector3 doorPos = new Vector3(a.pos.x, 0f, a.pos.z);
            Vector3 buildingPos = doorPos + new Vector3(0f, 0f, -5f);

            var artist = AssetDatabase.LoadAssetAtPath<GameObject>(artistPath);
            if (artist != null)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(artist);
                b.name = $"Building_{a.id}";
                b.transform.position = buildingPos;
                b.transform.rotation = Quaternion.Euler(0, 180, 0); // face the door (player side)
                EnsureMeshColliders(b);
                return;
            }

            Debug.LogWarning($"[SceneBuilder] Building art missing at {artistPath}; cube fallback.");
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"Building_{a.id}";
            cube.transform.position = buildingPos + new Vector3(0f, 2f, 0f);
            cube.transform.localScale = new Vector3(7f, 4f, 5f);
            ApplyMaterial(cube, MaterialPalette.Area(MaterialFolder));
        }

        static string PickBuildingForDoor(string id)
        {
            if (id == "LopHoc_Door") return ArtFarmhouse;   // Classroom
            if (id == "NhaAn_Door")  return ArtGreenhouse;  // Cafeteria
            if (id == "KTX_Door")    return ArtBarn;        // Dormitory
            return ArtFarmhouse;
        }

        // Building prefabs ship with MeshRenderers but sometimes lack colliders. Add one
        // MeshCollider per child MeshFilter so the player can't walk through the walls.
        static void EnsureMeshColliders(GameObject root)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(includeInactive: false))
            {
                if (mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.convex = false;
            }
        }

        // ====================================================================
        // 99_Ending
        // ====================================================================
        static void BuildEnding()
        {
            var canvas = BuildCanvas("EndingCanvas", sortOrder: SortOrder_SceneMenu);
            SciFiTheme.AddSceneBackdrop(canvas.transform);

            var card = BuildCard(canvas.transform, "EndingCard", new Vector2(900, 520));
            BuildHeaderText(card.transform, "Title", "HOAN THANH KHOA HOC",
                            new Vector2(0, 140), new Vector2(820, 90), 48);
            BuildBodyText(card.transform, "Sub", "Chuc mung dong chi da ket thuc hoc ky quan su",
                          new Vector2(0, 50), new Vector2(820, 60), 22, SciFiTheme.TextMuted);
        }

        // ====================================================================
        // Canvas helpers
        // ====================================================================
        static GameObject BuildCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        // A nested Canvas inside another Canvas — overrides sorting so it stacks
        // independently above/below sibling canvases.
        static GameObject BuildSubCanvas(Transform parent, string name, int sortOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var canvas = go.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortOrder;
            go.AddComponent<GraphicRaycaster>();
            return go;
        }

        static GameObject BuildPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>();
            return go;
        }

        // Centered sci-fi card with sliced panel sprite. popup_bg_01 reads cleaner than
        // _02 (less angular cut-out frame) and is the asset pack's main panel.
        static GameObject BuildCard(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = SciFiTheme.Load("popup_bg_01");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            return go;
        }

        // ====================================================================
        // Text helpers
        // ====================================================================
        static GameObject BuildTextChild(Transform parent, string name, string text,
                                          Vector2 anchoredPos, Vector2 size, int fontSize,
                                          Color? color = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? SciFiTheme.TextWhite;
            return go;
        }

        static GameObject BuildHeaderText(Transform parent, string name, string text,
                                          Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            var go = BuildTextChild(parent, name, text, anchoredPos, size, fontSize);
            SciFiTheme.StyleHeader(go.GetComponent<TextMeshProUGUI>(), fontSize);
            return go;
        }

        static GameObject BuildBodyText(Transform parent, string name, string text,
                                        Vector2 anchoredPos, Vector2 size, int fontSize,
                                        Color? color = null)
        {
            var go = BuildTextChild(parent, name, text, anchoredPos, size, fontSize, color);
            SciFiTheme.StyleBody(go.GetComponent<TextMeshProUGUI>(), fontSize);
            if (color.HasValue) go.GetComponent<TextMeshProUGUI>().color = color.Value;
            return go;
        }

        // ====================================================================
        // Button / input helpers
        // ====================================================================
        static Button BuildButtonChild(Transform parent, string name, string label,
                                        Vector2 anchoredPos, Vector2 size, bool primary = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var btn = go.AddComponent<Button>();

            BuildTextChild(go.transform, "Label", label, Vector2.zero, size, Mathf.Max(18, (int)(size.y * 0.34f)));
            SciFiTheme.StyleButton(btn, primary);
            return btn;
        }

        static Button BuildPrimaryButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
            => BuildButtonChild(parent, name, label, pos, size, primary: true);

        static Button BuildSecondaryButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
            => BuildButtonChild(parent, name, label, pos, size, primary: false);

        static TMP_InputField BuildInputFieldChild(Transform parent, string name,
                                                    Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = Color.white;
            var input = go.AddComponent<TMP_InputField>();

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(go.transform, false);
            var taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(20, 8);
            taRect.offsetMax = new Vector2(-20, -8);

            var textGo = BuildTextChild(textArea.transform, "Text", "",
                                         Vector2.zero, size - new Vector2(40, 16), 22);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            input.textComponent = tmp;
            input.fontAsset = tmp.font;
            SciFiTheme.StyleInputField(input);
            return input;
        }

        // ====================================================================
        // Modals (children of ModalCanvas)
        // ====================================================================
        // Each modal is a full-screen container with a sci-fi dim BG + a centered card.
        // Hidden by default via UIScreenBase.Awake -> Hide().
        static (GameObject panel, GameObject card) BuildModalPanel(Transform parent, string name, Vector2 cardSize)
        {
            var panel = BuildPanel(parent, name);
            SciFiTheme.StyleScreenDim(panel);
            var card = BuildCard(panel.transform, name + "Card", cardSize);
            return (panel, card);
        }

        static UIConfirmController BuildConfirmScreen(Transform parent)
        {
            var (panel, card) = BuildModalPanel(parent, "UIConfirm", new Vector2(720, 360));
            BuildHeaderText(card.transform, "Title", "XAC NHAN", new Vector2(0, 110), new Vector2(640, 60), 30);
            var body = BuildBodyText(card.transform, "Body", "...", new Vector2(0, 20), new Vector2(640, 120), 22);
            var ok = BuildPrimaryButton(card.transform, "OK", "OK", new Vector2(-130, -110), new Vector2(220, 70));
            var cancel = BuildSecondaryButton(card.transform, "Cancel", "Huy", new Vector2(130, -110), new Vector2(220, 70));

            var ctrl = panel.AddComponent<UIConfirmController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "okButton", ok);
            AssignSerialized(ctrl, "cancelButton", cancel);
            return ctrl;
        }

        static UIDialogueController BuildDialogueScreen(Transform parent)
        {
            var (panel, card) = BuildModalPanel(parent, "UIDialogue", new Vector2(1080, 600));
            var npcName = BuildHeaderText(card.transform, "NpcName", "NPC",
                                          new Vector2(0, 230), new Vector2(900, 60), 32);

            // Conversation history area with framed background.
            var histBg = new GameObject("HistoryBG");
            histBg.transform.SetParent(card.transform, false);
            var hbgRect = histBg.AddComponent<RectTransform>();
            hbgRect.sizeDelta = new Vector2(960, 320);
            hbgRect.anchoredPosition = new Vector2(0, 30);
            var hbgImg = histBg.AddComponent<Image>();
            hbgImg.sprite = SciFiTheme.Load("list_bg_n");
            hbgImg.type = Image.Type.Sliced;
            hbgImg.color = new Color(0f, 0.04f, 0.12f, 0.85f);

            var history = BuildBodyText(histBg.transform, "History", "",
                                         new Vector2(0, 0), new Vector2(920, 290), 20);
            var histText = history.GetComponent<TextMeshProUGUI>();
            histText.alignment = TextAlignmentOptions.TopLeft;
            var histRect = history.GetComponent<RectTransform>();
            histRect.anchorMin = new Vector2(0, 0);
            histRect.anchorMax = new Vector2(1, 1);
            histRect.offsetMin = new Vector2(20, 15);
            histRect.offsetMax = new Vector2(-20, -15);

            var input = BuildInputFieldChild(card.transform, "Input",
                                              new Vector2(-100, -200), new Vector2(720, 64));
            var send = BuildPrimaryButton(card.transform, "Send", "Gui",
                                           new Vector2(380, -200), new Vector2(160, 64));
            var close = BuildSecondaryButton(card.transform, "Close", "X",
                                              new Vector2(500, 235), new Vector2(64, 64));

            var ctrl = panel.AddComponent<UIDialogueController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "npcName", npcName.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "history", histText);
            AssignSerialized(ctrl, "input", input);
            AssignSerialized(ctrl, "sendButton", send);
            AssignSerialized(ctrl, "closeButton", close);
            return ctrl;
        }

        static UILoadingController BuildLoadingScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UILoading");
            SciFiTheme.StyleScreenDim(panel, alpha: 0.92f);

            var card = BuildCard(panel.transform, "UILoadingCard", new Vector2(600, 220));
            var label = BuildBodyText(card.transform, "Label", "Dang tai...",
                                       new Vector2(0, 30), new Vector2(540, 60), 26);

            // Animated progress strip — uses the loading_bar sprite as a filling band.
            var barBg = new GameObject("BarBG");
            barBg.transform.SetParent(card.transform, false);
            var barBgRect = barBg.AddComponent<RectTransform>();
            barBgRect.sizeDelta = new Vector2(460, 18);
            barBgRect.anchoredPosition = new Vector2(0, -40);
            var barBgImg = barBg.AddComponent<Image>();
            barBgImg.sprite = SciFiTheme.Load("loading_bar_bg");
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = new Color(0.05f, 0.10f, 0.22f, 1f);

            var bar = new GameObject("Bar");
            bar.transform.SetParent(barBg.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 1);
            barRect.offsetMin = new Vector2(2, 2);
            barRect.offsetMax = new Vector2(-2, -2);
            var barImg = bar.AddComponent<Image>();
            barImg.sprite = SciFiTheme.Load("loading_bar");
            barImg.type = Image.Type.Sliced;
            barImg.color = SciFiTheme.AccentCyan;

            var ctrl = panel.AddComponent<UILoadingController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "label", label.GetComponent<TextMeshProUGUI>());
            return ctrl;
        }

        static UIQuizController BuildQuizScreen(Transform parent)
        {
            var (panel, card) = BuildModalPanel(parent, "UIQuiz", new Vector2(1100, 720));

            var counter = BuildHeaderText(card.transform, "Counter", "1/10",
                                           new Vector2(-440, 300), new Vector2(180, 60), 26);
            var timer = BuildHeaderText(card.transform, "Timer", "15",
                                         new Vector2(440, 300), new Vector2(180, 60), 28);
            var question = BuildBodyText(card.transform, "Question", "Cau hoi?",
                                          new Vector2(0, 200), new Vector2(960, 140), 26);

            // UIQuizController lives on the panel so UIScreenBase.Hide() disables the
            // full modal (including the card and its buttons).
            var ctrl = panel.AddComponent<UIQuizController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "questionText", question.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "counterText", counter.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "timerText", timer.GetComponent<TextMeshProUGUI>());

            var buttons = new Button[4];
            var labels = new TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -240 : 240);
                float y = (i < 2 ? 50 : -60);
                var b = BuildButtonChild(card.transform, $"Ans{i}", $"Dap an {(char)('A' + i)}",
                                          new Vector2(x, y), new Vector2(440, 82));
                buttons[i] = b;
                var lbl = b.transform.Find("Label");
                if (lbl != null) labels[i] = lbl.GetComponent<TextMeshProUGUI>();
            }
            AssignSerializedArray(ctrl, "answerButtons", buttons);
            AssignSerializedArray(ctrl, "answerLabels", labels);

            var cont = BuildPrimaryButton(card.transform, "Continue", "Tiep theo",
                                           new Vector2(0, -230), new Vector2(280, 76));
            AssignSerialized(ctrl, "continueButton", cont);
            return ctrl;
        }

        static UIEndingController BuildEndingScreen(Transform parent)
        {
            var (panel, card) = BuildModalPanel(parent, "UIEnding", new Vector2(900, 560));
            var head = BuildHeaderText(card.transform, "Headline", "TOT NGHIEP",
                                        new Vector2(0, 150), new Vector2(820, 110), 56);
            var body = BuildBodyText(card.transform, "Body", "...",
                                      new Vector2(0, 0), new Vector2(820, 220), 24);
            var ctrl = panel.AddComponent<UIEndingController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "headlineText", head.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TextMeshProUGUI>());
            return ctrl;
        }

        static UIExpelController BuildExpelScreen(Transform parent)
        {
            var (panel, card) = BuildModalPanel(parent, "UIExpel", new Vector2(820, 420));

            // Override card to a red-tinted variant.
            var cardImg = card.GetComponent<Image>();
            if (cardImg != null) cardImg.color = new Color(1f, 0.78f, 0.78f, 1f);

            BuildHeaderText(card.transform, "Title", "DUOI HOC", new Vector2(0, 130), new Vector2(720, 70), 38);
            var body = BuildBodyText(card.transform, "Body", "...",
                                      new Vector2(0, -30), new Vector2(720, 200), 24, SciFiTheme.WarnRed);
            var ctrl = panel.AddComponent<UIExpelController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TextMeshProUGUI>());
            return ctrl;
        }

        // ====================================================================
        // HUDs (children of HUDCanvas, auto-hidden outside gameplay scenes)
        // ====================================================================
        // Quest panel — 4 lines visible at all times so the player always knows the
        // current objective, time window and remaining time without opening a quest log.
        // Layout (top→bottom): NHIEM VU header / title / objective hint / window + countdown.
        static void BuildQuestHUD(Transform parent)
        {
            var panel = BuildPanel(parent, "QuestHUD");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(440, 200);
            rect.anchoredPosition = new Vector2(-30, -30);
            SciFiTheme.StyleHUDChip(panel);

            var header = BuildBodyText(panel.transform, "Header", "NHIEM VU",
                                       new Vector2(0, 75), new Vector2(400, 28), 14, SciFiTheme.TextMuted);
            var title = BuildBodyText(panel.transform, "Title", "...",
                                       new Vector2(0, 42), new Vector2(400, 36), 22);
            title.GetComponent<TextMeshProUGUI>().color = SciFiTheme.AccentCyan;
            title.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var objective = BuildBodyText(panel.transform, "Objective", "...",
                                          new Vector2(0, 5), new Vector2(400, 32), 17);

            var window = BuildBodyText(panel.transform, "Window", "00:00 - 00:00",
                                       new Vector2(-90, -42), new Vector2(220, 28), 14, SciFiTheme.TextMuted);
            window.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

            var countdown = BuildBodyText(panel.transform, "Countdown", "",
                                          new Vector2(90, -42), new Vector2(220, 28), 16);
            countdown.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
            countdown.GetComponent<TextMeshProUGUI>().color = SciFiTheme.AccentCyan;
            countdown.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var ctrl = panel.AddComponent<UIQuestHUDController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "titleText", title.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "objectiveText", objective.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "windowText", window.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "countdownText", countdown.GetComponent<TextMeshProUGUI>());
        }

        static void BuildClockHUD(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "ClockHUD");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(320, 96);
            rect.anchoredPosition = new Vector2(0, -30);
            SciFiTheme.StyleHUDChip(panel);

            var day = BuildBodyText(panel.transform, "Day", "Ngay 1 (Mon)",
                                      new Vector2(0, 22), new Vector2(300, 30), 16, SciFiTheme.TextMuted);
            var time = BuildBodyText(panel.transform, "Time", "05:00",
                                       new Vector2(0, -20), new Vector2(300, 40), 26);
            time.GetComponent<TextMeshProUGUI>().color = SciFiTheme.AccentCyan;
            time.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

            var ctrl = panel.AddComponent<UIClockHUDController>();
            AssignSerialized(ctrl, "dayText", day.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "timeText", time.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "clock", locator != null ? locator.clock : null);
        }

        static void BuildScoreHUD(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "ScoreHUD");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(340, 110);
            rect.anchoredPosition = new Vector2(30, -30);
            SciFiTheme.StyleHUDChip(panel);

            var hocTap = BuildBodyText(panel.transform, "HocTap", "Hoc tap: 0/480",
                                         new Vector2(0, 28), new Vector2(320, 30), 18);
            var renLuyen = BuildBodyText(panel.transform, "RenLuyen", "Ren luyen: 100/100",
                                           new Vector2(0, -16), new Vector2(320, 30), 18,
                                           SciFiTheme.TextMuted);

            var ctrl = panel.AddComponent<UIScoreHUDController>();
            AssignSerialized(ctrl, "hocTapText", hocTap.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "renLuyenText", renLuyen.GetComponent<TextMeshProUGUI>());
            AssignSerialized(ctrl, "playerState", locator != null ? locator.playerState : null);
        }

        static void BuildInteractPrompt(Transform parent)
        {
            var panel = BuildPanel(parent, "InteractPrompt");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(420, 80);
            rect.anchoredPosition = new Vector2(0, 80);
            SciFiTheme.StyleHUDChip(panel);

            var prompt = BuildBodyText(panel.transform, "Prompt", "Bam E de tuong tac",
                                         new Vector2(0, 0), new Vector2(400, 60), 22);
            prompt.GetComponent<TextMeshProUGUI>().color = SciFiTheme.AccentCyan;

            var ctrl = panel.AddComponent<UIInteractPromptController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "promptText", prompt.GetComponent<TextMeshProUGUI>());
        }

        // Big touch-friendly Interact button on the right side, mirrors the E key.
        // UIInteractButton listens to InteractZone messages so it auto-hides when no
        // interactable is in range and re-broadcasts InteractPressedMsg on tap.
        static void BuildInteractButton(Transform parent)
        {
            var go = new GameObject("InteractButton");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(180, 180);
            rect.anchoredPosition = new Vector2(-60, 60);

            var canvasGroup = go.AddComponent<CanvasGroup>();

            // Backing image: chevron sci-fi primary button shape so it reads as "press me".
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(go.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var img = bgGo.AddComponent<Image>();
            img.sprite = SciFiTheme.Load("popup_btn_n");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;

            var btn = bgGo.AddComponent<Button>();
            SciFiTheme.StyleButton(btn, primary: true);

            // Big "E" label centred so desktop + touch users both see what binds to this.
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(bgGo.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero; labelRect.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "E";
            tmp.fontSize = 72;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = SciFiTheme.TextWhite;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 0.7f);

            var ctrl = go.AddComponent<UIInteractButton>();
            AssignSerialized(ctrl, "button", btn);
            AssignSerialized(ctrl, "canvasGroup", canvasGroup);
            AssignSerialized(ctrl, "label", tmp);
        }

        static void BuildMiniMap(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "MiniMap");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(260, 260);
            rect.anchoredPosition = new Vector2(-30, -160);

            // Map background (RawImage backed by minimap render-texture).
            var mapBg = new GameObject("MapImage");
            mapBg.transform.SetParent(panel.transform, false);
            var mapRect = mapBg.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0.04f, 0.06f);
            mapRect.anchorMax = new Vector2(0.96f, 0.94f);
            mapRect.offsetMin = Vector2.zero; mapRect.offsetMax = Vector2.zero;
            var raw = mapBg.AddComponent<RawImage>();
            raw.color = Color.white;
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
            if (rt == null) rt = EnsureMinimapRT();
            raw.texture = rt;

            // Sci-fi frame.
            var frame = new GameObject("Frame");
            frame.transform.SetParent(panel.transform, false);
            var frRect = frame.AddComponent<RectTransform>();
            frRect.anchorMin = Vector2.zero; frRect.anchorMax = Vector2.one;
            frRect.offsetMin = Vector2.zero; frRect.offsetMax = Vector2.zero;
            var frImg = frame.AddComponent<Image>();
            frImg.sprite = SciFiTheme.Load("item_frame_f");
            frImg.type = Image.Type.Sliced;
            frImg.color = SciFiTheme.AccentCyan;
            frImg.raycastTarget = false;

            // Centered player dot.
            var dotGo = new GameObject("PlayerDot");
            dotGo.transform.SetParent(panel.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(12, 12);
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = new Color(0.20f, 0.95f, 0.30f);

            BuildBodyText(panel.transform, "Label", "BAN DO",
                          new Vector2(0, -120), new Vector2(220, 22), 14, SciFiTheme.AccentCyan);

            var ctrl = panel.AddComponent<UIMiniMapController>();
            AssignSerialized(ctrl, "mapRect", rect);
            AssignSerialized(ctrl, "playerDot", dotRect);
            AssignSerialized(ctrl, "playerState", locator != null ? locator.playerState : null);
        }

        // Joystick: instantiate the Floating Joystick from Joystick Pack (proper touch
        // input from a polished prefab) rather than rolling our own. PlayerController
        // resolves it at Start() via FindFirstObjectByType<Joystick>().
        static void BuildJoystick(Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FloatingJoystickPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[SceneBuilder] Joystick prefab missing at {FloatingJoystickPath}, skipping.");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.name = "Joystick";
            // Reanchor to bottom-left quadrant of HUDCanvas (sized to a sensible thumb area).
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0.35f, 0.55f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ====================================================================
        // utility
        // ====================================================================
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

        static T LoadAsset<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path);

        static void AssignSerialized(Component comp, string fieldName, Object value)
        {
            if (comp == null) { Debug.LogWarning($"[AssignSerialized] comp null for field {fieldName}"); return; }
            if (value == null) Debug.LogWarning($"[AssignSerialized] {comp.GetType().Name}.{fieldName} value is null!");
            var fi = comp.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (fi != null) fi.SetValue(comp, value);
            else Debug.LogWarning($"[AssignSerialized] field '{fieldName}' not found on {comp.GetType().Name}");

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[AssignSerialized] SerializedProperty '{fieldName}' not found on {comp.GetType().Name}");
            }
            EditorUtility.SetDirty(comp);
        }

        static void AssignSerializedArray(Component comp, string fieldName, Object[] values)
        {
            if (comp == null) return;
            var fi = comp.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);
            if (fi != null && fi.FieldType.IsArray)
            {
                var elemType = fi.FieldType.GetElementType();
                var arr = System.Array.CreateInstance(elemType, values.Length);
                for (int i = 0; i < values.Length; i++)
                    arr.SetValue(values[i], i);
                fi.SetValue(comp, arr);
            }

            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(comp);
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

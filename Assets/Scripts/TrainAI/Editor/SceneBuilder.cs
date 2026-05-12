using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrainAI.Presentation;
using TrainAI.Services;
using TrainAI.SO.Base;
using TrainAI.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
            cfg.distance = 6f;
            cfg.height = 2.2f;
            cfg.yawSpeed = 140f;
            cfg.pitchMin = -10f;
            cfg.pitchMax = 60f;
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

        static void BuildPersistentUI(ServiceLocatorSO locator)
        {
            var canvas = BuildCanvas("UICanvas", sortOrder: 100);

            var router = canvas.AddComponent<UIRouterMono>();
            AssignSerialized(router, "services", locator);

            var confirm = BuildConfirmScreen(canvas.transform);
            var dialogue = BuildDialogueScreen(canvas.transform);
            var loading = BuildLoadingScreen(canvas.transform);
            var quiz = BuildQuizScreen(canvas.transform);
            var ending = BuildEndingScreen(canvas.transform);
            var expel = BuildExpelScreen(canvas.transform);
            BuildQuestHUD(canvas.transform);
            BuildClockHUD(canvas.transform, locator);
            BuildScoreHUD(canvas.transform, locator);
            BuildInteractPrompt(canvas.transform);
            BuildMiniMap(canvas.transform, locator);
            BuildJoystick(canvas.transform);

            AssignSerialized(router, "confirm", confirm);
            AssignSerialized(router, "dialogue", dialogue);
            AssignSerialized(router, "loading", loading);
            AssignSerialized(router, "quiz", quiz);
            AssignSerialized(router, "ending", ending);
            AssignSerialized(router, "expel", expel);
        }

        // ====================================================================
        // 01_MainMenu
        // ====================================================================
        static void BuildMainMenu(ServiceLocatorSO locator)
        {
            var canvas = BuildCanvas("MainMenuCanvas");
            var panel = BuildPanel(canvas.transform, "MainMenuPanel");
            BuildPanelBG(panel, new Color(0.05f, 0.10f, 0.18f, 1f));

            BuildTextChild(panel.transform, "Title", "TrainAI - Hoc ky quan su",
                           new Vector2(0, 200), new Vector2(600, 80), 32);
            var newBtn = BuildButtonChild(panel.transform, "NewGameButton", "New Game",
                                          new Vector2(0, 60), new Vector2(240, 60));
            var contBtn = BuildButtonChild(panel.transform, "ContinueButton", "Tiep tuc",
                                           new Vector2(0, -20), new Vector2(240, 60));
            var exitBtn = BuildButtonChild(panel.transform, "ExitButton", "Thoat",
                                           new Vector2(0, -100), new Vector2(240, 60));

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
            var canvas = BuildCanvas("CreateCharCanvas");
            var panel = BuildPanel(canvas.transform, "CreateCharPanel");
            BuildPanelBG(panel, new Color(0.05f, 0.10f, 0.18f, 1f));

            BuildTextChild(panel.transform, "Header", "Ban can dien ten truoc khi vao game",
                           new Vector2(0, 100), new Vector2(700, 80), 24);
            var input = BuildInputFieldChild(panel.transform, "NameInput",
                                              new Vector2(0, 0), new Vector2(400, 50));
            var confirmBtn = BuildButtonChild(panel.transform, "ConfirmButton", "Xac nhan",
                                              new Vector2(0, -80), new Vector2(200, 60));

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
            // Ground - bigger so the play area feels generous, colored, with a soft grid look.
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(12, 1, 12); // 120x120
            ApplyMaterial(ground, MaterialPalette.Ground(MaterialFolder));

            // Player
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/Player.prefab");
            GameObject player = null;
            if (playerPrefab != null)
            {
                player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                player.name = "Player";
                // Mesh feet sit at root Y=0 (see PrefabBuilder.BuildPlayer), spawn at ground level.
                player.transform.position = new Vector3(0, 0.05f, 0);
                if (locator != null)
                {
                    var pc = player.GetComponent<PlayerController>();
                    if (pc != null) AssignSerialized(pc, "playerState", locator.playerState);
                }
            }

            // Camera rig (third-person follow).
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

            // Spawn area interactable cubes with colored materials.
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

                    Material areaMat = IsDoor(a.id) ? MaterialPalette.Door(MaterialFolder)
                                     : IsFreeArea(a.id) ? MaterialPalette.FreeArea(MaterialFolder)
                                     : MaterialPalette.Area(MaterialFolder);
                    ApplyMaterial(cube, areaMat);

                    // Sign label so the player can read what each cube is even without the prompt.
                    BuildSignLabel(cube.transform, a.id, a.size.y);
                }
            }

            // NPCs (mesh feet at root Y=0 in new prefab, so spawnPos.y=0 sits properly).
            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/NPC.prefab");
            if (locator != null && locator.npcDB != null && npcPrefab != null)
            {
                foreach (var npcSo in locator.npcDB.all)
                {
                    if (npcSo == null) continue;
                    var npcGo = (GameObject)PrefabUtility.InstantiatePrefab(npcPrefab);
                    npcGo.name = $"NPC_{npcSo.id}";
                    var sp = npcSo.spawnPos;
                    if (sp.y < 0.01f) sp.y = 0.05f; // lift slightly so capsule stands on ground.
                    npcGo.transform.position = sp;
                    var view = npcGo.GetComponent<NpcView>();
                    if (view != null)
                    {
                        AssignSerialized(view, "npcDef", npcSo);
                        AssignSerialized(view, "services", locator);
                    }
                    BuildSignLabel(npcGo.transform, npcSo.id, 2.0f);
                }
            }

            // Interaction router bridge.
            var bridgeGo = new GameObject("InteractionBridge");
            var bridge = bridgeGo.AddComponent<InteractionRouterBridge>();
            AssignSerialized(bridge, "services", locator);

            // Quest arrow above player's head.
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/QuestArrow.prefab");
            if (arrowPrefab != null && player != null)
            {
                var arrow = (GameObject)PrefabUtility.InstantiatePrefab(arrowPrefab);
                arrow.transform.SetParent(player.transform, false);
                arrow.transform.localPosition = Vector3.zero;
                arrow.transform.localRotation = Quaternion.identity;
            }

            // Minimap top-down camera rendering into the shared RT.
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

        static void BuildSignLabel(Transform parent, string text, float topY)
        {
            var labelGo = new GameObject("Sign");
            labelGo.transform.SetParent(parent, false);
            labelGo.transform.localPosition = new Vector3(0, topY + 0.6f, 0);
            labelGo.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

            var canvas = labelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            labelGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            labelGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var rect = labelGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(4f, 1f);
            rect.localScale = Vector3.one * 0.35f;

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(labelGo.transform, false);
            var tRect = tGo.AddComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(4f, 1f);
            tRect.anchoredPosition = Vector2.zero;
            var tmp = tGo.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = HumanizeId(text);
            tmp.fontSize = 1.2f;
            tmp.color = Color.white;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.fontStyle = TMPro.FontStyles.Bold;
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

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Interactable_Center";
            cube.transform.position = new Vector3(0, 0.5f, 2);
            cube.transform.localScale = new Vector3(2, 1, 2);
            var box = cube.GetComponent<BoxCollider>();
            box.isTrigger = true;
            cube.AddComponent<InteractableMarker>();
            ApplyMaterial(cube, MaterialPalette.Area(MaterialFolder));
        }

        // ====================================================================
        // 99_Ending
        // ====================================================================
        static void BuildEnding()
        {
            var canvas = BuildCanvas("EndingCanvas");
            var panel = BuildPanel(canvas.transform, "EndingPanel");
            BuildPanelBG(panel, new Color(0.05f, 0.07f, 0.10f, 1f));
            BuildTextChild(panel.transform, "Title", "Hoan thanh khoa hoc quan su",
                           new Vector2(0, 100), new Vector2(800, 80), 36);
        }

        // ====================================================================
        // UI helpers
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

        static GameObject BuildTextChild(Transform parent, string name, string text,
                                          Vector2 anchoredPos, Vector2 size, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go;
        }

        static Button BuildButtonChild(Transform parent, string name, string label,
                                        Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.3f, 0.5f, 1f);
            var btn = go.AddComponent<Button>();
            BuildTextChild(go.transform, "Label", label, Vector2.zero, size, 20);
            return btn;
        }

        static TMPro.TMP_InputField BuildInputFieldChild(Transform parent, string name,
                                                          Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 1f);
            var input = go.AddComponent<TMPro.TMP_InputField>();
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(go.transform, false);
            var taRect = textArea.AddComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.offsetMin = new Vector2(10, 5);
            taRect.offsetMax = new Vector2(-10, -5);
            var textGo = BuildTextChild(textArea.transform, "Text", "", Vector2.zero, size - new Vector2(20, 10), 18);
            input.textComponent = textGo.GetComponent<TMPro.TextMeshProUGUI>();
            return input;
        }

        // ====================================================================
        // UI screen panels
        // ====================================================================
        static UIConfirmController BuildConfirmScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UIConfirm");
            BuildPanelBG(panel);
            var body = BuildTextChild(panel.transform, "Body", "...", new Vector2(0, 50), new Vector2(700, 100), 24);
            var ok = BuildButtonChild(panel.transform, "OK", "OK", new Vector2(-120, -80), new Vector2(180, 60));
            var cancel = BuildButtonChild(panel.transform, "Cancel", "Huy", new Vector2(120, -80), new Vector2(180, 60));
            var ctrl = panel.AddComponent<UIConfirmController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "okButton", ok);
            AssignSerialized(ctrl, "cancelButton", cancel);
            return ctrl;
        }

        static UIDialogueController BuildDialogueScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UIDialogue");
            BuildPanelBG(panel);
            var name = BuildTextChild(panel.transform, "NpcName", "NPC", new Vector2(0, 200), new Vector2(600, 50), 26);
            var history = BuildTextChild(panel.transform, "History", "", new Vector2(0, 50), new Vector2(800, 250), 18);
            history.GetComponent<TMPro.TextMeshProUGUI>().alignment = TMPro.TextAlignmentOptions.TopLeft;
            var input = BuildInputFieldChild(panel.transform, "Input", new Vector2(-100, -150), new Vector2(500, 50));
            var send = BuildButtonChild(panel.transform, "Send", "Gui", new Vector2(220, -150), new Vector2(120, 50));
            var close = BuildButtonChild(panel.transform, "Close", "X", new Vector2(380, 220), new Vector2(60, 50));
            var ctrl = panel.AddComponent<UIDialogueController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "npcName", name.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "history", history.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "input", input);
            AssignSerialized(ctrl, "sendButton", send);
            AssignSerialized(ctrl, "closeButton", close);
            return ctrl;
        }

        static UILoadingController BuildLoadingScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UILoading");
            BuildPanelBG(panel, new Color(0, 0, 0, 0.85f));
            var label = BuildTextChild(panel.transform, "Label", "Dang tai...",
                                        new Vector2(0, 0), new Vector2(700, 100), 28);
            var ctrl = panel.AddComponent<UILoadingController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "label", label.GetComponent<TMPro.TextMeshProUGUI>());
            return ctrl;
        }

        static UIQuizController BuildQuizScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UIQuiz");
            BuildPanelBG(panel);
            var question = BuildTextChild(panel.transform, "Question", "Cau hoi?",
                                           new Vector2(0, 200), new Vector2(900, 120), 26);
            var counter = BuildTextChild(panel.transform, "Counter", "1/10",
                                          new Vector2(-400, 280), new Vector2(120, 40), 20);
            var timer = BuildTextChild(panel.transform, "Timer", "15",
                                        new Vector2(400, 280), new Vector2(120, 40), 24);

            var ctrl = panel.AddComponent<UIQuizController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "questionText", question.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "counterText", counter.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "timerText", timer.GetComponent<TMPro.TextMeshProUGUI>());

            var buttons = new Button[4];
            var labels = new TMPro.TextMeshProUGUI[4];
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -200 : 200);
                float y = (i < 2 ? 50 : -50);
                var b = BuildButtonChild(panel.transform, $"Ans{i}", $"Dap an {(char)('A' + i)}",
                                          new Vector2(x, y), new Vector2(360, 70));
                buttons[i] = b;
                var lbl = b.transform.Find("Label");
                if (lbl != null) labels[i] = lbl.GetComponent<TMPro.TextMeshProUGUI>();
            }
            AssignSerializedArray(ctrl, "answerButtons", buttons);
            AssignSerializedArray(ctrl, "answerLabels", labels);

            var cont = BuildButtonChild(panel.transform, "Continue", "Tiep theo",
                                         new Vector2(0, -180), new Vector2(200, 60));
            AssignSerialized(ctrl, "continueButton", cont);
            return ctrl;
        }

        static UIEndingController BuildEndingScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UIEnding");
            BuildPanelBG(panel, new Color(0, 0, 0, 0.9f));
            var head = BuildTextChild(panel.transform, "Headline", "TOT NGHIEP",
                                       new Vector2(0, 120), new Vector2(800, 100), 48);
            var body = BuildTextChild(panel.transform, "Body", "...",
                                       new Vector2(0, -20), new Vector2(800, 200), 22);
            var ctrl = panel.AddComponent<UIEndingController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "headlineText", head.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TMPro.TextMeshProUGUI>());
            return ctrl;
        }

        static UIExpelController BuildExpelScreen(Transform parent)
        {
            var panel = BuildPanel(parent, "UIExpel");
            BuildPanelBG(panel, new Color(0.5f, 0, 0, 0.9f));
            var body = BuildTextChild(panel.transform, "Body", "...",
                                       new Vector2(0, 0), new Vector2(800, 150), 26);
            var ctrl = panel.AddComponent<UIExpelController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "bodyText", body.GetComponent<TMPro.TextMeshProUGUI>());
            return ctrl;
        }

        static void BuildQuestHUD(Transform parent)
        {
            var panel = BuildPanel(parent, "QuestHUD");
            panel.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            panel.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
            panel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, -240);

            var bg = new GameObject("BG");
            bg.transform.SetParent(panel.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);

            var title = BuildTextChild(panel.transform, "Title", "...",
                                        new Vector2(0, 20), new Vector2(380, 40), 22);
            var window = BuildTextChild(panel.transform, "Window", "",
                                         new Vector2(0, -20), new Vector2(380, 30), 18);
            var ctrl = panel.AddComponent<UIQuestHUDController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "titleText", title.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "windowText", window.GetComponent<TMPro.TextMeshProUGUI>());
        }

        static void BuildClockHUD(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "ClockHUD");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(300, 80);
            rect.anchoredPosition = new Vector2(0, -50);

            var bg = new GameObject("BG");
            bg.transform.SetParent(panel.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);

            var day = BuildTextChild(panel.transform, "Day", "Ngay 1 (Mon)",
                                      new Vector2(0, 20), new Vector2(280, 30), 18);
            var time = BuildTextChild(panel.transform, "Time", "05:00",
                                       new Vector2(0, -15), new Vector2(280, 30), 22);
            var ctrl = panel.AddComponent<UIClockHUDController>();
            AssignSerialized(ctrl, "dayText", day.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "timeText", time.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "clock", locator != null ? locator.clock : null);
        }

        static void BuildScoreHUD(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "ScoreHUD");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.sizeDelta = new Vector2(300, 100);
            rect.anchoredPosition = new Vector2(170, -60);

            var bg = new GameObject("BG");
            bg.transform.SetParent(panel.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);

            var hocTap = BuildTextChild(panel.transform, "HocTap", "Hoc tap: 0/480",
                                         new Vector2(0, 25), new Vector2(280, 30), 18);
            var renLuyen = BuildTextChild(panel.transform, "RenLuyen", "Ren luyen: 100/100",
                                           new Vector2(0, -15), new Vector2(280, 30), 18);
            var ctrl = panel.AddComponent<UIScoreHUDController>();
            AssignSerialized(ctrl, "hocTapText", hocTap.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "renLuyenText", renLuyen.GetComponent<TMPro.TextMeshProUGUI>());
            AssignSerialized(ctrl, "playerState", locator != null ? locator.playerState : null);
        }

        static void BuildInteractPrompt(Transform parent)
        {
            var panel = BuildPanel(parent, "InteractPrompt");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(300, 80);
            rect.anchoredPosition = new Vector2(-170, 100);

            var bg = new GameObject("BG");
            bg.transform.SetParent(panel.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.65f);

            var prompt = BuildTextChild(panel.transform, "Prompt", "Bam E de tuong tac",
                                         new Vector2(0, 0), new Vector2(280, 70), 18);
            var ctrl = panel.AddComponent<UIInteractPromptController>();
            AssignSerialized(ctrl, "canvasGroup", panel.GetComponent<CanvasGroup>());
            AssignSerialized(ctrl, "promptText", prompt.GetComponent<TMPro.TextMeshProUGUI>());
        }

        static void BuildMiniMap(Transform parent, ServiceLocatorSO locator)
        {
            var panel = BuildPanel(parent, "MiniMap");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.sizeDelta = new Vector2(240, 240);
            rect.anchoredPosition = new Vector2(-140, -150);

            // Map background (RawImage backed by minimap render-texture).
            var mapBg = new GameObject("MapImage");
            mapBg.transform.SetParent(panel.transform, false);
            var mapRect = mapBg.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero; mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = Vector2.zero; mapRect.offsetMax = Vector2.zero;
            var raw = mapBg.AddComponent<RawImage>();
            raw.color = Color.white;
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(RTPath);
            if (rt == null) rt = EnsureMinimapRT();
            raw.texture = rt;

            // Frame border on top of map.
            var frame = new GameObject("Frame");
            frame.transform.SetParent(panel.transform, false);
            var frRect = frame.AddComponent<RectTransform>();
            frRect.anchorMin = Vector2.zero; frRect.anchorMax = Vector2.one;
            frRect.offsetMin = Vector2.zero; frRect.offsetMax = Vector2.zero;
            var frImg = frame.AddComponent<Image>();
            frImg.color = new Color(1f, 1f, 1f, 0.18f);
            frImg.raycastTarget = false;

            // Centered player dot (since minimap camera is parented to player).
            var dotGo = new GameObject("PlayerDot");
            dotGo.transform.SetParent(panel.transform, false);
            var dotRect = dotGo.AddComponent<RectTransform>();
            dotRect.sizeDelta = new Vector2(12, 12);
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color = new Color(0.20f, 0.95f, 0.30f);

            // Label so the player knows what they're looking at.
            BuildTextChild(panel.transform, "Label", "Ban do",
                           new Vector2(0, -110), new Vector2(200, 22), 14);

            var ctrl = panel.AddComponent<UIMiniMapController>();
            AssignSerialized(ctrl, "mapRect", rect);
            AssignSerialized(ctrl, "playerDot", dotRect);
            AssignSerialized(ctrl, "playerState", locator != null ? locator.playerState : null);
        }

        static void BuildJoystick(Transform parent)
        {
            var panel = BuildPanel(parent, "Joystick");
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.sizeDelta = new Vector2(200, 200);
            rect.anchoredPosition = new Vector2(150, 150);

            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(panel.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(160, 160);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.3f, 0.3f, 0.3f, 0.4f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(bgGo.transform, false);
            var handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(60, 60);
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);

            var ctrl = panel.AddComponent<UIJoystickController>();
            AssignSerialized(ctrl, "background", bgRect);
            AssignSerialized(ctrl, "handle", handleRect);
        }

        static void BuildPanelBG(GameObject panel, Color? color = null)
        {
            var bgGo = new GameObject("BG");
            bgGo.transform.SetParent(panel.transform, false);
            var rect = bgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = bgGo.AddComponent<Image>();
            img.color = color ?? new Color(0, 0, 0, 0.7f);
            bgGo.transform.SetAsFirstSibling();
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

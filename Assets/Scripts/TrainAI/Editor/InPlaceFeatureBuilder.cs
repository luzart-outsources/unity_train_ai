using TMPro;
using TrainAI.Services;
using TrainAI.SO.Base;
using TrainAI.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.Editor
{
    // In-place feature additions that DON'T regenerate scenes. Each menu
    // item finds the existing UI parent (HUDCanvas / ModalCanvas / ScoreHUD
    // / UIQuiz) by name, adds new children under it, attaches components,
    // wires references, and saves the scene. Re-runnable: every Add* method
    // checks for an existing child and bails if found (no duplicates).
    //
    // Why a separate file from SceneBuilder:
    //   SceneBuilder uses NewScene(EmptyScene)+Single mode → destructive.
    //   This builder only mutates already-loaded scenes via Object.Instantiate-
    //   style additions; designer-side polish (transforms, theme tweaks,
    //   custom children) is preserved.
    public static class InPlaceFeatureBuilder
    {
        const string BootstrapScenePath = "Assets/Scenes/TrainAI/00_Bootstrap.unity";
        const string LocatorPath        = "Assets/_Data/Config/ServiceLocator.asset";
        const string MainMenuSceneRef   = "Assets/_Data/Scenes/SceneRef_01_MainMenu.asset";
        const string NpcAnimController  = "Assets/Animation/Controllers/NPC.controller";

        // ====================================================================
        // F4 + F7 + F5 — UI additions on the persistent UICanvas (Bootstrap)
        // ====================================================================
        [MenuItem("Tools/Build Game/In-Place/F4 + F5 + F7: Add UI Features to Bootstrap", false, 300)]
        public static void AddAllBootstrapUI()
        {
            var scene = EnsureBootstrapLoaded();
            var locator = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);

            AddPauseUI(locator);          // F4
            AddScoreHUDAvatarName();      // F7
            AddQuizFeedbackOverlay();     // F5
            AddMiniMapHolaWire();         // F1 (just wires the field if HolaMap.png exists)

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[InPlace] Bootstrap UI features applied (F4+F5+F7+F1-wire).");
        }

        // ====================================================================
        // F4 — Pause button (HUD) + Pause modal (ModalCanvas)
        // ====================================================================
        public static void AddPauseUI(ServiceLocatorSO locator)
        {
            var hud = GameObject.Find("UICanvas/HUDCanvas");
            var modal = GameObject.Find("UICanvas/ModalCanvas");
            if (hud == null || modal == null) { Debug.LogError("[InPlace.F4] HUDCanvas or ModalCanvas missing"); return; }

            // 1. Pause modal (skip if exists)
            UIPauseController ctrl = null;
            var existingPause = modal.transform.Find("UIPause");
            if (existingPause == null)
            {
                var pauseGo = new GameObject("UIPause");
                pauseGo.transform.SetParent(modal.transform, false);
                var pauseRect = pauseGo.AddComponent<RectTransform>();
                pauseRect.anchorMin = Vector2.zero; pauseRect.anchorMax = Vector2.one;
                pauseRect.offsetMin = Vector2.zero; pauseRect.offsetMax = Vector2.zero;
                var pauseCG = pauseGo.AddComponent<CanvasGroup>();

                // Full-screen dim
                var dim = new GameObject("Dim");
                dim.transform.SetParent(pauseGo.transform, false);
                var dimRect = dim.AddComponent<RectTransform>();
                dimRect.anchorMin = Vector2.zero; dimRect.anchorMax = Vector2.one;
                dimRect.offsetMin = Vector2.zero; dimRect.offsetMax = Vector2.zero;
                var dimImg = dim.AddComponent<Image>();
                dimImg.color = new Color(0, 0, 0, 0.78f);
                dimImg.raycastTarget = true;

                // Card
                var card = new GameObject("UIPauseCard");
                card.transform.SetParent(pauseGo.transform, false);
                var cardRect = card.AddComponent<RectTransform>();
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.sizeDelta = new Vector2(640, 520);
                cardRect.anchoredPosition = Vector2.zero;
                var cardImg = card.AddComponent<Image>();
                cardImg.sprite = SciFiTheme.Load("popup_bg_02");
                cardImg.type = Image.Type.Sliced;
                cardImg.color = Color.white;

                var title    = AddText(card.transform, "Title", "TAM DUNG", new Vector2(0, 180), new Vector2(560, 70), 36, SciFiTheme.TextWhite, FontStyles.Bold, TextAlignmentOptions.Center);
                var hint     = AddText(card.transform, "Hint", "Game da dung. Chon mot tuy chon.", new Vector2(0, 110), new Vector2(560, 40), 18, SciFiTheme.TextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
                var resume   = AddButton(card.transform, "Resume", "Tiep tuc choi", new Vector2(0, 30), new Vector2(380, 76), primary: true);
                var saveQuit = AddButton(card.transform, "SaveQuit", "Luu va ve menu", new Vector2(0, -60), new Vector2(380, 76), primary: true);
                var quitNo   = AddButton(card.transform, "QuitNoSave", "Thoat khong luu", new Vector2(0, -150), new Vector2(380, 70), primary: false);

                ctrl = pauseGo.AddComponent<UIPauseController>();
                AssignField(ctrl, "canvasGroup", pauseCG);
                AssignField(ctrl, "resumeButton", resume);
                AssignField(ctrl, "saveQuitButton", saveQuit);
                AssignField(ctrl, "quitNoSaveButtonOrFallback", quitNo); // tolerated below — actual field name resolved
                AssignField(ctrl, "quitNoSaveButton", quitNo);
                AssignField(ctrl, "services", locator);
                AssignField(ctrl, "mainMenuScene", AssetDatabase.LoadAssetAtPath<SceneRefSO>(MainMenuSceneRef));

                // Start hidden (alpha 0, gameObject stays active so ESC keeps working)
                pauseCG.alpha = 0f; pauseCG.blocksRaycasts = false; pauseCG.interactable = false;
                Debug.Log("[InPlace.F4] Created UIPause modal");
            }
            else
            {
                ctrl = existingPause.GetComponent<UIPauseController>();
                Debug.Log("[InPlace.F4] UIPause already exists, kept");
            }

            // 2. PauseButton on HUD (skip if exists)
            if (hud.transform.Find("PauseButton") == null && ctrl != null)
            {
                var pbGo = new GameObject("PauseButton");
                pbGo.transform.SetParent(hud.transform, false);
                var rt = pbGo.AddComponent<RectTransform>();
                // Top-LEFT, below ScoreHUD to avoid QuestHUD overlap on the right.
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.sizeDelta = new Vector2(72, 72);
                rt.anchoredPosition = new Vector2(30, -220);
                var img = pbGo.AddComponent<Image>();
                img.sprite = SciFiTheme.Load("popup_btn_n");
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                var btn = pbGo.AddComponent<Button>();

                var label = AddText(pbGo.transform, "Label", "II", new Vector2(0, 0), Vector2.zero, 38, SciFiTheme.TextWhite, FontStyles.Bold, TextAlignmentOptions.Center, stretch: true);

                // onClick → ctrl.Open
                UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, ctrl.Open);
                Debug.Log("[InPlace.F4] Created PauseButton wired to UIPause.Open");
            }
        }

        // ====================================================================
        // F7 — Avatar circle + name TMP on existing ScoreHUD
        // ====================================================================
        public static void AddScoreHUDAvatarName()
        {
            var score = GameObject.Find("UICanvas/HUDCanvas/ScoreHUD");
            if (score == null) { Debug.LogError("[InPlace.F7] ScoreHUD missing"); return; }

            TMP_Text letterTmp = null, nameTmp = null;

            // Avatar
            if (score.transform.Find("Avatar") == null)
            {
                var avatarGo = new GameObject("Avatar");
                avatarGo.transform.SetParent(score.transform, false);
                var ar = avatarGo.AddComponent<RectTransform>();
                ar.anchorMin = new Vector2(0, 1); ar.anchorMax = new Vector2(0, 1);
                ar.pivot = new Vector2(0, 1);
                ar.sizeDelta = new Vector2(56, 56);
                ar.anchoredPosition = new Vector2(8, -8);
                var avImg = avatarGo.AddComponent<Image>();
                avImg.sprite = SciFiTheme.Load("item_frame_n");
                avImg.type = Image.Type.Sliced;
                avImg.color = new Color(0.10f, 0.30f, 0.55f, 1f);

                letterTmp = AddText(avatarGo.transform, "Letter", "H", new Vector2(0, 0), Vector2.zero, 32, SciFiTheme.AccentCyan, FontStyles.Bold, TextAlignmentOptions.Center, stretch: true);
                Debug.Log("[InPlace.F7] Avatar added to ScoreHUD");
            }
            else
            {
                letterTmp = score.transform.Find("Avatar/Letter")?.GetComponent<TMP_Text>();
            }

            // Name
            if (score.transform.Find("PlayerName") == null)
            {
                var nameGo = new GameObject("PlayerName");
                nameGo.transform.SetParent(score.transform, false);
                var nr = nameGo.AddComponent<RectTransform>();
                nr.anchorMin = new Vector2(0, 1); nr.anchorMax = new Vector2(0, 1);
                nr.pivot = new Vector2(0, 1);
                nr.sizeDelta = new Vector2(220, 28);
                nr.anchoredPosition = new Vector2(72, -18);
                nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
                nameTmp.text = "Hoc vien";
                nameTmp.fontSize = 18;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.alignment = TextAlignmentOptions.MidlineLeft;
                nameTmp.color = SciFiTheme.TextWhite;
                nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
                nameTmp.overflowMode = TextOverflowModes.Ellipsis;
                nameTmp.raycastTarget = false;
                Debug.Log("[InPlace.F7] PlayerName added to ScoreHUD");
            }
            else
            {
                nameTmp = score.transform.Find("PlayerName").GetComponent<TMP_Text>();
            }

            // Wire UIScoreHUDController fields (component already exists, my edited script has new fields)
            var ctrl = score.GetComponent<UIScoreHUDController>();
            if (ctrl != null)
            {
                if (letterTmp != null) AssignField(ctrl, "avatarLetterText", letterTmp);
                if (nameTmp != null)   AssignField(ctrl, "playerNameText", nameTmp);
                Debug.Log("[InPlace.F7] UIScoreHUDController fields wired");
            }
        }

        // ====================================================================
        // F5 — Quiz feedback overlay (icon + label) inside UIQuiz
        // ====================================================================
        public static void AddQuizFeedbackOverlay()
        {
            var quiz = GameObject.Find("UICanvas/ModalCanvas/UIQuiz");
            if (quiz == null) { Debug.LogError("[InPlace.F5] UIQuiz missing"); return; }

            // Prefer placing inside the quiz card so it floats over the answer grid.
            // Fall back to the panel root if there is no obvious card child.
            Transform host = quiz.transform.Find("UIQuizCard");
            if (host == null) host = quiz.transform;

            TMP_Text iconTmp = null, resTmp = null;

            if (host.Find("FeedbackOverlay") == null)
            {
                var overlay = new GameObject("FeedbackOverlay");
                overlay.transform.SetParent(host, false);
                var orect = overlay.AddComponent<RectTransform>();
                orect.anchorMin = new Vector2(0.5f, 0.5f); orect.anchorMax = new Vector2(0.5f, 0.5f);
                orect.pivot = new Vector2(0.5f, 0.5f);
                orect.sizeDelta = new Vector2(420, 220);
                orect.anchoredPosition = new Vector2(0, -10);

                // ASCII-only "V" baked into scene — avoids TMP runtime atlas
                // extension that races D3D12 command buffer (crash class
                // 'm_CmdState == kActive'). ShowOverlay rewrites this text live.
                iconTmp = AddText(overlay.transform, "Icon", "V", new Vector2(0, 36), new Vector2(260, 160), 140, new Color(0.16f, 0.78f, 0.20f), FontStyles.Bold, TextAlignmentOptions.Center);
                iconTmp.outlineWidth = 0.25f;
                iconTmp.outlineColor = new Color(0f, 0f, 0f, 0.85f);

                resTmp = AddText(overlay.transform, "Result", "Dung!", new Vector2(0, -80), new Vector2(360, 60), 48, new Color(0.16f, 0.78f, 0.20f), FontStyles.Bold, TextAlignmentOptions.Center);
                resTmp.outlineWidth = 0.2f;
                resTmp.outlineColor = new Color(0f, 0f, 0f, 0.7f);

                overlay.SetActive(false);
                Debug.Log("[InPlace.F5] FeedbackOverlay added inside UIQuiz");
            }
            else
            {
                var overlayT = host.Find("FeedbackOverlay");
                iconTmp = overlayT.Find("Icon")?.GetComponent<TMP_Text>();
                resTmp  = overlayT.Find("Result")?.GetComponent<TMP_Text>();
            }

            // Wire UIQuizController fields
            var ctrl = quiz.GetComponent<UIQuizController>();
            if (ctrl != null)
            {
                var overlayGo = host.Find("FeedbackOverlay")?.gameObject;
                if (overlayGo != null) AssignField(ctrl, "feedbackOverlay", overlayGo);
                if (iconTmp != null) AssignField(ctrl, "feedbackIconText", iconTmp);
                if (resTmp != null)  AssignField(ctrl, "feedbackResultText", resTmp);
                Debug.Log("[InPlace.F5] UIQuizController feedback refs wired");
            }
        }

        // ====================================================================
        // F1 — wire holaMapTexture if Assets/_Assets/HolaMap.png exists
        // ====================================================================
        public static void AddMiniMapHolaWire()
        {
            var minimap = GameObject.Find("UICanvas/HUDCanvas/MiniMap");
            if (minimap == null) { Debug.LogWarning("[InPlace.F1] MiniMap missing"); return; }

            var ctrl = minimap.GetComponent<UIMiniMapController>();
            if (ctrl == null) { Debug.LogWarning("[InPlace.F1] UIMiniMapController missing on MiniMap"); return; }

            Texture hola = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Assets/HolaMap.png");
            if (hola == null) hola = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Assets/HolaMap.jpg");
            if (hola == null) { Debug.Log("[InPlace.F1] Assets/_Assets/HolaMap.png|.jpg not present — keeping RT fallback. Paste image and rerun to swap."); return; }

            // Find RawImage anywhere under MiniMap (designer might have renamed)
            var raw = minimap.GetComponentInChildren<RawImage>(true);
            if (raw != null) AssignField(ctrl, "mapImage", raw);
            AssignField(ctrl, "holaMapTexture", hola);
            Debug.Log($"[InPlace.F1] Wired HolaMap texture ({hola.name})");
        }

        // ====================================================================
        // F6 — decorative NPCs in 3 sub-scenes (open each, add, save)
        // ====================================================================
        [MenuItem("Tools/Build Game/In-Place/F6: Add Decorative NPCs to Sub-Scenes", false, 301)]
        public static void AddSubSceneNPCs()
        {
            AddNPCsInScene("Assets/Scenes/TrainAI/11_LopHoc.unity", new[]{
                ("Student_A", new Vector3(-3, 0, 0),  new Color(0.30f, 0.55f, 0.85f), Pose.Seated),
                ("Student_B", new Vector3( 0, 0, -2), new Color(0.60f, 0.30f, 0.70f), Pose.Seated),
                ("Student_C", new Vector3( 3, 0, 0),  new Color(0.85f, 0.65f, 0.20f), Pose.Seated),
                ("Teacher",   new Vector3( 0, 0, 7.5f), new Color(0.20f, 0.55f, 0.30f), Pose.Standing),
            });
            AddNPCsInScene("Assets/Scenes/TrainAI/12_NhaAn.unity", new[]{
                ("Diner_A", new Vector3(-3, 0, 1),  new Color(0.85f, 0.30f, 0.30f), Pose.Seated),
                ("Diner_B", new Vector3( 3, 0, 1),  new Color(0.35f, 0.65f, 0.85f), Pose.Seated),
                ("Diner_C", new Vector3(-2, 0, -3), new Color(0.65f, 0.85f, 0.30f), Pose.Seated),
                ("Diner_D", new Vector3( 2, 0, -3), new Color(0.85f, 0.55f, 0.20f), Pose.Seated),
                ("Queue_E", new Vector3( 4, 0, 5.5f), new Color(0.55f, 0.55f, 0.85f), Pose.Standing),
            });
            AddNPCsInScene("Assets/Scenes/TrainAI/13_KyTucXa.unity", new[]{
                ("Roommate_A", new Vector3(-7, 1.05f, 6), new Color(0.40f, 0.60f, 0.85f), Pose.Lying),
                ("Roommate_B", new Vector3(-4, 0, 8),     new Color(0.70f, 0.45f, 0.30f), Pose.Standing),
                ("Roommate_C", new Vector3( 4, 0, 8),     new Color(0.30f, 0.75f, 0.55f), Pose.Standing),
            });
            Debug.Log("[InPlace.F6] Done.");
        }

        enum Pose { Standing, Seated, Lying }

        static void AddNPCsInScene(string scenePath, (string label, Vector3 pos, Color tint, Pose pose)[] entries)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int added = 0, skipped = 0;
            foreach (var (label, pos, tint, pose) in entries)
            {
                string name = "NPC_" + label;
                bool exists = false;
                foreach (var r in scene.GetRootGameObjects()) if (r.name == name) { exists = true; break; }
                if (exists) { skipped++; continue; }
                BuildDecorativeNpc(name, pos, tint, pose);
                added++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[InPlace.F6] {System.IO.Path.GetFileName(scenePath)}: added={added} skipped={skipped}");
        }

        static GameObject BuildDecorativeNpc(string name, Vector3 basePos, Color tint, Pose pose)
        {
            var root = new GameObject(name);
            root.transform.position = basePos;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var bodyMat = new Material(shader) { color = tint, name = $"NPC_body_{tint.r:F2}_{tint.g:F2}_{tint.b:F2}" };
            var headMat = new Material(shader) { color = tint * 1.25f, name = bodyMat.name + "_head" };

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            switch (pose)
            {
                case Pose.Lying:
                    body.transform.localPosition = new Vector3(0, 0, 0);
                    body.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    body.transform.localScale = new Vector3(0.55f, 0.85f, 0.45f);
                    break;
                case Pose.Seated:
                    body.transform.localPosition = new Vector3(0, 0.55f, 0);
                    body.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                    break;
                default:
                    body.transform.localPosition = new Vector3(0, 0.9f, 0);
                    body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
                    break;
            }
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            body.GetComponent<Renderer>().sharedMaterial = bodyMat;

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = pose == Pose.Lying ? new Vector3(0, 0.05f, 0.65f)
                                          : pose == Pose.Seated ? new Vector3(0, 1.30f, 0)
                                          : new Vector3(0, 1.85f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var headCol = head.GetComponent<Collider>();
            if (headCol != null) Object.DestroyImmediate(headCol);
            head.GetComponent<Renderer>().sharedMaterial = headMat;

            return root;
        }

        // ====================================================================
        // F2 + F3 — Commander reposition + animator controller assignment
        // ====================================================================
        [MenuItem("Tools/Build Game/In-Place/F2 + F3: Commander to CongChinh + Animator Wire", false, 302)]
        public static void RepositionCommanderAndWireAnimator()
        {
            var worldScene = EditorSceneManager.OpenScene("Assets/Scenes/TrainAI/10_World.unity", OpenSceneMode.Single);

            // 1. Patch the _NPCSpawner.entries array — commander's spawn position
            //    is baked into the scene there. Editing the runtime GameObject
            //    doesn't persist, since NpcStagedSpawner re-instantiates at the
            //    serialized entries[i].position each Play.
            var spawnerGo = GameObject.Find("_NPCSpawner");
            int patched = 0;
            if (spawnerGo != null)
            {
                var spawner = spawnerGo.GetComponent("NpcStagedSpawner") as Component;
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    var entries = so.FindProperty("entries");
                    if (entries != null && entries.isArray)
                    {
                        for (int i = 0; i < entries.arraySize; i++)
                        {
                            var elem = entries.GetArrayElementAtIndex(i);
                            var label = elem.FindPropertyRelative("label");
                            var npcDef = elem.FindPropertyRelative("npcDef");
                            bool isCommander = false;
                            if (label != null && label.stringValue != null && label.stringValue.Contains("DaiDoi")) isCommander = true;
                            if (!isCommander && npcDef != null && npcDef.objectReferenceValue != null && npcDef.objectReferenceValue.name.Contains("DaiDoi")) isCommander = true;
                            if (isCommander)
                            {
                                var posProp = elem.FindPropertyRelative("position");
                                var eulerProp = elem.FindPropertyRelative("eulerAngles");
                                if (posProp != null) posProp.vector3Value = new Vector3(78, 0, -22);
                                if (eulerProp != null) eulerProp.vector3Value = new Vector3(0, 180, 0);
                                patched++;
                            }
                        }
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(spawner);
                        Debug.Log($"[InPlace.F2] Patched {patched} commander entries in _NPCSpawner to (78,0,-22)");
                    }
                    else Debug.LogWarning("[InPlace.F2] _NPCSpawner has no 'entries' SerializedProperty");
                }
                else Debug.LogWarning("[InPlace.F2] _NPCSpawner missing NpcStagedSpawner component");
            }
            else Debug.LogWarning("[InPlace.F2] _NPCSpawner not in 10_World — run NPC spawn menu first if needed");

            // 2. Also reposition any scene-resident NPC_DaiDoiTruong GameObjects
            //    (legacy SceneBuilder auto-spawn path may have placed one).
            foreach (var r in worldScene.GetRootGameObjects())
            {
                if (r.name == "NPC_DaiDoiTruong")
                {
                    r.transform.position = new Vector3(78, 0, -22);
                    r.transform.rotation = Quaternion.Euler(0, 180, 0);
                    EditorUtility.SetDirty(r);
                    var anim2 = r.GetComponentInChildren<Animator>();
                    var ctrl2 = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcAnimController);
                    if (anim2 != null && ctrl2 != null && anim2.runtimeAnimatorController == null)
                    {
                        anim2.runtimeAnimatorController = ctrl2;
                        EditorUtility.SetDirty(anim2);
                        Debug.Log("[InPlace.F3] Wired NPC.controller on scene-resident commander");
                    }
                    Debug.Log("[InPlace.F2] Repositioned scene-resident NPC_DaiDoiTruong");
                }
            }

            // 3. Make sure NPC.prefab's Visual Animator has a controller assigned
            //    so all runtime-spawned NPCs animate. Only assigns if missing
            //    (don't trample a designer choice).
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainAI/NPC.prefab");
            var ctrlAsset = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(NpcAnimController);
            if (prefab != null && ctrlAsset != null)
            {
                var content = PrefabUtility.LoadPrefabContents("Assets/Prefabs/TrainAI/NPC.prefab");
                var visual = content.transform.Find("Visual");
                if (visual != null)
                {
                    var anim3 = visual.GetComponent<Animator>();
                    if (anim3 != null && anim3.runtimeAnimatorController == null)
                    {
                        anim3.runtimeAnimatorController = ctrlAsset;
                        PrefabUtility.SaveAsPrefabAsset(content, "Assets/Prefabs/TrainAI/NPC.prefab");
                        Debug.Log("[InPlace.F3] Assigned NPC.controller to NPC.prefab Visual Animator");
                    }
                    else if (anim3 != null)
                    {
                        Debug.Log($"[InPlace.F3] NPC.prefab Animator already has controller — kept");
                    }
                }
                PrefabUtility.UnloadPrefabContents(content);
            }

            EditorSceneManager.MarkSceneDirty(worldScene);
            EditorSceneManager.SaveScene(worldScene);
        }

        // ====================================================================
        // helpers
        // ====================================================================
        static UnityEngine.SceneManagement.Scene EnsureBootstrapLoaded()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != BootstrapScenePath)
                scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            return scene;
        }

        static TMP_Text AddText(Transform parent, string name, string content, Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style, TextAlignmentOptions align, bool stretch = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            if (stretch)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.sizeDelta = size;
                rt.anchoredPosition = pos;
            }
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        static Button AddButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, bool primary)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = go.AddComponent<Image>();
            img.sprite = SciFiTheme.Load(primary ? "popup_btn_n" : "btn_common_n");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            var btn = go.AddComponent<Button>();
            AddText(go.transform, "Label", label, Vector2.zero, Vector2.zero, primary ? 26 : 22, SciFiTheme.TextWhite, FontStyles.Bold, TextAlignmentOptions.Center, stretch: true);
            return btn;
        }

        static Transform FindByName(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            for (int i = 0; i < parent.childCount; i++)
            {
                var f = FindByName(parent.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        // SerializedObject-backed field assignment so the scene file persists
        // the reference even on private [SerializeField] fields. Tolerates
        // missing field names (logs warn, doesn't throw) so the same builder
        // can target multiple controller versions.
        static void AssignField(Component comp, string fieldName, Object value)
        {
            if (comp == null) return;
            var so = new SerializedObject(comp);
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[InPlace] Field '{fieldName}' not found on {comp.GetType().Name} — skipping"); return; }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
        }
    }
}

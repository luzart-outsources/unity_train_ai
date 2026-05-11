using Luzart;
using Luzart.NewBase;
using TMPro;
using TrainAI.Configs;
using TrainAI.UI.Components;
using TrainAI.UI.HUD;
using TrainAI.UI.Screens;
using TrainAI.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.Editor.Setup
{
    public static class PrefabBuilder
    {
        private const string PrefabRoot = "Assets/Prefabs/TrainAI";
        private const string UIPrefabRoot = "Assets/Prefabs/TrainAI/UI";

        public class BuiltPrefabs
        {
            public GameObject Player;
            public GameObject NPC;
            public GameObject Interactable;
            public GameObject UIRootCanvas;
            public GameObject MainMenu, CharCreate, GameplayHud, Loading, Confirm,
                              Quiz, Dialogue, Ending, KickedOut, OpeningCutscene, Toast;
        }

        public static BuiltPrefabs CreateAll()
        {
            AssetScanner.EnsureFolder(PrefabRoot);
            AssetScanner.EnsureFolder(UIPrefabRoot);

            var built = new BuiltPrefabs();
            built.Player = CreatePlayerPrefab();
            built.NPC = CreateNPCPrefab();
            built.Interactable = CreateInteractablePrefab();

            built.UIRootCanvas = CreateUIRootCanvasPrefab();
            built.MainMenu = CreateMainMenuPrefab();
            built.CharCreate = CreateCharCreatePrefab();
            built.GameplayHud = CreateGameplayHudPrefab();
            built.Loading = CreateLoadingPrefab();
            built.Confirm = CreateConfirmPrefab();
            built.Quiz = CreateQuizPrefab();
            built.Dialogue = CreateDialoguePrefab();
            built.Ending = CreateEndingPrefab();
            built.KickedOut = CreateKickedOutPrefab();
            built.OpeningCutscene = CreateOpeningCutscenePrefab();
            built.Toast = CreateToastPrefab();

            AssetDatabase.SaveAssets();
            return built;
        }

        private static GameObject CreatePlayerPrefab()
        {
            string path = $"{PrefabRoot}/Player.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            var col = go.GetComponent<CapsuleCollider>();
            if (col != null) Object.DestroyImmediate(col);
            var cc = go.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0);
            cc.height = 2f;
            cc.radius = 0.5f;
            go.AddComponent<TrainAI.Player.PlayerController>();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateNPCPrefab()
        {
            string path = $"{PrefabRoot}/NPC_Generic.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "NPC_Generic";
            go.tag = "Untagged";
            var ren = go.GetComponent<MeshRenderer>();
            if (ren != null && ren.sharedMaterial != null)
            {
                var mat = new Material(ren.sharedMaterial);
                mat.color = new Color(0.3f, 0.6f, 0.9f);
                ren.material = mat;
            }
            go.AddComponent<NPCWaypointAgent>();

            // Trigger.
            var trigGo = new GameObject("Trigger");
            trigGo.transform.SetParent(go.transform, false);
            var sphere = trigGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2f;
            trigGo.AddComponent<InteractableTrigger>();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateInteractablePrefab()
        {
            string path = $"{PrefabRoot}/Interactable.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("Interactable");
            // Visual - small cube marker.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Marker";
            cube.transform.SetParent(go.transform, false);
            cube.transform.localScale = Vector3.one * 0.5f;
            Object.DestroyImmediate(cube.GetComponent<BoxCollider>());

            var sphere = go.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = 2f;
            go.AddComponent<InteractableTrigger>();

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        // ============ UI Prefabs ============

        private static GameObject CreateUIRootCanvasPrefab()
        {
            string path = $"{UIPrefabRoot}/UIRoot.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("UIRoot");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            // 6 lane child.
            string[] lanes = { "0_WorldOverlay", "1_Screen", "2_Hud", "3_Popup", "4_System", "5_Toast" };
            foreach (var ln in lanes)
            {
                var child = new GameObject(ln);
                child.transform.SetParent(go.transform, false);
                var rt = child.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            }

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject MakeFullScreenPanel(string name, Color bg)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = bg;
            return go;
        }

        private static GameObject AddText(GameObject parent, string text, int fontSize, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 pos, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var go = new GameObject("Text_" + text.Substring(0, Mathf.Min(text.Length, 10)));
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            var font = AssetScanner.FindDefaultTmpFont();
            if (font != null) t.font = font;
            return go;
        }

        private static GameObject AddButton(GameObject parent, string label, Vector2 anchorPos, Vector2 size, Sprite spr)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchorPos;
            var img = go.AddComponent<Image>();
            img.sprite = spr;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            go.AddComponent<Button>();

            var lbl = AddText(go, label, 28, new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            return go;
        }

        private static GameObject CreateMainMenuPrefab()
        {
            string path = $"{UIPrefabRoot}/MainMenuScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("MainMenuScreen", new Color(0.05f, 0.1f, 0.2f, 1f));
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<MainMenuScreen>();

            AddText(go, "Hoc ky quan doi", 96, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 200), new Vector2(0, -200));

            Sprite btnSpr = AssetScanner.FindKenneyButton("blue");
            var btnNew = AddButton(go, "Bat dau moi", new Vector2(0, 100), new Vector2(400, 80), btnSpr);
            var btnCont = AddButton(go, "Tiep tuc", new Vector2(0, 0), new Vector2(400, 80), btnSpr);
            var btnQuit = AddButton(go, "Thoat", new Vector2(0, -100), new Vector2(400, 80), btnSpr);

            // Wire serialized fields via SerializedObject.
            var so = new SerializedObject(screen);
            so.FindProperty("btnNewGame").objectReferenceValue = btnNew.GetComponent<Button>();
            so.FindProperty("btnContinue").objectReferenceValue = btnCont.GetComponent<Button>();
            so.FindProperty("btnQuit").objectReferenceValue = btnQuit.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            AddTweenFadeIn(go);

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateCharCreatePrefab()
        {
            string path = $"{UIPrefabRoot}/CharacterCreateScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("CharacterCreateScreen", new Color(0.05f, 0.1f, 0.2f, 1f));
            var screen = go.AddComponent<CharacterCreateScreen>();

            var promptGo = AddText(go, "Ban can dien ten truoc khi vao game", 36,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 60), new Vector2(0, 100));

            // InputField TMP.
            var inputGo = new GameObject("InputName");
            inputGo.transform.SetParent(go.transform, false);
            var inRt = inputGo.AddComponent<RectTransform>();
            inRt.anchorMin = new Vector2(0.5f, 0.5f); inRt.anchorMax = new Vector2(0.5f, 0.5f);
            inRt.sizeDelta = new Vector2(500, 60); inRt.anchoredPosition = new Vector2(0, 0);
            var inImg = inputGo.AddComponent<Image>();
            inImg.color = new Color(1, 1, 1, 0.2f);
            var input = inputGo.AddComponent<TMP_InputField>();

            // Text Area child for InputField.
            var area = new GameObject("Text Area");
            area.transform.SetParent(inputGo.transform, false);
            var areaRt = area.AddComponent<RectTransform>();
            areaRt.anchorMin = Vector2.zero; areaRt.anchorMax = Vector2.one;
            areaRt.sizeDelta = Vector2.zero; areaRt.anchoredPosition = Vector2.zero;
            var mask = area.AddComponent<RectMask2D>();

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(area.transform, false);
            var tRt = textGo.AddComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero; tRt.anchoredPosition = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 28;
            tmp.color = Color.white;
            var font = AssetScanner.FindDefaultTmpFont();
            if (font != null) tmp.font = font;

            input.textViewport = areaRt;
            input.textComponent = tmp;

            Sprite btnSpr = AssetScanner.FindKenneyButton("green");
            var btnConfirm = AddButton(go, "Xac nhan", new Vector2(0, -100), new Vector2(400, 80), btnSpr);

            var so = new SerializedObject(screen);
            so.FindProperty("inputName").objectReferenceValue = input;
            so.FindProperty("btnConfirm").objectReferenceValue = btnConfirm.GetComponent<Button>();
            so.FindProperty("txtPrompt").objectReferenceValue = promptGo.GetComponent<TextMeshProUGUI>();
            so.ApplyModifiedPropertiesWithoutUndo();

            AddTweenFadeIn(go);

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateGameplayHudPrefab()
        {
            string path = $"{UIPrefabRoot}/GameplayHud.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("GameplayHud");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
            var hud = go.AddComponent<GameplayHud>();

            // Clock (top center).
            var clockGo = new GameObject("ClockHUD");
            clockGo.transform.SetParent(go.transform, false);
            var crt = clockGo.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1f); crt.anchorMax = new Vector2(0.5f, 1f);
            crt.sizeDelta = new Vector2(400, 100); crt.anchoredPosition = new Vector2(0, -60);
            var clockImg = clockGo.AddComponent<Image>();
            clockImg.color = new Color(0, 0, 0, 0.5f);
            var clockHud = clockGo.AddComponent<ClockHUD>();
            var txtClock = AddText(clockGo, "05:00", 48, new Vector2(0, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var txtDay = AddText(clockGo, "T2, Ngay 1", 24, new Vector2(0, 0), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            new SerializedObject(clockHud).Apply(("txtClock", txtClock.GetComponent<TextMeshProUGUI>()), ("txtDay", txtDay.GetComponent<TextMeshProUGUI>()));

            // Quest HUD (right, below minimap).
            var questGo = new GameObject("QuestHUD");
            questGo.transform.SetParent(go.transform, false);
            var qrt = questGo.AddComponent<RectTransform>();
            qrt.anchorMin = new Vector2(1, 1); qrt.anchorMax = new Vector2(1, 1);
            qrt.sizeDelta = new Vector2(400, 80); qrt.anchoredPosition = new Vector2(-220, -260);
            var questImg = questGo.AddComponent<Image>();
            questImg.color = new Color(0, 0, 0, 0.5f);
            var qHud = questGo.AddComponent<QuestHUD>();
            var qTitle = AddText(questGo, "Tap the duc", 28, new Vector2(0, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var qTime = AddText(questGo, "(05:15)", 22, new Vector2(0, 0), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            new SerializedObject(qHud).Apply(("txtQuestTitle", qTitle.GetComponent<TextMeshProUGUI>()), ("txtQuestTime", qTime.GetComponent<TextMeshProUGUI>()));

            // Score HUD (right top).
            var scoreGo = new GameObject("ScoreHUD");
            scoreGo.transform.SetParent(go.transform, false);
            var srt = scoreGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1); srt.anchorMax = new Vector2(0, 1);
            srt.sizeDelta = new Vector2(280, 100); srt.anchoredPosition = new Vector2(160, -60);
            var scoreImg = scoreGo.AddComponent<Image>();
            scoreImg.color = new Color(0, 0, 0, 0.5f);
            var sHud = scoreGo.AddComponent<ScoreHUD>();
            var txtDiscipline = AddText(scoreGo, "RL: 100/100", 22, new Vector2(0, 0.5f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            var txtAcademic = AddText(scoreGo, "HT: 0/480", 22, new Vector2(0, 0), new Vector2(1, 0.5f), Vector2.zero, Vector2.zero);
            new SerializedObject(sHud).Apply(("txtDiscipline", txtDiscipline.GetComponent<TextMeshProUGUI>()), ("txtAcademic", txtAcademic.GetComponent<TextMeshProUGUI>()));

            // Joystick (bottom left).
            var joyBg = new GameObject("JoystickBackground");
            joyBg.transform.SetParent(go.transform, false);
            var jrt = joyBg.AddComponent<RectTransform>();
            jrt.anchorMin = new Vector2(0, 0); jrt.anchorMax = new Vector2(0, 0);
            jrt.sizeDelta = new Vector2(200, 200); jrt.anchoredPosition = new Vector2(150, 150);
            var joyImg = joyBg.AddComponent<Image>();
            joyImg.color = new Color(1, 1, 1, 0.3f);
            var joy = joyBg.AddComponent<JoystickWidget>();

            var joyHandle = new GameObject("Handle");
            joyHandle.transform.SetParent(joyBg.transform, false);
            var hrt = joyHandle.AddComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(80, 80);
            hrt.anchoredPosition = Vector2.zero;
            var hImg = joyHandle.AddComponent<Image>();
            hImg.color = new Color(1, 1, 1, 0.6f);
            new SerializedObject(joy).Apply(("background", joyBg.GetComponent<RectTransform>()), ("handle", joyHandle.GetComponent<RectTransform>()));

            // Interact Button (bottom right).
            var ibGo = new GameObject("InteractButton");
            ibGo.transform.SetParent(go.transform, false);
            var ibrt = ibGo.AddComponent<RectTransform>();
            ibrt.anchorMin = new Vector2(1, 0); ibrt.anchorMax = new Vector2(1, 0);
            ibrt.sizeDelta = new Vector2(160, 160); ibrt.anchoredPosition = new Vector2(-150, 150);
            var ibImg = ibGo.AddComponent<Image>();
            var btnSpr = AssetScanner.FindKenneyButton("yellow") ?? AssetScanner.FindKenneyButton("blue");
            ibImg.sprite = btnSpr;
            var ibBtn = ibGo.AddComponent<Button>();
            var ibTxt = AddText(ibGo, "INTERACT", 22, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sel = ibGo.AddComponent<SelectToggleImage>();
            // sel.imSelect/spSelect/spUnSelect cau hinh thu cong neu can. De default sang/toi co bang Button.interactable.
            var ibComp = ibGo.AddComponent<InteractButton>();
            new SerializedObject(ibComp).Apply(("state", sel), ("button", ibBtn));

            new SerializedObject(hud).Apply(
                ("clockHud", clockHud),
                ("questHud", qHud),
                ("scoreHud", sHud),
                ("interactButton", ibComp),
                ("joystick", joy));

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateLoadingPrefab()
        {
            string path = $"{UIPrefabRoot}/LoadingScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("LoadingScreen", new Color(0, 0, 0, 1f));
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<LoadingScreen>();
            var txt = AddText(go, "Dang chuyen canh...", 36, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 60), Vector2.zero);
            new SerializedObject(screen).Apply(("txtMessage", txt.GetComponent<TextMeshProUGUI>()));
            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateConfirmPrefab()
        {
            string path = $"{UIPrefabRoot}/ConfirmScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("ConfirmScreen", new Color(0, 0, 0, 0.6f));
            go.AddComponent<CanvasGroup>();

            // Inner panel.
            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(800, 400); prt.anchoredPosition = Vector2.zero;
            var panelImg = panel.AddComponent<Image>();
            panelImg.sprite = AssetScanner.FindKenneyPanel();
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.95f, 0.9f, 0.8f, 1f);

            var screen = go.AddComponent<ConfirmScreen>();
            var title = AddText(panel, "Xac nhan", 36, new Vector2(0, 0.7f), new Vector2(1, 1), Vector2.zero, Vector2.zero);
            title.GetComponent<TextMeshProUGUI>().color = Color.black;
            var msg = AddText(panel, "Ban dang lam mot viec.", 28, new Vector2(0, 0.3f), new Vector2(1, 0.7f), Vector2.zero, Vector2.zero);
            msg.GetComponent<TextMeshProUGUI>().color = Color.black;

            var btnSpr = AssetScanner.FindKenneyButton("blue");
            var btnOk = AddButton(panel, "OK", new Vector2(0, -120), new Vector2(280, 80), btnSpr);
            var lblOk = btnOk.GetComponentInChildren<TextMeshProUGUI>();

            new SerializedObject(screen).Apply(
                ("txtTitle", title.GetComponent<TextMeshProUGUI>()),
                ("txtMessage", msg.GetComponent<TextMeshProUGUI>()),
                ("btnOk", btnOk.GetComponent<Button>()),
                ("txtOkLabel", lblOk));

            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateQuizPrefab()
        {
            string path = $"{UIPrefabRoot}/QuizScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("QuizScreen", new Color(0, 0, 0, 0.7f));
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<QuizScreen>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(1200, 700); prt.anchoredPosition = Vector2.zero;
            var pimg = panel.AddComponent<Image>();
            pimg.sprite = AssetScanner.FindKenneyPanel();
            pimg.type = Image.Type.Sliced;
            pimg.color = new Color(0.95f, 0.9f, 0.8f, 1f);

            var stem = AddText(panel, "Cau hoi 1?", 32, new Vector2(0, 0.65f), new Vector2(1, 0.9f), Vector2.zero, Vector2.zero);
            stem.GetComponent<TextMeshProUGUI>().color = Color.black;
            var counter = AddText(panel, "1/10", 24, new Vector2(0, 0.9f), new Vector2(0.2f, 1f), Vector2.zero, Vector2.zero);
            counter.GetComponent<TextMeshProUGUI>().color = Color.black;
            var countdown = AddText(panel, "15", 32, new Vector2(0.85f, 0.9f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            countdown.GetComponent<TextMeshProUGUI>().color = Color.red;

            // 4 answer buttons + feedback.
            Sprite btnSpr = AssetScanner.FindKenneyButton("blue");
            Button[] btns = new Button[4];
            TextMeshProUGUI[] btnTxts = new TextMeshProUGUI[4];
            SelectSwitchImage[] feedbacks = new SelectSwitchImage[4];
            for (int i = 0; i < 4; i++)
            {
                float yPos = 100f - i * 100f;
                var b = AddButton(panel, $"Dap an {(char)('A' + i)}", new Vector2(0, yPos), new Vector2(800, 80), btnSpr);
                btns[i] = b.GetComponent<Button>();
                btnTxts[i] = b.GetComponentInChildren<TextMeshProUGUI>();

                // SelectSwitchImage tren image cua button (sprite tho/sang/sai).
                var sel = b.AddComponent<SelectSwitchImage>();
                feedbacks[i] = sel;
            }

            var btnContinue = AddButton(panel, "Tiep tuc", new Vector2(0, -290), new Vector2(280, 70), btnSpr);

            new SerializedObject(screen).Apply(
                ("txtStem", stem.GetComponent<TextMeshProUGUI>()),
                ("txtCounter", counter.GetComponent<TextMeshProUGUI>()),
                ("txtCountdown", countdown.GetComponent<TextMeshProUGUI>()),
                ("btnContinue", btnContinue.GetComponent<Button>()));

            // Wire arrays.
            var so = new SerializedObject(screen);
            var arrBtns = so.FindProperty("btnAnswers");
            var arrTxts = so.FindProperty("txtAnswers");
            var arrFb = so.FindProperty("answerFeedback");
            arrBtns.arraySize = 4; arrTxts.arraySize = 4; arrFb.arraySize = 4;
            for (int i = 0; i < 4; i++)
            {
                arrBtns.GetArrayElementAtIndex(i).objectReferenceValue = btns[i];
                arrTxts.GetArrayElementAtIndex(i).objectReferenceValue = btnTxts[i];
                arrFb.GetArrayElementAtIndex(i).objectReferenceValue = feedbacks[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateDialoguePrefab()
        {
            string path = $"{UIPrefabRoot}/DialogueScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("DialogueScreen", new Color(0, 0, 0, 0.6f));
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<DialogueScreen>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(go.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0, 0); prt.anchorMax = new Vector2(1, 0.4f);
            prt.sizeDelta = Vector2.zero;
            prt.offsetMin = new Vector2(60, 60); prt.offsetMax = new Vector2(-60, 0);
            var pimg = panel.AddComponent<Image>();
            pimg.sprite = AssetScanner.FindKenneyPanel();
            pimg.type = Image.Type.Sliced;
            pimg.color = new Color(0.95f, 0.9f, 0.8f, 1f);

            var name = AddText(panel, "NPC", 28, new Vector2(0, 0.85f), new Vector2(0.4f, 1), Vector2.zero, Vector2.zero);
            name.GetComponent<TextMeshProUGUI>().color = Color.black;

            // Bubble container (scroll area placeholder).
            var bubbles = new GameObject("Bubbles");
            bubbles.transform.SetParent(panel.transform, false);
            var brt = bubbles.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0, 0.3f); brt.anchorMax = new Vector2(1, 0.85f);
            brt.sizeDelta = Vector2.zero;
            var vlayout = bubbles.AddComponent<VerticalLayoutGroup>();
            vlayout.childAlignment = TextAnchor.LowerLeft;
            vlayout.spacing = 8f;

            // Input.
            var inputGo = new GameObject("Input");
            inputGo.transform.SetParent(panel.transform, false);
            var irt = inputGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0); irt.anchorMax = new Vector2(0.7f, 0.25f);
            irt.sizeDelta = Vector2.zero;
            irt.offsetMin = new Vector2(20, 20); irt.offsetMax = new Vector2(-20, -10);
            var inImg = inputGo.AddComponent<Image>();
            inImg.color = Color.white;
            var input = inputGo.AddComponent<TMP_InputField>();
            var iText = AddText(inputGo, "", 24, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.MidlineLeft);
            iText.GetComponent<TextMeshProUGUI>().color = Color.black;
            input.textComponent = iText.GetComponent<TextMeshProUGUI>();

            var btnSpr = AssetScanner.FindKenneyButton("blue");
            var btnSend = AddButton(panel, "Gui", new Vector2(0, 0), new Vector2(150, 60), btnSpr);
            var btnSendRt = btnSend.GetComponent<RectTransform>();
            btnSendRt.anchorMin = new Vector2(0.7f, 0); btnSendRt.anchorMax = new Vector2(1, 0.25f);
            btnSendRt.offsetMin = new Vector2(20, 20); btnSendRt.offsetMax = new Vector2(-20, -10);
            btnSendRt.sizeDelta = Vector2.zero;

            new SerializedObject(screen).Apply(
                ("inputField", input),
                ("btnSend", btnSend.GetComponent<Button>()),
                ("txtNpcName", name.GetComponent<TextMeshProUGUI>()),
                ("bubbleContainer", bubbles.transform));

            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateEndingPrefab()
        {
            string path = $"{UIPrefabRoot}/EndingScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("EndingScreen", new Color(0.05f, 0.1f, 0.2f, 1f));
            go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<EndingScreen>();

            var title = AddText(go, "Ban da hoan thanh khoa hoc quan su!", 48,
                new Vector2(0, 0.7f), new Vector2(1, 0.85f), Vector2.zero, Vector2.zero);
            var academic = AddText(go, "Diem hoc tap: 0/480", 28,
                new Vector2(0, 0.55f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            var discipline = AddText(go, "Diem ren luyen: 100/100", 28,
                new Vector2(0, 0.45f), new Vector2(1, 0.55f), Vector2.zero, Vector2.zero);
            var rank = AddText(go, "Tot nghiep loai: -", 36,
                new Vector2(0, 0.3f), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero);
            var sel = rank.AddComponent<SelectSwitchTMP_Text>();

            var btnSpr = AssetScanner.FindKenneyButton("blue");
            var btnBack = AddButton(go, "Ve menu chinh", new Vector2(0, -300), new Vector2(400, 80), btnSpr);

            new SerializedObject(screen).Apply(
                ("txtTitle", title.GetComponent<TextMeshProUGUI>()),
                ("txtAcademic", academic.GetComponent<TextMeshProUGUI>()),
                ("txtDiscipline", discipline.GetComponent<TextMeshProUGUI>()),
                ("txtRank", rank.GetComponent<TextMeshProUGUI>()),
                ("rankSelector", sel),
                ("btnBackToMenu", btnBack.GetComponent<Button>()));

            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateKickedOutPrefab()
        {
            string path = $"{UIPrefabRoot}/KickedOutScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("KickedOutScreen", new Color(0.2f, 0.05f, 0.05f, 0.95f));
            var screen = go.AddComponent<KickedOutScreen>();
            var msg = AddText(go, "Ban bi duoi hoc!", 48, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(800, 80), new Vector2(0, 100));
            var btnSpr = AssetScanner.FindKenneyButton("red") ?? AssetScanner.FindKenneyButton("blue");
            var btn = AddButton(go, "Xac nhan", new Vector2(0, -50), new Vector2(400, 80), btnSpr);
            new SerializedObject(screen).Apply(("txtMessage", msg.GetComponent<TextMeshProUGUI>()), ("btnConfirm", btn.GetComponent<Button>()));
            AddTweenFadeIn(go);
            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateOpeningCutscenePrefab()
        {
            string path = $"{UIPrefabRoot}/OpeningCutsceneScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = MakeFullScreenPanel("OpeningCutsceneScreen", Color.black);
            var screen = go.AddComponent<OpeningCutsceneScreen>();

            // VideoPlayer + RawImage.
            var video = go.AddComponent<UnityEngine.Video.VideoPlayer>();
            video.playOnAwake = false;
            video.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;

            var btnSpr = AssetScanner.FindKenneyButton("grey");
            var btnSkip = AddButton(go, "Skip >>", new Vector2(700, -400), new Vector2(180, 60), btnSpr);

            new SerializedObject(screen).Apply(("videoPlayer", video), ("btnSkip", btnSkip.GetComponent<Button>()));

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        private static GameObject CreateToastPrefab()
        {
            string path = $"{UIPrefabRoot}/ToastScreen.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("ToastScreen");
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.9f); rt.anchorMax = new Vector2(0.5f, 0.9f);
            rt.sizeDelta = new Vector2(700, 80);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.7f, 0.9f);
            var cg = go.AddComponent<CanvasGroup>();
            var screen = go.AddComponent<ToastScreen>();

            var msg = AddText(go, "Toast message", 28, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            new SerializedObject(screen).Apply(
                ("txtMessage", msg.GetComponent<TextMeshProUGUI>()),
                ("canvasGroup", cg),
                ("imgBg", img));

            var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return saved;
        }

        // Add 1 simple TweenAnimation FadeByCanvasGroup with TweenAnimationCaller OnEnable.
        private static void AddTweenFadeIn(GameObject root)
        {
            var cg = root.GetComponent<CanvasGroup>();
            if (cg == null) cg = root.AddComponent<CanvasGroup>();
            var tween = root.AddComponent<TweenAnimation>();
            // Default field "typeAnimation" = Move; set qua SerializedObject.
            var sob = new SerializedObject(tween);
            var typeProp = sob.FindProperty("typeAnimation");
            if (typeProp != null) typeProp.enumValueIndex = (int)EAnimation.FadeByCanvasGroup;
            // settings.General.Target = cg.
            var settings = sob.FindProperty("tweenAnimationSettings");
            if (settings != null)
            {
                var general = settings.FindPropertyRelative("General");
                if (general != null)
                {
                    var target = general.FindPropertyRelative("Target");
                    if (target != null) target.objectReferenceValue = cg;
                    var dur = general.FindPropertyRelative("Duration");
                    if (dur != null) dur.floatValue = 0.3f;
                }
                var values = settings.FindPropertyRelative("Values");
                if (values != null)
                {
                    var ofrom = values.FindPropertyRelative("OverrideFrom");
                    if (ofrom != null) ofrom.boolValue = true;
                    var oto = values.FindPropertyRelative("OverrideTo");
                    if (oto != null) oto.boolValue = true;
                    var floatFrom = values.FindPropertyRelative("FloatFrom");
                    if (floatFrom != null) floatFrom.floatValue = 0f;
                    var floatTo = values.FindPropertyRelative("FloatTo");
                    if (floatTo != null) floatTo.floatValue = 1f;
                }
            }
            sob.ApplyModifiedPropertiesWithoutUndo();

            var caller = root.AddComponent<TweenAnimationCaller>();
            new SerializedObject(caller).Apply(("tweenAnimation", tween));
        }
    }

    // Extension helper de wire serialized fields gon hon.
    internal static class SerializedObjectExtensions
    {
        public static void Apply(this SerializedObject so, params (string field, Object value)[] pairs)
        {
            foreach (var (f, v) in pairs)
            {
                var prop = so.FindProperty(f);
                if (prop != null) prop.objectReferenceValue = v;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

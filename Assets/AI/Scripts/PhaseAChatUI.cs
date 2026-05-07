// PhaseAChatUI.cs
//
// Unity UI (Canvas-based) cho Phase A chat. Thay thế PhaseAChatTester (IMGUI).
// Editor builder tạo full Canvas hierarchy với Background + Title + ScrollView
// + InputField + SendButton, wire references qua Inspector. Khi Play, script
// chỉ làm logic: classify + add chat bubbles.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(NPCDialogueBrain))]
public class PhaseAChatUI : MonoBehaviour
{
    [Header("UI refs — set bởi editor builder")]
    public Text titleText;
    public Text statusText;
    public RectTransform contentParent;  // ScrollView/Viewport/Content
    public ScrollRect scrollRect;
    public InputField inputField;
    public Button sendButton;

    private NPCDialogueBrain _brain;
    private int _passCount = 0;
    private Font _font;

    // Auto sanity samples
    private static readonly (string text, string expected)[] Samples = new[]
    {
        ("Mấy giờ thì ăn cơm",       "HOI_GIO_AN"),
        ("Phòng học ở đâu vậy",      "HOI_VI_TRI"),
        ("Em xin phép về quê",       "XIN_PHEP"),
        ("Wifi yếu quá",              "OUT_OF_SCOPE"),
        ("Súng AK47 dùng thế nào",   "HOI_KIEN_THUC"),
    };

    void Awake()
    {
        _brain = GetComponent<NPCDialogueBrain>();
        // Use Arial OS font — supports Vietnamese diacritics
        _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    void Start()
    {
        Debug.Log("══ PHASE A — Intent Classifier Chat (Canvas UI) ══");

        if (_brain == null || !_brain.enabled)
        {
            Debug.LogError("[PhaseA] NPCDialogueBrain not ready");
            if (statusText != null) statusText.text = "❌ Brain not ready — check Inspector";
            return;
        }

        if (sendButton != null) sendButton.onClick.AddListener(OnSend);
        // KHÔNG dùng Input.GetKeyDown — project Unity 6 set activeInputHandler=1
        // (Input System only). Bắt Enter qua Update() + Keyboard.current.

        // Auto sanity
        Debug.Log("┌─ Sanity (5 sample) ─");
        foreach (var (text, expected) in Samples)
        {
            var (intent, conf) = _brain.Classify(text);
            string reply = _brain.Respond(text);
            bool ok = intent == expected;
            if (ok) _passCount++;
            AddBubble(text, isUser: true, subInfo: null);
            AddBubble(reply, isUser: false,
                      subInfo: $"{intent} ({conf * 100:F0}%) — expect {expected} {(ok ? "✓" : "✗")}",
                      subColor: ok ? new Color(0.55f, 0.85f, 0.55f) : new Color(0.95f, 0.45f, 0.45f));
            Debug.Log($"│ {(ok ? "✓" : "✗")} \"{text}\" → {intent} ({conf * 100:F1}%)");
        }
        Debug.Log($"│ Score {_passCount}/{Samples.Length}");

        if (statusText != null)
            statusText.text = $"Sanity: {_passCount}/{Samples.Length} đúng. Gõ tiếng Việt vào ô dưới để test thêm.";

        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        // Scroll to bottom after 1 frame to allow layout to compute
        StartCoroutine(ScrollToBottomNextFrame());
    }

    System.Collections.IEnumerator ScrollToBottomNextFrame()
    {
        yield return null;
        yield return null;
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
    }

    void Update()
    {
        // Bắt Enter để submit — Unity 6 Input System
#if ENABLE_INPUT_SYSTEM
        if (inputField != null && inputField.isFocused
            && Keyboard.current != null
            && (Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            // Defer 1 frame vì Enter có thể vừa được consume bởi InputField
            OnSend();
        }
#endif
    }

    void OnSend()
    {
        if (inputField == null) return;
        string text = inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var (intent, conf) = _brain.Classify(text);
        string reply = _brain.Respond(text);
        AddBubble(text, isUser: true, subInfo: null);
        AddBubble(reply, isUser: false,
                  subInfo: $"{intent} ({conf * 100:F0}%)",
                  subColor: new Color(0.7f, 0.9f, 0.7f));

        inputField.text = "";
        inputField.Select();
        inputField.ActivateInputField();
        Debug.Log($"[PhaseA] \"{text}\" → {intent} ({conf * 100:F1}%) | {reply}");

        StartCoroutine(ScrollToBottomNextFrame());
    }

    // ─────────────────────────────────────────────────────────────────────
    // Tạo chat bubble runtime với Image + Text(s) + ContentSizeFitter
    // ─────────────────────────────────────────────────────────────────────
    void AddBubble(string content, bool isUser, string subInfo, Color? subColor = null)
    {
        if (contentParent == null) return;

        // Outer wrapper với HorizontalLayoutGroup để align trái/phải
        var wrapper = new GameObject(isUser ? "UserMsg" : "NPCMsg", typeof(RectTransform));
        wrapper.transform.SetParent(contentParent, false);
        var wrapperLayout = wrapper.AddComponent<HorizontalLayoutGroup>();
        wrapperLayout.padding = new RectOffset(0, 0, 4, 4);
        wrapperLayout.childAlignment = isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
        wrapperLayout.childControlWidth = false;
        wrapperLayout.childControlHeight = false;
        wrapperLayout.childForceExpandWidth = false;
        wrapperLayout.childForceExpandHeight = false;
        var wrapperFitter = wrapper.AddComponent<ContentSizeFitter>();
        wrapperFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        wrapperFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var wrapperLE = wrapper.AddComponent<LayoutElement>();
        wrapperLE.flexibleWidth = 1;

        // Bubble với background color
        var bubble = new GameObject("Bubble", typeof(RectTransform));
        bubble.transform.SetParent(wrapper.transform, false);
        var bg = bubble.AddComponent<Image>();
        bg.color = isUser ? new Color(0.18f, 0.4f, 0.75f, 0.95f)
                          : new Color(0.22f, 0.22f, 0.28f, 0.95f);
        var bubbleLayout = bubble.AddComponent<VerticalLayoutGroup>();
        bubbleLayout.padding = new RectOffset(14, 14, 10, 10);
        bubbleLayout.spacing = 4;
        bubbleLayout.childAlignment = TextAnchor.UpperLeft;
        bubbleLayout.childControlWidth = true;
        bubbleLayout.childControlHeight = true;
        bubbleLayout.childForceExpandWidth = false;
        bubbleLayout.childForceExpandHeight = false;
        var bubbleFitter = bubble.AddComponent<ContentSizeFitter>();
        bubbleFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        bubbleFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var bubbleLE = bubble.AddComponent<LayoutElement>();
        bubbleLE.preferredWidth = 600;  // max bubble width

        // Main content text
        var mainGo = new GameObject("Main", typeof(RectTransform));
        mainGo.transform.SetParent(bubble.transform, false);
        var mainText = mainGo.AddComponent<Text>();
        mainText.text = content;
        mainText.font = _font;
        mainText.fontSize = 18;
        mainText.color = Color.white;
        mainText.alignment = TextAnchor.UpperLeft;
        mainText.horizontalOverflow = HorizontalWrapMode.Wrap;
        mainText.verticalOverflow = VerticalWrapMode.Overflow;

        // Sub-info text (if any)
        if (!string.IsNullOrEmpty(subInfo))
        {
            var subGo = new GameObject("SubInfo", typeof(RectTransform));
            subGo.transform.SetParent(bubble.transform, false);
            var subText = subGo.AddComponent<Text>();
            subText.text = subInfo;
            subText.font = _font;
            subText.fontSize = 14;
            subText.color = subColor ?? new Color(0.7f, 0.9f, 0.7f);
            subText.alignment = TextAnchor.UpperLeft;
            subText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subText.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}

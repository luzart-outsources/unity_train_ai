
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCDialogueBrain))]
public class PhaseAChatTester : MonoBehaviour
{
    private NPCDialogueBrain _brain;

    private struct ChatEntry
    {
        public bool isUser;       // true = user input, false = NPC reply / system
        public string content;
        public string subInfo;    // intent + confidence (cho NPC reply)
    }

    private List<ChatEntry> _entries = new List<ChatEntry>();
    private string _inputText = "";
    private Vector2 _scroll = Vector2.zero;
    private bool _focusOnNextRepaint = true;
    private bool _ready = false;
    private int _passCount = 0;
    private int _totalSamples = 0;

    private static readonly (string text, string expected)[] Samples = new[]
    {
        ("Mấy giờ thì ăn cơm",       "HOI_GIO_AN"),
        ("Phòng học ở đâu vậy",      "HOI_VI_TRI"),
        ("Em xin phép về quê",       "XIN_PHEP"),
        ("Wifi yếu quá",              "OUT_OF_SCOPE"),
        ("Súng AK47 dùng thế nào",   "HOI_KIEN_THUC"),
    };

    void Awake() => _brain = GetComponent<NPCDialogueBrain>();

    void Start()
    {
        Debug.Log("== PHASE A — Intent Classifier Chat Test ==");
        if (_brain == null || !_brain.enabled)
        {
            Debug.LogError("[PhaseA] NPCDialogueBrain not ready");
            return;
        }

        // Auto sanity test
        Debug.Log("Auto sanity (5 sample) -");
        _totalSamples = Samples.Length;
        foreach (var (text, expected) in Samples)
        {
            var (intent, conf) = _brain.Classify(text);
            string reply = _brain.Respond(text);
            bool ok = intent == expected;
            if (ok) _passCount++;
            _entries.Add(new ChatEntry { isUser = true, content = text });
            _entries.Add(new ChatEntry
            {
                isUser = false,
                content = reply,
                subInfo = $"{intent} ({conf*100:F0}%) — expect {expected} {(ok ? "ok" : "fail")}"
            });
            Debug.Log($"| {(ok ? "ok" : "fail")} \"{text}\" -> {intent} ({conf*100:F1}%, expect {expected})");
        }
        Debug.Log($"| Score: {_passCount}/{_totalSamples}");
        Debug.Log("");
        _ready = true;
    }

    void OnGUI()
    {
        if (!_ready) return;

        // Big bold title bar
        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        var subStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = new Color(0.8f, 0.8f, 0.8f) } };
        var userStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 8, 8), wordWrap = true, normal = { textColor = Color.white, background = MakeTex(new Color(0.2f, 0.4f, 0.7f)) } };
        var npcStyle = new GUIStyle(GUI.skin.box) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 12, 8, 8), wordWrap = true, normal = { textColor = Color.white, background = MakeTex(new Color(0.25f, 0.25f, 0.3f)) } };
        var subInfoStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(0.6f, 0.8f, 0.6f) }, padding = new RectOffset(12, 0, 0, 4) };
        var inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18, padding = new RectOffset(10, 10, 8, 8) };
        var sendStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };

        float w = Screen.width;
        float h = Screen.height;
        float padding = 30f;

        GUI.Box(new Rect(0, 0, w, h), GUIContent.none);

        // Header
        var headerRect = new Rect(padding, padding, w - 2 * padding, 70);
        GUI.Label(headerRect, "Phase A — NPC Chỉ Huy Chat", titleStyle);
        GUI.Label(new Rect(padding, padding + 35, w - 2 * padding, 30),
                  $"Sanity test: {_passCount}/{_totalSamples} — Gõ tiếng Việt để chat thêm",
                  subStyle);

        // Chat history scroll
        var historyHeight = h - 200f;
        var historyRect = new Rect(padding, 110, w - 2 * padding, historyHeight);
        GUI.Box(historyRect, GUIContent.none);

        var contentRect = new Rect(0, 0, historyRect.width - 25, EstimateContentHeight(historyRect.width - 50, userStyle, npcStyle, subInfoStyle));
        _scroll = GUI.BeginScrollView(historyRect, _scroll, contentRect);

        float y = 10;
        float bubbleMaxW = historyRect.width - 60;
        foreach (var e in _entries)
        {
            var style = e.isUser ? userStyle : npcStyle;
            var bubbleW = Mathf.Min(bubbleMaxW, style.CalcSize(new GUIContent(e.content)).x + 30);
            bubbleW = Mathf.Max(bubbleW, 100);
            var bubbleH = style.CalcHeight(new GUIContent(e.content), bubbleW);

            float x = e.isUser ? historyRect.width - bubbleW - 40 : 20;
            GUI.Box(new Rect(x, y, bubbleW, bubbleH), e.content, style);
            y += bubbleH;

            if (!e.isUser && !string.IsNullOrEmpty(e.subInfo))
            {
                GUI.Label(new Rect(x, y, bubbleW, 18), e.subInfo, subInfoStyle);
                y += 22;
            }
            y += 8;
        }
        GUI.EndScrollView();

        // Input row at bottom
        float inputY = h - 80f;
        float buttonW = 120f;
        var inputRect = new Rect(padding, inputY, w - 2 * padding - buttonW - 10, 50);
        var sendRect = new Rect(w - padding - buttonW, inputY, buttonW, 50);

        GUI.SetNextControlName("ChatInput");
        _inputText = GUI.TextField(inputRect, _inputText, inputStyle);

        bool sendClicked = GUI.Button(sendRect, "Gửi", sendStyle);
        bool enterPressed = Event.current.type == EventType.KeyDown
                            && (Event.current.keyCode == KeyCode.Return
                                || Event.current.keyCode == KeyCode.KeypadEnter);

        if ((sendClicked || enterPressed) && !string.IsNullOrWhiteSpace(_inputText))
        {
            Submit(_inputText.Trim());
            _inputText = "";
            _focusOnNextRepaint = true;
            if (enterPressed) Event.current.Use();
        }

        // Auto-focus input
        if (_focusOnNextRepaint && Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl("ChatInput");
            _focusOnNextRepaint = false;
        }
    }

    void Submit(string text)
    {
        var (intent, conf) = _brain.Classify(text);
        string reply = _brain.Respond(text);
        _entries.Add(new ChatEntry { isUser = true, content = text });
        _entries.Add(new ChatEntry
        {
            isUser = false,
            content = reply,
            subInfo = $"{intent} ({conf*100:F0}%)"
        });
        // Auto-scroll to bottom
        _scroll.y = float.MaxValue;
        Debug.Log($"[PhaseA] \"{text}\" -> {intent} ({conf*100:F1}%) | {reply}");
    }

    float EstimateContentHeight(float bubbleMaxW, GUIStyle u, GUIStyle n, GUIStyle s)
    {
        float total = 20;
        foreach (var e in _entries)
        {
            var style = e.isUser ? u : n;
            float bw = Mathf.Min(bubbleMaxW, style.CalcSize(new GUIContent(e.content)).x + 30);
            bw = Mathf.Max(bw, 100);
            total += style.CalcHeight(new GUIContent(e.content), bw);
            if (!e.isUser && !string.IsNullOrEmpty(e.subInfo)) total += 22;
            total += 8;
        }
        return total;
    }

    // Helper — tao solid color texture cho box backgrounds
    private Dictionary<Color, Texture2D> _texCache = new Dictionary<Color, Texture2D>();
    Texture2D MakeTex(Color c)
    {
        if (_texCache.TryGetValue(c, out var existing) && existing != null) return existing;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, c);
        tex.Apply();
        _texCache[c] = tex;
        return tex;
    }
}

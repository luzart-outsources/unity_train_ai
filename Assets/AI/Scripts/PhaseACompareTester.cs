
using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;

public class PhaseACompareTester : MonoBehaviour
{
    [Header("V1 (CŨ) - LSTM v3 + DummyContext")]
    public ModelAsset v1Model;
    public TextAsset v1Meta;
    public TextAsset v1Responses;

    [Header("V2 (MỚI) - v4 + EntityExtractor + SmartContext")]
    public ModelAsset v2Model;
    public TextAsset v2Meta;
    public TextAsset v2Responses;
    public TextAsset slotVocabJson;

    [Header("Inference")]
    public BackendType backend = BackendType.CPU;
    public float minConfidence = 0.40f;

    private NPCDialogueBrain _v1Brain;
    private NPCDialogueBrain _v2Brain;
    private SmartRuntimeContext _v2Context;
    private EntityExtractor _extractor;

    private struct Entry
    {
        public bool isUser;
        public string text;
        public string subInfo;       // intent + conf
        public string subInfoExtra;  // slot extractions for V2
    }

    private readonly List<Entry> _v1History = new();
    private readonly List<Entry> _v2History = new();
    private string _input = "";
    private Vector2 _scrollV1 = Vector2.zero;
    private Vector2 _scrollV2 = Vector2.zero;
    private bool _ready = false;
    private bool _focusOnNextRepaint = true;
    private int _v1Pass = 0, _v2Pass = 0, _samplesTotal = 0;

    // Same sanity samples used by single-brain tester so scores are comparable.
    private static readonly (string text, string expected)[] Samples = new[]
    {
        ("Mấy giờ thì ăn cơm",       "HOI_GIO_AN"),
        ("Phòng học ở đâu vậy",      "HOI_VI_TRI"),
        ("Em xin phép về quê",       "XIN_PHEP"),
        ("Wifi yếu quá",              "OUT_OF_SCOPE"),
        ("Súng AK47 dùng thế nào",   "HOI_KIEN_THUC"),
        ("Khu A ở đâu",              "HOI_VI_TRI"),       // user pain-point case
        ("đói quá",                   "HOI_GIO_AN"),       // ellipsis
        ("Schedule mai sao rồi",     "HOI_LICH"),         // code-mix
    };

    void Awake()
    {
        // Build V1 brain on a child GameObject so it doesn't conflict with this component.
        var v1Go = new GameObject("V1_Brain_Old");
        v1Go.transform.SetParent(transform, false);
        _v1Brain = v1Go.AddComponent<NPCDialogueBrain>();
        _v1Brain.modelAsset = v1Model;
        _v1Brain.metaJson = v1Meta;
        _v1Brain.responsesJson = v1Responses;
        _v1Brain.backend = backend;
        _v1Brain.minConfidence = minConfidence;
        _v1Brain.context = new DummyContext();

        var v2Go = new GameObject("V2_Brain_New");
        v2Go.transform.SetParent(transform, false);
        _v2Brain = v2Go.AddComponent<NPCDialogueBrain>();
        _v2Brain.modelAsset = v2Model != null ? v2Model : v1Model;
        _v2Brain.metaJson = v2Meta != null ? v2Meta : v1Meta;
        _v2Brain.responsesJson = v2Responses != null ? v2Responses : v1Responses;
        _v2Brain.backend = backend;
        _v2Brain.minConfidence = minConfidence;
        _v2Context = new SmartRuntimeContext(new DummyContext());
        _v2Brain.context = _v2Context;

        if (slotVocabJson != null)
        {
            _extractor = new EntityExtractor(slotVocabJson.text);
        }
        else
        {
            Debug.LogWarning("[Compare] slotVocabJson missing — V2 sẽ không có slot extraction");
        }
    }

    void Start()
    {
        Debug.Log("== Phase A — A/B Compare V1 (Old) vs V2 (New) ==");
        if (_v1Brain == null || _v2Brain == null) { Debug.LogError("[Compare] brains not initialized"); return; }

        _samplesTotal = Samples.Length;
        Debug.Log("Sanity samples -");
        foreach (var (text, expected) in Samples)
        {
            // V1
            var (v1Intent, v1Conf) = _v1Brain.Classify(text);
            string v1Reply = _v1Brain.Respond(text);
            bool v1Ok = v1Intent == expected;
            if (v1Ok) _v1Pass++;
            _v1History.Add(new Entry { isUser = true, text = text });
            _v1History.Add(new Entry
            {
                isUser = false, text = v1Reply,
                subInfo = $"{v1Intent} ({v1Conf*100:F0}%) — expect {expected} {(v1Ok ? "ok" : "fail")}",
            });

            // V2
            var slots = _extractor != null ? _extractor.Extract(text) : new Dictionary<string,string>();
            _v2Context.SetExtractedSlots(slots);
            var (v2Intent, v2Conf) = _v2Brain.Classify(text);
            string v2Reply = _v2Brain.Respond(text);
            bool v2Ok = v2Intent == expected;
            if (v2Ok) _v2Pass++;
            string slotInfo = slots.Count > 0 ? "slots: " + string.Join(", ", FormatSlots(slots)) : "(no slots)";
            _v2History.Add(new Entry { isUser = true, text = text });
            _v2History.Add(new Entry
            {
                isUser = false, text = v2Reply,
                subInfo = $"{v2Intent} ({v2Conf*100:F0}%) — expect {expected} {(v2Ok ? "ok" : "fail")}",
                subInfoExtra = slotInfo,
            });

            Debug.Log($"| \"{text,-30}\" V1={v1Intent} ({v1Conf*100:F0}%) {(v1Ok?"ok":"fail")} | V2={v2Intent} ({v2Conf*100:F0}%) {(v2Ok?"ok":"fail")} slots={slotInfo}");
        }
        Debug.Log($"| V1 score {_v1Pass}/{_samplesTotal}  |  V2 score {_v2Pass}/{_samplesTotal}");
        Debug.Log("");
        _ready = true;
    }

    static IEnumerable<string> FormatSlots(Dictionary<string,string> slots)
    {
        foreach (var kv in slots) yield return $"{kv.Key}=\"{kv.Value}\"";
    }

    void OnGUI()
    {
        if (!_ready) return;

        float w = Screen.width;
        float h = Screen.height;
        float padding = 16f;
        float headerH = 90f;
        float inputRowH = 70f;

        // Background
        GUI.color = new Color(0.07f, 0.10f, 0.13f, 1f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 26, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        var subStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 14,
            normal = { textColor = new Color(0.75f, 0.9f, 0.75f) }
        };

        GUI.Label(new Rect(padding, padding, w - 2 * padding, 32), "Phase A — A/B Compare: V1 (Old) vs V2 (New)", titleStyle);
        GUI.Label(new Rect(padding, padding + 36, w - 2 * padding, 24),
                  $"Sanity 8 câu — V1: {_v1Pass}/{_samplesTotal}  |  V2: {_v2Pass}/{_samplesTotal}    Gõ tiếng Việt rồi Enter để test thêm", subStyle);

        // Two columns
        float colW = (w - 3 * padding) / 2f;
        float colY = padding + headerH;
        float colH = h - headerH - inputRowH - 2 * padding;

        DrawColumn(new Rect(padding, colY, colW, colH), "V1 — AI CŨ (LSTM v3, no slot)", new Color(0.55f, 0.20f, 0.20f, 0.95f), _v1History, ref _scrollV1);
        DrawColumn(new Rect(padding * 2 + colW, colY, colW, colH), "V2 — AI MỚI (v4 + EntityExtractor)", new Color(0.20f, 0.45f, 0.20f, 0.95f), _v2History, ref _scrollV2);

        // Input row
        float inputY = h - inputRowH - padding;
        var inputStyle = new GUIStyle(GUI.skin.textField) {
            fontSize = 18, padding = new RectOffset(10, 10, 8, 8)
        };
        var sendStyle = new GUIStyle(GUI.skin.button) {
            fontSize = 18, fontStyle = FontStyle.Bold
        };
        float btnW = 140f;
        var inputRect = new Rect(padding, inputY, w - 3 * padding - btnW, inputRowH - 8);
        var sendRect = new Rect(w - padding - btnW, inputY, btnW, inputRowH - 8);

        GUI.SetNextControlName("CmpInput");
        _input = GUI.TextField(inputRect, _input, inputStyle);

        bool sendClicked = GUI.Button(sendRect, "Gửi cả 2", sendStyle);
        bool enterPressed = Event.current.type == EventType.KeyDown
                            && (Event.current.keyCode == KeyCode.Return
                                || Event.current.keyCode == KeyCode.KeypadEnter);

        if ((sendClicked || enterPressed) && !string.IsNullOrWhiteSpace(_input))
        {
            Submit(_input.Trim());
            _input = "";
            _focusOnNextRepaint = true;
            if (enterPressed) Event.current.Use();
        }

        if (_focusOnNextRepaint && Event.current.type == EventType.Repaint)
        {
            GUI.FocusControl("CmpInput");
            _focusOnNextRepaint = false;
        }
    }

    void DrawColumn(Rect rect, string title, Color userColor, List<Entry> hist, ref Vector2 scroll)
    {
        // Background
        GUI.color = new Color(0.10f, 0.13f, 0.18f, 1f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        var titleStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 18, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }, padding = new RectOffset(10, 10, 8, 8),
        };
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 36), title, titleStyle);

        var historyRect = new Rect(rect.x + 4, rect.y + 38, rect.width - 8, rect.height - 42);

        var userStyle = new GUIStyle(GUI.skin.box) {
            fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6),
            wordWrap = true, normal = { textColor = Color.white, background = MakeTex(userColor) }
        };
        var npcStyle = new GUIStyle(GUI.skin.box) {
            fontSize = 15, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 6, 6),
            wordWrap = true, normal = { textColor = Color.white, background = MakeTex(new Color(0.22f, 0.22f, 0.26f, 0.95f)) }
        };
        var subStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 11, normal = { textColor = new Color(0.65f, 0.85f, 0.65f) },
            padding = new RectOffset(10, 0, 0, 2),
        };
        var slotStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 11, normal = { textColor = new Color(0.95f, 0.85f, 0.5f) },
            padding = new RectOffset(10, 0, 0, 4),
        };

        float bubbleMaxW = historyRect.width - 30;
        float total = ComputeTotalHeight(hist, bubbleMaxW, userStyle, npcStyle);
        var contentRect = new Rect(0, 0, historyRect.width - 20, total);
        scroll = GUI.BeginScrollView(historyRect, scroll, contentRect);

        float y = 6;
        foreach (var e in hist)
        {
            var style = e.isUser ? userStyle : npcStyle;
            float bw = Mathf.Min(bubbleMaxW, style.CalcSize(new GUIContent(e.text)).x + 24);
            bw = Mathf.Max(bw, 80);
            float bh = style.CalcHeight(new GUIContent(e.text), bw);
            float x = e.isUser ? historyRect.width - bw - 30 : 8;
            GUI.Box(new Rect(x, y, bw, bh), e.text, style);
            y += bh;
            if (!e.isUser && !string.IsNullOrEmpty(e.subInfo))
            {
                GUI.Label(new Rect(x, y, bw, 16), e.subInfo, subStyle);
                y += 16;
            }
            if (!e.isUser && !string.IsNullOrEmpty(e.subInfoExtra))
            {
                GUI.Label(new Rect(x, y, bw, 16), e.subInfoExtra, slotStyle);
                y += 16;
            }
            y += 6;
        }

        GUI.EndScrollView();
    }

    float ComputeTotalHeight(List<Entry> hist, float bubbleMaxW, GUIStyle u, GUIStyle n)
    {
        float total = 12;
        foreach (var e in hist)
        {
            var style = e.isUser ? u : n;
            float bw = Mathf.Min(bubbleMaxW, style.CalcSize(new GUIContent(e.text)).x + 24);
            bw = Mathf.Max(bw, 80);
            total += style.CalcHeight(new GUIContent(e.text), bw);
            if (!e.isUser && !string.IsNullOrEmpty(e.subInfo)) total += 16;
            if (!e.isUser && !string.IsNullOrEmpty(e.subInfoExtra)) total += 16;
            total += 6;
        }
        return total;
    }

    void Submit(string text)
    {
        // V1
        var (v1Intent, v1Conf) = _v1Brain.Classify(text);
        string v1Reply = _v1Brain.Respond(text);
        _v1History.Add(new Entry { isUser = true, text = text });
        _v1History.Add(new Entry { isUser = false, text = v1Reply, subInfo = $"{v1Intent} ({v1Conf*100:F0}%)" });

        // V2 - extract slots first, set context, then respond
        var slots = _extractor != null ? _extractor.Extract(text) : new Dictionary<string,string>();
        _v2Context.SetExtractedSlots(slots);
        var (v2Intent, v2Conf) = _v2Brain.Classify(text);
        string v2Reply = _v2Brain.Respond(text);
        string slotInfo = slots.Count > 0 ? "slots: " + string.Join(", ", FormatSlots(slots)) : "(no slots)";
        _v2History.Add(new Entry { isUser = true, text = text });
        _v2History.Add(new Entry { isUser = false, text = v2Reply, subInfo = $"{v2Intent} ({v2Conf*100:F0}%)", subInfoExtra = slotInfo });

        _scrollV1.y = float.MaxValue;
        _scrollV2.y = float.MaxValue;
        Debug.Log($"[Compare] \"{text}\"  V1->{v1Intent} ({v1Conf*100:F0}%) | V2->{v2Intent} ({v2Conf*100:F0}%) {slotInfo}");
    }

    private Dictionary<Color, Texture2D> _texCache = new();
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

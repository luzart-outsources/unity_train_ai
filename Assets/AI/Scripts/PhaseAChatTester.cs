// PhaseAChatTester.cs
//
// Test Phase A interactive: tự classify 5 sample sentences khi Start, sau đó
// mở UI chat (IMGUI) để user gõ tay câu bất kỳ và xem kết quả.
//
// Không cần TextMeshPro — dùng OnGUI() native.

using System.Collections.Generic;
using UnityEngine;
using Unity.InferenceEngine;

public class PhaseAChatTester : MonoBehaviour
{
    [Header("Phase A assets")]
    public ModelAsset intentModel;
    public TextAsset intentMeta;
    public TextAsset responsesJson;

    private NPCDialogueBrain _brain;
    private string _inputText = "";
    private List<string> _history = new List<string>();
    private Vector2 _scroll = Vector2.zero;
    private bool _ready = false;

    // Sample sentences for auto-test (sanity check)
    private static readonly (string text, string expected)[] SamplesA = new[]
    {
        ("Mấy giờ thì ăn cơm",       "HOI_GIO_AN"),
        ("Phòng học ở đâu vậy",      "HOI_VI_TRI"),
        ("Em xin phép về quê",       "XIN_PHEP"),
        ("Wifi yếu quá",              "OUT_OF_SCOPE"),
        ("Súng AK47 dùng thế nào",   "HOI_KIEN_THUC"),
    };

    void Start()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log(" PHASE A — Intent Classifier Chat Test");
        Debug.Log("══════════════════════════════════════════════");

        if (intentModel == null || intentMeta == null || responsesJson == null)
        {
            Debug.LogError("[PhaseA] Assets missing — assign trong Inspector");
            enabled = false;
            return;
        }

        // Tạo Commander GameObject với NPCDialogueBrain
        // SetActive(false) trước AddComponent để Awake không chạy với fields null
        var commander = new GameObject("Commander");
        commander.SetActive(false);
        _brain = commander.AddComponent<NPCDialogueBrain>();
        _brain.modelAsset = intentModel;
        _brain.metaJson = intentMeta;
        _brain.responsesJson = responsesJson;
        _brain.backend = BackendType.CPU;
        _brain.minConfidence = 0.40f;
        commander.SetActive(true);

        // Auto-test sanity check
        Debug.Log("┌─ Auto sanity test (5 sample) ─");
        int correct = 0;
        foreach (var (text, expected) in SamplesA)
        {
            var (intent, conf) = _brain.Classify(text);
            bool ok = intent == expected;
            if (ok) correct++;
            string mark = ok ? "✓" : "✗";
            string line = $"{mark} \"{text}\" → {intent} ({conf*100:F1}%, expect {expected})";
            Debug.Log("│ " + line);
            _history.Add(line);
        }
        float acc = (float)correct / SamplesA.Length;
        string scoreLine = $"Score: {correct}/{SamplesA.Length} = {acc*100:F0}%";
        Debug.Log("│ " + scoreLine);
        _history.Add("");
        _history.Add(scoreLine);
        _history.Add(acc >= 0.8f ? "✅ Sanity PASS" : "⚠️ Sanity FAIL");
        _history.Add("");
        _history.Add("→ Gõ câu bên dưới để test thêm:");
        _history.Add("");

        Debug.Log($"│ → Score {correct}/{SamplesA.Length} = {acc*100:F0}%");
        Debug.Log("└────────────────────────────────────────────");
        Debug.Log("");
        Debug.Log("► Chat UI ready — gõ trong Game window để test thêm.");

        _ready = true;
    }

    // ─────────────────────────────────────────────────────────────────────
    // IMGUI chat panel — không cần Canvas/TMP setup
    // ─────────────────────────────────────────────────────────────────────
    void OnGUI()
    {
        if (!_ready) return;

        var skin = GUI.skin;
        // Larger font for readability
        var oldSize = skin.label.fontSize;
        var oldButtonSize = skin.button.fontSize;
        var oldFieldSize = skin.textField.fontSize;
        skin.label.fontSize = 16;
        skin.button.fontSize = 16;
        skin.textField.fontSize = 16;

        // Layout: full screen panel
        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40),
                            GUI.skin.box);

        GUILayout.Label("<b>Phase A — NPC Chat Test (gõ tiếng Việt)</b>",
                        new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20 });
        GUILayout.Space(10);

        // History scroll view
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Screen.height - 200));
        foreach (var line in _history)
        {
            GUILayout.Label(line);
        }
        GUILayout.EndScrollView();

        GUILayout.Space(10);

        // Input row
        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("ChatInput");
        _inputText = GUILayout.TextField(_inputText, GUILayout.Height(35));
        bool clicked = GUILayout.Button("Gửi", GUILayout.Width(80), GUILayout.Height(35));
        GUILayout.EndHorizontal();

        // Submit on Enter or click
        bool enterPressed = Event.current.type == EventType.KeyDown
                            && (Event.current.keyCode == KeyCode.Return
                                || Event.current.keyCode == KeyCode.KeypadEnter);
        if ((clicked || enterPressed) && !string.IsNullOrWhiteSpace(_inputText))
        {
            Submit(_inputText.Trim());
            _inputText = "";
            GUI.FocusControl("ChatInput");
            if (enterPressed) Event.current.Use();
        }

        GUILayout.EndArea();

        // Restore old font sizes
        skin.label.fontSize = oldSize;
        skin.button.fontSize = oldButtonSize;
        skin.textField.fontSize = oldFieldSize;
    }

    void Submit(string text)
    {
        var (intent, conf) = _brain.Classify(text);
        string reply = _brain.Respond(text);
        _history.Add($"<b>You:</b> {text}");
        _history.Add($"<b>Intent:</b> {intent} ({conf*100:F1}%)");
        _history.Add($"<b>NPC:</b> {reply}");
        _history.Add("");
        _scroll.y = float.MaxValue;  // auto-scroll to bottom

        Debug.Log($"[PhaseA] \"{text}\" → {intent} ({conf*100:F1}%) | {reply}");
    }
}

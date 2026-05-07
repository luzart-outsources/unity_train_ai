// PhaseAChatTester.cs
//
// UI chat IMGUI cho Phase A. KHÔNG spawn Commander — expect được gắn cùng
// GameObject với NPCDialogueBrain (đã configured bởi editor builder).
//
// Workflow:
//   - Awake: tìm NPCDialogueBrain trên cùng GameObject
//   - Start: classify 5 sample sentences (sanity check) → log Console
//   - OnGUI: chat UI cho user gõ tay câu bất kỳ

using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NPCDialogueBrain))]
public class PhaseAChatTester : MonoBehaviour
{
    private NPCDialogueBrain _brain;
    private string _inputText = "";
    private List<string> _history = new List<string>();
    private Vector2 _scroll = Vector2.zero;
    private bool _ready = false;

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
    }

    void Start()
    {
        Debug.Log("══════════════════════════════════════════════");
        Debug.Log(" PHASE A — Intent Classifier Chat Test");
        Debug.Log("══════════════════════════════════════════════");

        if (_brain == null || !_brain.enabled)
        {
            Debug.LogError("[PhaseA] NPCDialogueBrain disabled hoặc missing — check Inspector của Commander");
            return;
        }

        Debug.Log("┌─ Auto sanity test (5 sample) ─");
        int correct = 0;
        foreach (var (text, expected) in Samples)
        {
            var (intent, conf) = _brain.Classify(text);
            bool ok = intent == expected;
            if (ok) correct++;
            string mark = ok ? "✓" : "✗";
            string line = $"{mark} \"{text}\" → {intent} ({conf*100:F1}%, expect {expected})";
            Debug.Log("│ " + line);
            _history.Add(line);
        }
        float acc = (float)correct / Samples.Length;
        _history.Add("");
        _history.Add($"Score: {correct}/{Samples.Length} = {acc*100:F0}%");
        _history.Add(acc >= 0.8f ? "✅ Sanity PASS" : "⚠️ Sanity FAIL");
        _history.Add("");
        _history.Add("→ Gõ câu bên dưới để test thêm:");
        _history.Add("");

        Debug.Log($"│ → Score {correct}/{Samples.Length} = {acc*100:F0}%");
        Debug.Log("└────────────────────────────────────────────");
        Debug.Log("► Chat UI ready — gõ trong Game window để test thêm.");

        _ready = true;
    }

    void OnGUI()
    {
        if (!_ready) return;

        var skin = GUI.skin;
        var oldLabel = skin.label.fontSize;
        var oldButton = skin.button.fontSize;
        var oldField = skin.textField.fontSize;
        skin.label.fontSize = 16;
        skin.button.fontSize = 16;
        skin.textField.fontSize = 16;

        GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40), GUI.skin.box);

        GUILayout.Label("<b>Phase A — NPC Chat Test (gõ tiếng Việt)</b>",
                        new GUIStyle(GUI.skin.label) { richText = true, fontSize = 20 });
        GUILayout.Space(10);

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(Screen.height - 200));
        var lblStyle = new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15, wordWrap = true };
        foreach (var line in _history) GUILayout.Label(line, lblStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUI.SetNextControlName("ChatInput");
        _inputText = GUILayout.TextField(_inputText, GUILayout.Height(35));
        bool clicked = GUILayout.Button("Gửi", GUILayout.Width(80), GUILayout.Height(35));
        GUILayout.EndHorizontal();

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

        skin.label.fontSize = oldLabel;
        skin.button.fontSize = oldButton;
        skin.textField.fontSize = oldField;
    }

    void Submit(string text)
    {
        var (intent, conf) = _brain.Classify(text);
        string reply = _brain.Respond(text);
        _history.Add($"<b>You:</b> {text}");
        _history.Add($"<b>Intent:</b> {intent} ({conf*100:F1}%)");
        _history.Add($"<b>NPC:</b> {reply}");
        _history.Add("");
        _scroll.y = float.MaxValue;
        Debug.Log($"[PhaseA] \"{text}\" → {intent} ({conf*100:F1}%) | {reply}");
    }
}

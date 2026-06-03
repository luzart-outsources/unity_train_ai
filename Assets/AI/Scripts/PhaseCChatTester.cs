// Phase C — Minimal OnGUI chat tester for HybridChatClient.
//
// Drop this on any GameObject (no Canvas required). Hit Play and type
// Vietnamese questions in the input box. Each turn shows:
//   - User question
//   - Bot reply
//   - Routing label (template / groq_rag / fallback) + confidence score
//
// The Phase C pipeline is HTTP — make sure chat_server.py is running:
//     AI_Training/phase_c_chat/scripts/chat_server.py
//
// This is intentionally a lightweight tester that doesn't depend on TMP
// or UI Toolkit, so it works in any scene.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrainAI.AI
{
    public class PhaseCChatTester : MonoBehaviour
    {
        [Header("Wire to a HybridChatClient component in the scene")]
        public HybridChatClient client;

        [Header("UI")]
        [Tooltip("Pre-fill samples shown as quick buttons")]
        public string[] sampleQuestions = new[]
        {
            "Sân vận động ở đâu",
            "Mấy giờ ăn cơm",
            "Hôm nay học môn gì",
            "Đại đội trưởng là ai",
            "Em chào thủ trưởng",
            "khu A1 ở đâu",     // original bug
            "ddt la ai v",      // slang
            "Where is canteen", // code-mix
        };

        private struct Entry
        {
            public bool   isUser;
            public string text;
            public string meta;
            public Color  metaColor;
        }
        private readonly List<Entry> _history = new();
        private string _input = "";
        private Vector2 _scroll;
        private bool _busy;
        private string _status = "Ready. Server: http://127.0.0.1:8765";

        private void Awake()
        {
            if (client == null) client = FindObjectOfType<HybridChatClient>();
            if (client == null)
            {
                Debug.LogWarning("[PhaseCChatTester] No HybridChatClient found. Add one to the scene.");
            }
        }

        private void OnGUI()
        {
            const int W = 720, H = 540;
            var rect = new Rect(20, 20, W, H);
            GUI.Box(rect, "Phase C — NPC Chat (HTTP + Embedding Retrieval + Groq RAG)");

            var inner = new Rect(rect.x + 10, rect.y + 28, rect.width - 20, rect.height - 38);
            GUILayout.BeginArea(inner);

            // History.
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(380));
            foreach (var e in _history)
            {
                string label = e.isUser ? "Đồng chí" : "Đại đội trưởng";
                Color c = GUI.color;
                GUI.color = e.isUser ? new Color(0.8f, 0.9f, 1f) : new Color(0.9f, 1f, 0.85f);
                GUILayout.Label($"<b>{label}:</b> {e.text}", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
                GUI.color = c;
                if (!string.IsNullOrEmpty(e.meta))
                {
                    GUI.color = e.metaColor;
                    GUILayout.Label($"   <size=10>{e.meta}</size>", new GUIStyle(GUI.skin.label) { richText = true });
                    GUI.color = c;
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(4);

            // Sample buttons.
            GUILayout.BeginHorizontal();
            foreach (var s in sampleQuestions)
            {
                if (GUILayout.Button(s, GUILayout.Height(22)))
                {
                    _input = s;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4);

            // Input row.
            GUILayout.BeginHorizontal();
            GUI.enabled = !_busy;
            _input = GUILayout.TextField(_input ?? "", GUILayout.MinWidth(500));
            if (GUILayout.Button("Send", GUILayout.Width(80)) ||
                (Event.current.isKey && Event.current.keyCode == KeyCode.Return && !_busy))
            {
                Submit();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(_input)) return;
            if (client == null)
            {
                _status = "No client wired.";
                return;
            }

            string q = _input.Trim();
            _input = "";
            _history.Add(new Entry { isUser = true, text = q });
            _busy = true;
            _status = "Querying server...";
            StartCoroutine(client.Ask(q, OnReply, OnError));
        }

        private void OnReply(HybridChatClient.ChatReply r)
        {
            _busy = false;
            if (r == null)
            {
                _history.Add(new Entry
                {
                    isUser = false, text = "[empty reply]",
                    meta = "no payload", metaColor = Color.red,
                });
                _status = "Empty reply.";
                return;
            }
            Color metaColor = r.route switch
            {
                "template" => new Color(0.4f, 0.8f, 0.4f),
                "groq_rag" => new Color(0.4f, 0.6f, 1f),
                "fallback" => new Color(1f, 0.6f, 0.2f),
                _          => Color.gray,
            };
            _history.Add(new Entry
            {
                isUser = false,
                text   = r.answer,
                meta   = $"route={r.route} score={r.score:F2} intent={r.topIntent} entity={r.topEntity} latency={r.latencyMs:F0}ms",
                metaColor = metaColor,
            });
            _scroll.y = float.MaxValue;
            _status = $"Done in {r.latencyMs:F0}ms";
        }

        private void OnError(string err)
        {
            _busy = false;
            _history.Add(new Entry
            {
                isUser = false,
                text   = "Server không phản hồi. Hãy chạy chat_server.py.",
                meta   = err,
                metaColor = Color.red,
            });
            _status = err;
        }
    }
}

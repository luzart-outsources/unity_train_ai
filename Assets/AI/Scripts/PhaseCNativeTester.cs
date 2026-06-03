// Phase C Native — OnGUI tester for the offline ONNX EmbeddingChatBrain.
//
// Identical UI shape to PhaseCChatTester (which talks HTTP to the
// Python server) but here every turn is fully offline:
//   user input -> EmbeddingChatBrain.Ask() -> reply
// No internet, no Python.

using System.Collections.Generic;
using UnityEngine;

namespace TrainAI.AI
{
    public class PhaseCNativeTester : MonoBehaviour
    {
        [Header("Wire to an EmbeddingChatBrain")]
        public EmbeddingChatBrain brain;

        [Tooltip("Optional GameStateContext; uses fallback values if null")]
        public MonoBehaviour gameStateContextComponent;

        [Header("UI")]
        public string[] sampleQuestions = new[]
        {
            "Sân vận động ở đâu",
            "Mấy giờ ăn cơm",
            "Hôm nay học môn gì",
            "Đại đội trưởng là ai",
            "Em chào thủ trưởng",
            "khu A1 ở đâu",
            "ddt la ai v",
            "Where is canteen",
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
        private string _status = "Ready (offline ONNX retrieval).";

        private IGameStateProvider Provider =>
            gameStateContextComponent as IGameStateProvider;

        private void OnGUI()
        {
            const int W = 720, H = 540;
            var rect = new Rect(20, 20, W, H);
            GUI.Box(rect, "Phase C Native — ONNX Embedding Retrieval (offline)");

            var inner = new Rect(rect.x + 10, rect.y + 28, rect.width - 20, rect.height - 38);
            GUILayout.BeginArea(inner);

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
            GUILayout.BeginHorizontal();
            foreach (var s in sampleQuestions)
            {
                if (GUILayout.Button(s, GUILayout.Height(22))) _input = s;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(4);

            GUILayout.BeginHorizontal();
            _input = GUILayout.TextField(_input ?? "", GUILayout.MinWidth(500));
            if (GUILayout.Button("Send", GUILayout.Width(80)) ||
                (Event.current.isKey && Event.current.keyCode == KeyCode.Return))
            {
                Submit();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(_status);
            GUILayout.EndArea();
        }

        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(_input)) return;
            if (brain == null)
            {
                _status = "No brain wired.";
                return;
            }

            string q = _input.Trim();
            _input = "";
            _history.Add(new Entry { isUser = true, text = q });

            var t0 = Time.realtimeSinceStartup;
            var r  = brain.Ask(q, Provider);
            var dt = (Time.realtimeSinceStartup - t0) * 1000f;

            Color metaColor = r.route switch
            {
                "template"        => new Color(0.4f, 0.8f, 0.4f),
                "low_confidence"  => new Color(1f, 0.85f, 0.4f),
                "fallback"        => new Color(1f, 0.6f, 0.2f),
                _                 => Color.gray,
            };
            _history.Add(new Entry
            {
                isUser = false,
                text   = r.answer,
                meta   = $"route={r.route} score={r.score:F2} intent={r.topIntent} entity={r.topEntity} latency={dt:F0}ms",
                metaColor = metaColor,
            });
            _scroll.y = float.MaxValue;
            _status = $"Done in {dt:F0}ms";
        }
    }
}

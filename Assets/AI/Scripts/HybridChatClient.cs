// Phase C — HTTP client for the local Python chat server.
//
// Talks to chat_server.py at 127.0.0.1:8765 over POST /chat.
// Returns a fluent Đại đội trưởng reply that uses retrieval over a 11K
// Q&A bank + optional Groq LLM RAG fallback for low-confidence queries.
//
// Usage (MonoBehaviour):
//   - Add this to a GameObject in the scene.
//   - Assign a GameStateContext provider (or leave null for blank state).
//   - Call StartCoroutine(Ask(text, onReply, onError));
//
// The server URL and timeout are exposed in the Inspector.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace TrainAI.AI
{
    /// <summary>
    /// Hybrid embedding-retrieval + LLM-fallback chat client.
    /// Communicates with the local FastAPI server in AI_Training/phase_c_chat/scripts/chat_server.py.
    /// </summary>
    public class HybridChatClient : MonoBehaviour
    {
        [Header("Server")]
        [Tooltip("Base URL of the local Python chat server")]
        public string serverUrl = "http://127.0.0.1:8765";

        [Tooltip("HTTP timeout in seconds")]
        public int timeoutSec = 20;

        [Header("Game state provider (optional)")]
        [Tooltip("Component implementing IGameStateProvider — auto-discovers GameStateContext if null")]
        public MonoBehaviour gameStateProviderComponent;

        [Header("Settings")]
        [Tooltip("Allow Groq fallback for low-confidence queries")]
        public bool useGroqFallback = true;

        [Tooltip("How many candidate Q&A pairs to retrieve")]
        [Range(1, 10)] public int topK = 5;

        public IGameStateProvider Provider
        {
            get
            {
                if (gameStateProviderComponent is IGameStateProvider p) return p;
                // Scan scene MonoBehaviours for the first one implementing the interface.
                foreach (var mb in FindObjectsOfType<MonoBehaviour>())
                {
                    if (mb is IGameStateProvider gp) return gp;
                }
                return null;
            }
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>
        /// Send a query to the chat server. Calls onReply with the (answer, meta)
        /// tuple on success, or onError with a human-readable error string.
        /// </summary>
        public IEnumerator Ask(
            string query,
            Action<ChatReply> onReply,
            Action<string>    onError)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                onError?.Invoke("Empty query");
                yield break;
            }

            // Build request body.
            var req = new ChatRequest
            {
                query     = query,
                useGroq   = useGroqFallback,
                k         = topK,
                gameState = BuildGameStateDict(),
            };
            string bodyJson = JsonUtility.ToJson(req);

            string url = serverUrl.TrimEnd('/') + "/chat";
            using (var www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(bodyJson));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                www.timeout = timeoutSec;

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"[HybridChat] {www.result}: {www.error} | {www.downloadHandler?.text}");
                    yield break;
                }

                ChatReply reply;
                try
                {
                    reply = JsonUtility.FromJson<ChatReply>(www.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"[HybridChat] JSON parse failed: {ex.Message} | raw: {www.downloadHandler.text}");
                    yield break;
                }
                onReply?.Invoke(reply);
            }
        }

        /// <summary>
        /// Convenience: returns the answer text and discards extra info.
        /// </summary>
        public IEnumerator AskText(string query, Action<string> onAnswer, Action<string> onError)
        {
            yield return Ask(
                query,
                r => onAnswer?.Invoke(r != null ? r.answer : ""),
                onError);
        }

        // =====================================================================
        // Game state assembly
        // =====================================================================

        private ChatRequest.GameStateDict BuildGameStateDict()
        {
            var p = Provider;
            if (p == null) return new ChatRequest.GameStateDict();
            return new ChatRequest.GameStateDict
            {
                currentDay   = p.CurrentDay,
                currentTime  = p.CurrentTime,
                currentArea  = p.CurrentArea,
                playerName   = p.PlayerName,
                activeQuest  = p.ActiveQuest,
            };
        }

        // =====================================================================
        // DTOs (Unity JsonUtility-compatible — no Dictionaries)
        // =====================================================================

        [Serializable]
        public class ChatRequest
        {
            public string         query;
            public GameStateDict  gameState;
            public bool           useGroq = true;
            public int            k       = 5;

            [Serializable]
            public class GameStateDict
            {
                public int    currentDay;
                public string currentTime;
                public string currentArea;
                public string playerName;
                public string activeQuest;
            }
        }

        [Serializable]
        public class ChatReply
        {
            public string answer;
            public string route;        // "template" | "groq_rag" | "fallback"
            public float  score;        // top-1 cosine similarity
            public string topIntent;
            public string topEntity;
            public Hit[]  hits;
            public float  latencyMs;

            [Serializable]
            public class Hit
            {
                public float  score;
                public string id;
                public string question;
                public string answer;
                public string intent;
                public string entityId;
                public string entityType;
                public bool   needsGameState;
            }
        }
    }

    /// <summary>
    /// Interface implemented by GameStateContext (and any mock) so the chat
    /// client can pull live game state without taking a hard dependency.
    /// </summary>
    public interface IGameStateProvider
    {
        int    CurrentDay { get; }      // 1..30
        string CurrentTime { get; }     // "HH:mm"
        string CurrentArea { get; }     // Vietnamese display name
        string PlayerName  { get; }
        string ActiveQuest { get; }     // Q_dXX_NN id or display name
    }
}

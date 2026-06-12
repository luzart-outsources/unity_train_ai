// Phase C Native — Unity Inference Engine embedding retrieval brain.
//
// Loads the distilled student encoder (.onnx) + a pre-computed Q&A
// bank (.bytes) + metadata (.json) + vocab (.json) from Resources/AI/
// or from Inspector-assigned assets. Each user turn:
//   1. Tokenize Vietnamese input (Phase A pattern: whitespace + longest
//      multi-word match against vocab, lowercase).
//   2. Schedule the ONNX encoder -> 384-dim L2-normalized embedding.
//   3. Cosine similarity against bank -> argmax.
//   4. Resolve {__SCHEDULE_TODAY__} via IGameStateProvider if needed.
//
// Routing thresholds mirror chat_server.py:
//   score >= HIGH_TH  -> template answer
//   score >= LOW_TH   -> fallback (caller may still RAG with Groq)
//   else              -> graceful "I don't understand"
//
// This is the offline path. No Python server required.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.InferenceEngine;

namespace TrainAI.AI
{
    /// <summary>
    /// Sentis/InferenceEngine embedding retrieval brain — Unity-native
    /// equivalent of the Python chat_server.py /chat endpoint.
    /// </summary>
    public class EmbeddingChatBrain : MonoBehaviour
    {
        [Header("Assets")]
        [Tooltip("student_encoder.onnx (Assets/AI/Models/)")]
        public ModelAsset encoderModel;

        [Tooltip("vocab_phase_c.json (Assets/AI/Resources/) — token -> int dict")]
        public TextAsset vocabJson;

        [Tooltip("student_bank.bytes (Assets/AI/Resources/) — N x dim float32 vectors")]
        public TextAsset bankBytes;

        [Tooltip("student_bank.json (Assets/AI/Resources/) — array of {answer, intent, entityId, needsGameState}")]
        public TextAsset bankMetaJson;

        [Header("Inference")]
        public BackendType backend = BackendType.GPUCompute;
        public int maxLen = 40;
        [Tooltip("Above this score -> direct template answer (student over-confident: tune 0.88)")]
        [Range(0f, 1f)] public float highThreshold = 0.88f;
        [Tooltip("Below this score -> 'I don't understand' fallback")]
        [Range(0f, 1f)] public float lowThreshold  = 0.72f;
        [Tooltip("If true, low_confidence route returns the candidate answer (useful when a downstream RAG/LLM will refine it). Default false: low_confidence falls back to the safe 'I don't understand' answer offline.")]
        public bool surfaceLowConfidenceCandidate = false;

        [Header("Fallback")]
        [TextArea(2, 4)]
        public string fallbackAnswer =
            "Tôi chưa hiểu rõ câu hỏi. Đồng chí có thể hỏi về: lịch hôm nay, giờ ăn, " +
            "vị trí (sân vận động / nhà ăn / lớp học / ký túc xá / khu dọn vệ sinh / khu tự do), " +
            "ba môn học, hoặc thông tin Đại đội trưởng.";

        // =====================================================================
        // Internal state
        // =====================================================================

        private Model       _model;
        private Worker      _worker;
        private Dictionary<string, int> _vocab = new();
        private int _padId = 0;
        private int _unkId = 1;

        private float[][] _bank;            // bank vectors (cosine == dot since normalized)
        private int       _bankCount;
        private int       _bankDim;

        private BankMeta[] _meta;

        // For multi-word matching (Phase A pattern).
        private int _maxMultiWordLen = 1;

        // =====================================================================
        // Public result type
        // =====================================================================

        public struct ChatResult
        {
            public string answer;
            public string route;       // "template" | "fallback" | "low_confidence"
            public float  score;
            public string topIntent;
            public string topEntity;
            public int    topIndex;
        }

        // =====================================================================
        // Lifecycle
        // =====================================================================

        private void Awake()
        {
            if (!ValidateAssets()) { enabled = false; return; }
            BuildVocab(vocabJson.text);
            LoadBank(bankBytes.bytes);
            ParseMeta(bankMetaJson.text);
            _model  = ModelLoader.Load(encoderModel);
            _worker = new Worker(_model, backend);
            Debug.Log($"[EmbeddingChatBrain] ready  vocab={_vocab.Count} bank={_bankCount}x{_bankDim} backend={backend}");
        }

        private void OnDestroy() => _worker?.Dispose();

        private bool ValidateAssets()
        {
            bool ok = true;
            if (encoderModel == null) { Debug.LogError("EmbeddingChatBrain: missing encoderModel"); ok = false; }
            if (vocabJson    == null) { Debug.LogError("EmbeddingChatBrain: missing vocabJson"); ok = false; }
            if (bankBytes    == null) { Debug.LogError("EmbeddingChatBrain: missing bankBytes"); ok = false; }
            if (bankMetaJson == null) { Debug.LogError("EmbeddingChatBrain: missing bankMetaJson"); ok = false; }
            return ok;
        }

        // =====================================================================
        // Public API
        // =====================================================================

        /// <summary>Run retrieval. Optionally resolve placeholders against game state.</summary>
        public ChatResult Ask(string text, IGameStateProvider gameState = null)
        {
            float[] q = Embed(text);
            int    best  = 0;
            float  bestS = -2f;
            for (int i = 0; i < _bankCount; i++)
            {
                float s = Dot(q, _bank[i]);
                if (s > bestS) { bestS = s; best = i; }
            }

            var meta = _meta[best];
            string answer;
            string route;
            if (bestS >= highThreshold)
            {
                answer = ResolvePlaceholders(meta.answer, gameState);
                route  = "template";
            }
            else if (bestS >= lowThreshold)
            {
                // Score is in the "ambiguous" zone — by default play it safe and
                // return the fallback answer rather than risk confidently saying
                // the wrong thing (e.g. "1+1=?" tokenizing to "1" + "1" matches
                // NPC entries that mention "đồng chí 01" / "ban 01" at ~0.86).
                // Set surfaceLowConfidenceCandidate=true if a downstream LLM
                // will refine the candidate via RAG.
                answer = surfaceLowConfidenceCandidate
                    ? ResolvePlaceholders(meta.answer, gameState)
                    : fallbackAnswer;
                route  = "low_confidence";
            }
            else
            {
                answer = fallbackAnswer;
                route  = "fallback";
            }

            return new ChatResult
            {
                answer   = answer,
                route    = route,
                score    = bestS,
                topIntent = meta.intent,
                topEntity = meta.entityId,
                topIndex  = best,
            };
        }

        // =====================================================================
        // Tokenization (Phase A pattern: lowercase + greedy multi-word)
        // =====================================================================

        private int[] Encode(string text)
        {
            text = text.ToLowerInvariant().Trim();
            // Replace non-Vietnamese-alnum with spaces.
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) sb.Append(c);
                else sb.Append(' ');
            }
            var words = sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var ids = new int[maxLen];
            for (int i = 0; i < maxLen; i++) ids[i] = _padId;

            int wi = 0;
            int outI = 0;
            while (wi < words.Length && outI < maxLen)
            {
                int matchedSpan = 0;
                int matchedId   = _unkId;
                int maxSpan     = Mathf.Min(_maxMultiWordLen, words.Length - wi);
                for (int span = maxSpan; span >= 1; span--)
                {
                    string cand = span == 1 ? words[wi] : string.Join(" ", words, wi, span);
                    if (_vocab.TryGetValue(cand, out int id))
                    {
                        matchedSpan = span;
                        matchedId   = id;
                        break;
                    }
                }
                if (matchedSpan == 0)
                {
                    ids[outI++] = _unkId;
                    wi += 1;
                }
                else
                {
                    ids[outI++] = matchedId;
                    wi += matchedSpan;
                }
            }
            return ids;
        }

        // =====================================================================
        // Inference
        // =====================================================================

        private float[] Embed(string text)
        {
            int[] ids = Encode(text);
            // Sentis uses int (32-bit) for index tensors; ONNX may use int64 but
            // the IE bridge converts. Use Tensor<int>.
            using var input = new Tensor<int>(new TensorShape(1, maxLen), ids);
            _worker.Schedule(input);
            var outT = _worker.PeekOutput("embedding") as Tensor<float>;
            return outT.DownloadToArray();
        }

        private static float Dot(float[] a, float[] b)
        {
            int n = Mathf.Min(a.Length, b.Length);
            float s = 0f;
            for (int i = 0; i < n; i++) s += a[i] * b[i];
            return s;
        }

        // =====================================================================
        // Vocab + bank parsing
        // =====================================================================

        [Serializable] private class VocabRoot { public Dictionary<string,int> dummy; }

        private void BuildVocab(string json)
        {
            // JsonUtility can't parse Dictionary<string,int>, so do a tiny manual parse:
            // expected format {"<pad>":0, "<unk>":1, "em":2, ...}
            _vocab.Clear();
            int n = json.Length;
            int i = 0;
            while (i < n && json[i] != '{') i++;
            i++;  // past '{'
            while (i < n)
            {
                while (i < n && (json[i] == ' ' || json[i] == ',' || json[i] == '\n' || json[i] == '\r' || json[i] == '\t')) i++;
                if (i >= n || json[i] == '}') break;
                if (json[i] != '"') { i++; continue; }
                int kStart = ++i;
                var keyBuf = new StringBuilder();
                while (i < n && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < n)
                    {
                        char nc = json[i + 1];
                        if (nc == '"' || nc == '\\') keyBuf.Append(nc);
                        else if (nc == 'n') keyBuf.Append('\n');
                        else if (nc == 't') keyBuf.Append('\t');
                        else keyBuf.Append(nc);
                        i += 2;
                    }
                    else { keyBuf.Append(json[i]); i++; }
                }
                i++;                            // past closing quote
                while (i < n && (json[i] == ':' || json[i] == ' ')) i++;
                int vStart = i;
                while (i < n && json[i] != ',' && json[i] != '}') i++;
                int.TryParse(json.Substring(vStart, i - vStart).Trim(), out int id);
                _vocab[keyBuf.ToString()] = id;
            }

            // Determine max multi-word span (whitespace count + 1).
            _maxMultiWordLen = 1;
            foreach (var k in _vocab.Keys)
            {
                int spaces = 0;
                foreach (var c in k) if (c == ' ') spaces++;
                if (spaces + 1 > _maxMultiWordLen) _maxMultiWordLen = spaces + 1;
            }
        }

        private void LoadBank(byte[] raw)
        {
            // Header: 16 bytes -> uint32 N, uint32 DIM, 8 bytes reserved
            if (raw.Length < 16) throw new InvalidDataException("bank too small");
            int N   = BitConverter.ToInt32(raw, 0);
            int DIM = BitConverter.ToInt32(raw, 4);
            long expected = 16L + (long)N * DIM * 4;
            if (raw.Length < expected)
                throw new InvalidDataException($"bank truncated: need {expected} bytes, got {raw.Length}");
            _bankCount = N;
            _bankDim   = DIM;
            _bank = new float[N][];
            int off = 16;
            for (int i = 0; i < N; i++)
            {
                var v = new float[DIM];
                Buffer.BlockCopy(raw, off, v, 0, DIM * 4);
                _bank[i] = v;
                off += DIM * 4;
            }
        }

        // =====================================================================
        // Bank metadata
        // =====================================================================

        [Serializable]
        public class BankMeta
        {
            public string id;
            public string answer;
            public string intent;
            public string entityId;
            public string entityType;
            public bool   needsGameState;
        }

        [Serializable] private class BankMetaArray { public BankMeta[] items; }

        private void ParseMeta(string json)
        {
            // Wrap into {"items":[ ... ]} so JsonUtility can parse it.
            string wrapped = "{\"items\":" + json + "}";
            var arr = JsonUtility.FromJson<BankMetaArray>(wrapped);
            _meta = arr?.items ?? new BankMeta[0];
            if (_meta.Length != _bankCount)
                Debug.LogWarning($"[EmbeddingChatBrain] bank length {_bankCount} != metadata length {_meta.Length}");
        }

        // =====================================================================
        // Placeholder resolution
        // =====================================================================

        private static string ResolvePlaceholders(string answer, IGameStateProvider gs)
        {
            if (string.IsNullOrEmpty(answer)) return answer;
            if (!answer.Contains("{__SCHEDULE_TODAY__}")) return answer;
            int day = gs?.CurrentDay ?? 1;
            return answer.Replace("{__SCHEDULE_TODAY__}", BuildScheduleSummary(day));
        }

        private static string BuildScheduleSummary(int day)
        {
            // Conservative summary; the Python server uses full game data,
            // but for offline path we keep it short.
            return $"Lịch ngày {day}: 05:00 thể dục, 06:00 vệ sinh, 07:00 sáng, " +
                   "07:30-11:30 học, 11:30 trưa, 14:00-18:00 học, 18:00 tự do, 18:30 ngủ.";
        }
    }
}

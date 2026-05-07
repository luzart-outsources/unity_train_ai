
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.InferenceEngine;

[Serializable]
public class IntentMeta
{
    public string arch;
    public int max_len;
    public int pad_id;
    public int unk_id;
    // Note: JsonUtility cannot deserialize Dictionary<string,int> directly.
    // We parse vocab + id2label manually from the JSON string.
}

public class NPCDialogueBrain : MonoBehaviour
{
    [Header("Model assets (drag in Inspector)")]
    [Tooltip("fasttext_intent.onnx imported as ModelAsset")]
    public ModelAsset modelAsset;

    [Tooltip("fasttext_intent_meta.json — text asset")]
    public TextAsset metaJson;

    [Tooltip("responses.json — text asset")]
    public TextAsset responsesJson;

    [Header("Inference")]
    public BackendType backend = BackendType.GPUCompute;
    public float minConfidence = 0.40f;
    public string fallbackIntent = "OUT_OF_SCOPE";

    // Parsed metadata
    private Dictionary<string, int> _vocab;
    private Dictionary<int, string> _id2label;
    private int _maxLen;
    private int _padId;
    private int _unkId;

    // Per-intent response templates
    private Dictionary<string, List<string>> _responses;

    // Runtime model
    private Model _model;
    private Worker _worker;

    public IRuntimeContext context = new DummyContext();

    void Awake()
    {
        if (modelAsset == null || metaJson == null || responsesJson == null)
        {
            Debug.LogError("NPCDialogueBrain: missing assets — assign in Inspector.");
            enabled = false;
            return;
        }

        ParseMeta(metaJson.text);
        ParseResponses(responsesJson.text);

        _model = ModelLoader.Load(modelAsset);
        _worker = new Worker(_model, backend);
        Debug.Log($"[NPCBrain] loaded — vocab={_vocab.Count} intents={_id2label.Count} max_len={_maxLen}");
    }

    void OnDestroy()
    {
        _worker?.Dispose();
    }

    public string Respond(string userText)
    {
        var (intent, conf) = Classify(userText);
        if (conf < minConfidence)
        {
            intent = fallbackIntent;
        }
        return PickReply(intent);
    }

    public (string intent, float confidence) Classify(string text)
    {
        int[] ids = Encode(text);
        // Tensor shape: [1, max_len] int64. Sentis 2.x uses TensorShape constructor.
        using var input = new Tensor<int>(new TensorShape(1, _maxLen), ids);
        _worker.Schedule(input);

        // Peek (does not dispose), then we copy to CPU.
        var logitsT = _worker.PeekOutput("logits") as Tensor<float>;
        var logits = logitsT.DownloadToArray();  // [num_classes]

        // Softmax + argmax
        float maxLogit = logits.Max();
        float sumExp = 0f;
        for (int i = 0; i < logits.Length; i++) sumExp += Mathf.Exp(logits[i] - maxLogit);

        int bestId = 0;
        float bestProb = 0f;
        for (int i = 0; i < logits.Length; i++)
        {
            float p = Mathf.Exp(logits[i] - maxLogit) / sumExp;
            if (p > bestProb) { bestProb = p; bestId = i; }
        }
        return (_id2label[bestId], bestProb);
    }

    private int _maxMultiWordLen = 1;  // max word count of any vocab key

    int[] Encode(string text)
    {
        text = text.ToLowerInvariant().Trim();
        // Strip punctuation, normalize whitespace
        var sb = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) sb.Append(c);
            else sb.Append(' ');
        }
        var words = sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        var ids = new int[_maxLen];
        for (int i = 0; i < _maxLen; i++) ids[i] = _padId;

        int wi = 0;
        int idIdx = 0;
        while (wi < words.Length && idIdx < _maxLen)
        {
            int matchedSpan = 0;
            int matchedId = _unkId;
            int maxSpan = Math.Min(_maxMultiWordLen, words.Length - wi);
            for (int span = maxSpan; span >= 1; span--)
            {
                string candidate = span == 1 ? words[wi] : string.Join(" ", words, wi, span);
                if (_vocab.TryGetValue(candidate, out int id))
                {
                    matchedSpan = span;
                    matchedId = id;
                    break;
                }
            }
            if (matchedSpan == 0)
            {
                // No match -> 1 word as UNK
                ids[idIdx++] = _unkId;
                wi += 1;
            }
            else
            {
                ids[idIdx++] = matchedId;
                wi += matchedSpan;
            }
        }
        return ids;
    }

    string PickReply(string intent)
    {
        if (!_responses.TryGetValue(intent, out var pool) || pool.Count == 0)
            return "...";

        string template = pool[UnityEngine.Random.Range(0, pool.Count)];
        return Substitute(template);
    }

    string Substitute(string s)
    {
        // Replace any {key} with context.Get(key) when present.
        int searchFrom = 0;
        var sb = new StringBuilder();
        while (searchFrom < s.Length)
        {
            int open = s.IndexOf('{', searchFrom);
            if (open < 0) { sb.Append(s, searchFrom, s.Length - searchFrom); break; }
            int close = s.IndexOf('}', open + 1);
            if (close < 0) { sb.Append(s, searchFrom, s.Length - searchFrom); break; }
            sb.Append(s, searchFrom, open - searchFrom);
            string key = s.Substring(open + 1, close - open - 1);
            sb.Append(context.Get(key) ?? $"[{key}]");
            searchFrom = close + 1;
        }
        return sb.ToString();
    }

    void ParseMeta(string json)
    {
        _vocab = new Dictionary<string, int>();
        _id2label = new Dictionary<int, string>();
        _maxLen = ExtractInt(json, "max_len");
        _padId = ExtractInt(json, "pad_id");
        _unkId = ExtractInt(json, "unk_id");

        // vocab block: "vocab": { "tok": id, ... }
        ParseStringIntDict(json, "vocab", _vocab);

        // Tinh max word count cua vocab key — can cho greedy multi-word match
        _maxMultiWordLen = 1;
        foreach (var key in _vocab.Keys)
        {
            int spaceCount = 0;
            for (int i = 0; i < key.Length; i++)
                if (key[i] == ' ') spaceCount++;
            if (spaceCount + 1 > _maxMultiWordLen)
                _maxMultiWordLen = spaceCount + 1;
        }

        // id2label block: "id2label": { "0": "BAO_CAO", ... }
        var tmp = new Dictionary<string, string>();
        ParseStringStringDict(json, "id2label", tmp);
        foreach (var kv in tmp)
            if (int.TryParse(kv.Key, out int id))
                _id2label[id] = kv.Value;
    }

    void ParseResponses(string json)
    {
        // Top-level: { "INTENT": ["resp1", "resp2", ...], ... }
        _responses = new Dictionary<string, List<string>>();
        // Skip "_README" if present
        var idx = 0;
        while (idx < json.Length)
        {
            int kStart = json.IndexOf('"', idx);
            if (kStart < 0) break;
            int kEnd = json.IndexOf('"', kStart + 1);
            if (kEnd < 0) break;
            string key = json.Substring(kStart + 1, kEnd - kStart - 1);
            int colon = json.IndexOf(':', kEnd);
            int arrStart = json.IndexOf('[', colon);
            int objStart = json.IndexOf('"', colon);  // for string values like _README
            // If string value (not array), skip
            if (arrStart < 0 || (objStart > 0 && objStart < arrStart))
            {
                int sEnd = json.IndexOf('"', objStart + 1);
                idx = sEnd + 1;
                continue;
            }
            int arrEnd = FindMatching(json, arrStart, '[', ']');
            string arrJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);
            var list = new List<string>();
            int p = 0;
            while (p < arrJson.Length)
            {
                int qs = arrJson.IndexOf('"', p);
                if (qs < 0) break;
                int qe = qs + 1;
                while (qe < arrJson.Length && arrJson[qe] != '"')
                {
                    if (arrJson[qe] == '\\' && qe + 1 < arrJson.Length) qe += 2;
                    else qe++;
                }
                list.Add(arrJson.Substring(qs + 1, qe - qs - 1).Replace("\\\"", "\""));
                p = qe + 1;
            }
            _responses[key] = list;
            idx = arrEnd + 1;
        }
    }

    static int ExtractInt(string json, string key)
    {
        int ki = json.IndexOf("\"" + key + "\"");
        if (ki < 0) return 0;
        int colon = json.IndexOf(':', ki);
        int comma = json.IndexOf(',', colon);
        int brace = json.IndexOf('}', colon);
        int end = comma < 0 ? brace : (brace < 0 ? comma : Math.Min(comma, brace));
        if (end < 0) end = json.Length;
        return int.Parse(json.Substring(colon + 1, end - colon - 1).Trim());
    }

    static void ParseStringIntDict(string json, string key, Dictionary<string, int> outDict)
    {
        int ki = json.IndexOf("\"" + key + "\"");
        if (ki < 0) return;
        int braceStart = json.IndexOf('{', ki);
        int braceEnd = FindMatching(json, braceStart, '{', '}');
        var inner = json.Substring(braceStart + 1, braceEnd - braceStart - 1);
        int p = 0;
        while (p < inner.Length)
        {
            int qs = inner.IndexOf('"', p);
            if (qs < 0) break;
            int qe = qs + 1;
            while (qe < inner.Length && inner[qe] != '"')
            {
                if (inner[qe] == '\\' && qe + 1 < inner.Length) qe += 2;
                else qe++;
            }
            string k = inner.Substring(qs + 1, qe - qs - 1);
            int colon = inner.IndexOf(':', qe);
            int comma = inner.IndexOf(',', colon);
            int end = comma < 0 ? inner.Length : comma;
            string vStr = inner.Substring(colon + 1, end - colon - 1).Trim();
            if (int.TryParse(vStr, out int v)) outDict[k] = v;
            p = end + 1;
        }
    }

    static void ParseStringStringDict(string json, string key, Dictionary<string, string> outDict)
    {
        int ki = json.IndexOf("\"" + key + "\"");
        if (ki < 0) return;
        int braceStart = json.IndexOf('{', ki);
        int braceEnd = FindMatching(json, braceStart, '{', '}');
        var inner = json.Substring(braceStart + 1, braceEnd - braceStart - 1);
        int p = 0;
        while (p < inner.Length)
        {
            int qs = inner.IndexOf('"', p);
            if (qs < 0) break;
            int qe = inner.IndexOf('"', qs + 1);
            string k = inner.Substring(qs + 1, qe - qs - 1);
            int qs2 = inner.IndexOf('"', qe + 1);
            int qe2 = inner.IndexOf('"', qs2 + 1);
            string v = inner.Substring(qs2 + 1, qe2 - qs2 - 1);
            outDict[k] = v;
            p = qe2 + 1;
        }
    }

    static int FindMatching(string s, int start, char open, char close)
    {
        int depth = 0;
        for (int i = start; i < s.Length; i++)
        {
            if (s[i] == open) depth++;
            else if (s[i] == close)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }
}

public interface IRuntimeContext
{
    string Get(string key);
}

public class DummyContext : IRuntimeContext
{
    public string Get(string key)
    {
        switch (key)
        {
            case "scheduled_today":  return "tập điều lệnh sáng và học chính trị buổi tối";
            case "meal_time":        return "11h30 trưa và 18h tối";
            case "minutes_to_meal":  return "30";
            case "place":            return "khu B";
            case "direction":        return "đông";
            case "distance":         return "100";
            case "block":            return "B5";
            case "topic":            return "súng AK";
            case "chapter":          return "3";
            case "summary":          return "tháo, lắp, bảo dưỡng";
            default:                 return null;
        }
    }
}

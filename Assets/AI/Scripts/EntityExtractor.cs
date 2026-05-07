// EntityExtractor.cs
//
// Phase A v2 — rule-based slot extractor cho NPC chat.
//
// Vấn đề con AI cũ giải: model classify intent rất tốt (HOI_VI_TRI khi user hỏi
// về địa điểm), nhưng response template dùng placeholder {place} luôn được fill
// bằng giá trị HARDCODED trong DummyContext (vd: "khu B"). Kết quả: user hỏi
// "khu A ở đâu" → AI vẫn nhả "khu A nằm ở khu B5". Trông như AI ngu — nhưng thực
// ra là tầng response không nhận biết user vừa hỏi cái gì.
//
// EntityExtractor đọc câu user, scan qua vocabulary slot (đồng bộ với
// generate_dataset_v4.py), longest-match rồi return slot map cho RuntimeContext
// dùng khi substitute template.
//
// Slot vocab được dump bởi `scripts/dump_slot_vocab.py` ra
// `Assets/AI/Resources/slot_vocab.json` (load qua TextAsset / Resources.Load).
//
// Usage:
//   var extractor = new EntityExtractor(slotVocabJson);
//   var slots = extractor.Extract("khu A ở đâu");
//   // slots["place"] == "khu A"

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class EntityExtractor
{
    // slotName -> list of phrases sorted by descending word count (longest-first match)
    private readonly Dictionary<string, List<string>> _slots = new();

    // Tokens that should be considered word boundaries
    private static readonly char[] kWordBreaks =
        new[] { ' ', '\t', '\n', '\r', '.', ',', '?', '!', ';', ':', '(', ')', '[', ']', '"', '\'' };

    public EntityExtractor(string slotVocabJsonText)
    {
        ParseVocab(slotVocabJsonText);
    }

    /// <summary>Extract first match per slot from text. Returns dict slotName -> matched phrase.</summary>
    public Dictionary<string, string> Extract(string text)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(text)) return result;

        string norm = Normalize(text);

        foreach (var kv in _slots)
        {
            string slot = kv.Key;
            // Phrases are pre-sorted longest-first; first contains-as-word match wins.
            foreach (string phrase in kv.Value)
            {
                if (ContainsAsWord(norm, phrase))
                {
                    result[slot] = phrase;
                    break;
                }
            }
        }
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Matching helpers
    // ─────────────────────────────────────────────────────────────────────
    private static string Normalize(string s)
    {
        // Lowercase and pad with spaces so word-boundary check at edges is uniform.
        var sb = new StringBuilder(s.Length + 2);
        sb.Append(' ');
        foreach (char c in s.ToLowerInvariant())
        {
            // Replace punctuation with spaces; keep all letters/digits/diacritics.
            bool isBreak = false;
            for (int i = 0; i < kWordBreaks.Length; i++)
            {
                if (kWordBreaks[i] == c) { isBreak = true; break; }
            }
            sb.Append(isBreak ? ' ' : c);
        }
        sb.Append(' ');
        // Collapse multi-spaces.
        var compact = new StringBuilder(sb.Length);
        bool prevSpace = false;
        for (int i = 0; i < sb.Length; i++)
        {
            char c = sb[i];
            if (c == ' ')
            {
                if (!prevSpace) compact.Append(c);
                prevSpace = true;
            }
            else
            {
                compact.Append(c);
                prevSpace = false;
            }
        }
        return compact.ToString();
    }

    /// <summary>Check that <paramref name="needle"/> appears as a whole word/phrase in pre-normalized <paramref name="hay"/>.</summary>
    private static bool ContainsAsWord(string hay, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return false;
        // hay starts and ends with space (Normalize), so wrap needle similarly.
        // Simple substring match is fine: "khu a" surrounded by spaces won't match "khu ab".
        string padded = " " + needle.ToLowerInvariant() + " ";
        return hay.IndexOf(padded, StringComparison.Ordinal) >= 0;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Tiny JSON parser (slot_vocab.json shape)
    //   { "_README": "...", "place": ["a", "b"], "time": ["..."], ... }
    // We only need top-level keys mapping to string arrays.
    // ─────────────────────────────────────────────────────────────────────
    private void ParseVocab(string json)
    {
        int idx = 0;
        while (idx < json.Length)
        {
            int kStart = json.IndexOf('"', idx);
            if (kStart < 0) break;
            int kEnd = FindStringEnd(json, kStart);
            if (kEnd < 0) break;
            string key = Unescape(json.Substring(kStart + 1, kEnd - kStart - 1));
            int colon = json.IndexOf(':', kEnd);
            if (colon < 0) break;

            // Skip _README and any non-array string values.
            int peek = SkipWs(json, colon + 1);
            if (peek >= json.Length) break;
            char first = json[peek];
            if (first != '[')
            {
                // string value -> skip past it
                if (first == '"')
                {
                    int strEnd = FindStringEnd(json, peek);
                    idx = strEnd + 1;
                }
                else
                {
                    int comma = json.IndexOf(',', peek);
                    int brace = json.IndexOf('}', peek);
                    int end = comma < 0 ? brace : (brace < 0 ? comma : Math.Min(comma, brace));
                    idx = end < 0 ? json.Length : end + 1;
                }
                continue;
            }

            int arrEnd = FindMatching(json, peek, '[', ']');
            if (arrEnd < 0) break;
            string arrJson = json.Substring(peek + 1, arrEnd - peek - 1);

            var list = new List<string>();
            int p = 0;
            while (p < arrJson.Length)
            {
                int qs = arrJson.IndexOf('"', p);
                if (qs < 0) break;
                int qe = FindStringEnd(arrJson, qs);
                if (qe < 0) break;
                list.Add(Unescape(arrJson.Substring(qs + 1, qe - qs - 1)));
                p = qe + 1;
            }
            // Sort longest-first (defensive — JSON should already be sorted by Python).
            list.Sort((a, b) =>
            {
                int wa = CountWords(a);
                int wb = CountWords(b);
                if (wa != wb) return wb - wa;
                return b.Length - a.Length;
            });
            _slots[key] = list;
            idx = arrEnd + 1;
        }
    }

    private static int FindStringEnd(string s, int quoteStart)
    {
        for (int i = quoteStart + 1; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length) { i++; continue; }
            if (s[i] == '"') return i;
        }
        return -1;
    }

    private static int FindMatching(string s, int start, char open, char close)
    {
        int depth = 0;
        for (int i = start; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"')
            {
                int e = FindStringEnd(s, i);
                if (e < 0) return -1;
                i = e;
                continue;
            }
            if (c == open) depth++;
            else if (c == close) { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static int SkipWs(string s, int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        return i;
    }

    private static string Unescape(string s)
    {
        if (s.IndexOf('\\') < 0) return s;
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '\\' && i + 1 < s.Length)
            {
                char n = s[i + 1];
                switch (n)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    default: sb.Append(n); break;
                }
                i++;
            }
            else sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static int CountWords(string s)
    {
        int n = 1;
        for (int i = 0; i < s.Length; i++) if (s[i] == ' ') n++;
        return n;
    }
}

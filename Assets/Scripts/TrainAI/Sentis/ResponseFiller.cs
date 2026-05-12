using System;
using System.Collections.Generic;
using System.Text;
using TrainAI.Core;
using UnityEngine;

namespace TrainAI.Sentis
{
    public class ResponseFiller : IResponseFiller
    {
        readonly Dictionary<IntentId, List<string>> _byIntent = new();

        public bool IsReady => _byIntent.Count > 0;

        public ResponseFiller(string responsesJson)
        {
            ParseResponses(responsesJson);
        }

        public string Fill(IntentId intent, Func<string, string> tagLookup)
        {
            if (!_byIntent.TryGetValue(intent, out var pool) || pool == null || pool.Count == 0)
                return "...";
            string template = pool[UnityEngine.Random.Range(0, pool.Count)];
            return Substitute(template, tagLookup);
        }

        static string Substitute(string s, Func<string, string> lookup)
        {
            int from = 0;
            var sb = new StringBuilder();
            while (from < s.Length)
            {
                int open = s.IndexOf('{', from);
                if (open < 0) { sb.Append(s, from, s.Length - from); break; }
                int close = s.IndexOf('}', open + 1);
                if (close < 0) { sb.Append(s, from, s.Length - from); break; }
                sb.Append(s, from, open - from);
                string key = s.Substring(open + 1, close - open - 1);
                string value = lookup != null ? lookup(key) : null;
                sb.Append(value ?? $"[{key}]");
                from = close + 1;
            }
            return sb.ToString();
        }

        void ParseResponses(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            int idx = 0;
            while (idx < json.Length)
            {
                int kStart = json.IndexOf('"', idx);
                if (kStart < 0) break;
                int kEnd = json.IndexOf('"', kStart + 1);
                if (kEnd < 0) break;
                string key = json.Substring(kStart + 1, kEnd - kStart - 1);
                int colon = json.IndexOf(':', kEnd);
                int arrStart = json.IndexOf('[', colon);
                int objStart = json.IndexOf('"', colon);
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
                if (Enum.TryParse<IntentId>(key, out var iv)) _byIntent[iv] = list;
                idx = arrEnd + 1;
            }
        }

        static int FindMatching(string s, int start, char open, char close)
        {
            int depth = 0;
            for (int i = start; i < s.Length; i++)
            {
                if (s[i] == open) depth++;
                else if (s[i] == close) { depth--; if (depth == 0) return i; }
            }
            return -1;
        }
    }
}

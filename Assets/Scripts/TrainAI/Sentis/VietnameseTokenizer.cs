using System;
using System.Collections.Generic;
using System.Text;
using TrainAI.Core;

namespace TrainAI.Sentis
{
    public class VietnameseTokenizer : ITokenizer
    {
        readonly Dictionary<string, int> _vocab = new();
        readonly int _maxLen;
        readonly int _padId;
        readonly int _unkId;
        readonly int _maxMultiWordLen;

        public int MaxLen => _maxLen;

        public VietnameseTokenizer(string metaJson)
        {
            _maxLen = ExtractInt(metaJson, "max_len");
            _padId = ExtractInt(metaJson, "pad_id");
            _unkId = ExtractInt(metaJson, "unk_id");
            ParseStringIntDict(metaJson, "vocab", _vocab);

            int max = 1;
            foreach (var key in _vocab.Keys)
            {
                int spaces = 0;
                for (int i = 0; i < key.Length; i++) if (key[i] == ' ') spaces++;
                if (spaces + 1 > max) max = spaces + 1;
            }
            _maxMultiWordLen = max;
        }

        public int[] Encode(string text)
        {
            text = (text ?? string.Empty).ToLowerInvariant().Trim();
            var sb = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) sb.Append(c);
                else sb.Append(' ');
            }
            var words = sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            var ids = new int[_maxLen];
            for (int i = 0; i < _maxLen; i++) ids[i] = _padId;

            int wi = 0, idIdx = 0;
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
                        matchedSpan = span; matchedId = id; break;
                    }
                }
                if (matchedSpan == 0) { ids[idIdx++] = _unkId; wi += 1; }
                else { ids[idIdx++] = matchedId; wi += matchedSpan; }
            }
            return ids;
        }

        static int ExtractInt(string json, string key)
        {
            int ki = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
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
            int ki = json.IndexOf("\"" + key + "\"", StringComparison.Ordinal);
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

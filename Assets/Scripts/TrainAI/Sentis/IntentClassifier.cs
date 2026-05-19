using System;
using System.Collections.Generic;
using TrainAI.Core;
using Unity.InferenceEngine;
using UnityEngine;

namespace TrainAI.Sentis
{
    public class IntentClassifier : IIntentClassifier
    {
        readonly Worker _worker;
        readonly int _maxLen;
        readonly Dictionary<int, IntentId> _id2intent;

        public bool IsReady => _worker != null;

        public IntentClassifier(Worker worker, int maxLen, string metaJson)
        {
            _worker = worker;
            _maxLen = maxLen;
            _id2intent = new Dictionary<int, IntentId>();
            ParseId2Label(metaJson, _id2intent);
        }

        public IntentResult Predict(int[] tokenIds)
        {
            if (_worker == null || tokenIds == null)
                return new IntentResult { intent = IntentId.OUT_OF_SCOPE, score = 0f };

            // Both input and output tensors must be Disposed deterministically.
            // PeekOutput returns a Tensor handle owned by the worker; the
            // backing native buffer is released on the next Schedule(), but
            // the handle wrapper itself accumulates allocations in the Job
            // allocator if never disposed. Symptom: "JobTempAlloc has
            // allocations more than 4 frames old" warning + GPU TDR after
            // sustained gameplay — exact pattern from the 2026-05-19 08:26
            // crash dump.
            using var input = new Tensor<int>(new TensorShape(1, _maxLen), tokenIds);
            _worker.Schedule(input);

            using var logitsT = (_worker.PeekOutput("logits") as Tensor<float>)
                                ?? (_worker.PeekOutput() as Tensor<float>);
            if (logitsT == null) return new IntentResult { intent = IntentId.OUT_OF_SCOPE };
            var logits = logitsT.DownloadToArray();
            return Softmax(logits);
        }

        IntentResult Softmax(float[] logits)
        {
            float maxLogit = float.NegativeInfinity;
            for (int i = 0; i < logits.Length; i++) if (logits[i] > maxLogit) maxLogit = logits[i];

            float sumExp = 0f;
            var probs = new float[logits.Length];
            for (int i = 0; i < logits.Length; i++)
            {
                probs[i] = Mathf.Exp(logits[i] - maxLogit);
                sumExp += probs[i];
            }
            int bestIdx = 0, secondIdx = 0;
            float bestProb = 0f, secondProb = 0f;
            for (int i = 0; i < probs.Length; i++)
            {
                probs[i] /= sumExp;
                if (probs[i] > bestProb) { secondIdx = bestIdx; secondProb = bestProb; bestIdx = i; bestProb = probs[i]; }
                else if (probs[i] > secondProb) { secondIdx = i; secondProb = probs[i]; }
            }
            return new IntentResult
            {
                intent = MapId(bestIdx),
                score = bestProb,
                second = MapId(secondIdx),
                secondScore = secondProb
            };
        }

        IntentId MapId(int id) => _id2intent.TryGetValue(id, out var v) ? v : IntentId.OUT_OF_SCOPE;

        static void ParseId2Label(string json, Dictionary<int, IntentId> outDict)
        {
            int ki = json.IndexOf("\"id2label\"", StringComparison.Ordinal);
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
                string label = inner.Substring(qs2 + 1, qe2 - qs2 - 1);
                if (int.TryParse(k, out int id) && Enum.TryParse<IntentId>(label, out var iv))
                    outDict[id] = iv;
                p = qe2 + 1;
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

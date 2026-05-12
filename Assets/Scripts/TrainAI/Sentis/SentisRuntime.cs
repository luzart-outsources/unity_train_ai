using System;
using TrainAI.Core;
using Unity.InferenceEngine;
using UnityEngine;

namespace TrainAI.Sentis
{
    public class SentisRuntime : ISentisRuntime
    {
        public Worker IntentWorker { get; private set; }
        public Worker SoldierWorker { get; private set; }
        public ITokenizer Tokenizer { get; private set; }
        public IIntentClassifier IntentClassifier { get; private set; }
        public IResponseFiller ResponseFiller { get; private set; }
        public bool IsReady { get; private set; }

        public SentisRuntime(ModelAsset intentModel, ModelAsset soldierModel,
                             TextAsset intentMetaJson, TextAsset responsesJson,
                             BackendKind preferred)
        {
            BackendType backend = preferred switch
            {
                BackendKind.GPUCompute => BackendType.GPUCompute,
                BackendKind.GPUPixel => BackendType.GPUPixel,
                _ => BackendType.CPU
            };

            try
            {
                if (intentModel != null)
                    IntentWorker = new Worker(ModelLoader.Load(intentModel), backend);
                if (soldierModel != null)
                    SoldierWorker = new Worker(ModelLoader.Load(soldierModel), backend);
                if (intentMetaJson != null && IntentWorker != null)
                {
                    Tokenizer = new VietnameseTokenizer(intentMetaJson.text);
                    IntentClassifier = new IntentClassifier(IntentWorker, Tokenizer.MaxLen, intentMetaJson.text);
                }
                if (responsesJson != null)
                    ResponseFiller = new ResponseFiller(responsesJson.text);
                IsReady = IntentWorker != null || SoldierWorker != null;
                Debug.Log($"[Sentis] runtime ready. intent={IntentWorker != null} soldier={SoldierWorker != null} tokenizer={Tokenizer != null}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Sentis] init failed: {e.Message}");
                IsReady = false;
            }
        }

        public void Dispose()
        {
            IntentWorker?.Dispose(); IntentWorker = null;
            SoldierWorker?.Dispose(); SoldierWorker = null;
            IsReady = false;
        }
    }
}

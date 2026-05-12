using Unity.InferenceEngine;

namespace TrainAI.Services
{
    public class SentisRuntimeStub : ISentisRuntime
    {
        public bool IsReady => false;
        public Worker IntentWorker => null;
        public Worker SoldierWorker => null;
        public void Dispose() { }
    }
}

using TrainAI.Core;

namespace TrainAI.Services
{
    public class SentisRuntimeStub : ISentisRuntime
    {
        public bool IsReady => false;
        public ITokenizer Tokenizer => null;
        public IIntentClassifier IntentClassifier => null;
        public IResponseFiller ResponseFiller => null;
        public void Dispose() { }
    }
}

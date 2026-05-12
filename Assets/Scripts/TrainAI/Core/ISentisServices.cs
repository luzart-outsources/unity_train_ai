using System;

namespace TrainAI.Core
{
    public interface ITokenizer
    {
        int[] Encode(string text);
        int MaxLen { get; }
    }

    public struct IntentResult
    {
        public IntentId intent;
        public float score;
        public IntentId second;
        public float secondScore;
    }

    public interface IIntentClassifier
    {
        IntentResult Predict(int[] tokenIds);
        bool IsReady { get; }
    }

    public interface IResponseFiller
    {
        string Fill(IntentId intent, Func<string, string> tagLookup);
        bool IsReady { get; }
    }

    public interface ISentisRuntime : IDisposable
    {
        bool IsReady { get; }
        ITokenizer Tokenizer { get; }
        IIntentClassifier IntentClassifier { get; }
        IResponseFiller ResponseFiller { get; }
    }
}

using System;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    public class NpcContext
    {
        public string playerName;
        public string todaySummary;
        public int hocTap;
        public int renLuyen;
        public Func<string, string> fillTemplate;

        public ITokenizer tokenizer;
        public IIntentClassifier intentClassifier;
        public IResponseFiller responseFiller;
    }
}

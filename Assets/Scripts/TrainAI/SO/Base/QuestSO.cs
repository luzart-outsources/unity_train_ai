using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    public abstract class QuestSO : ScriptableObject
    {
        [Required] public string id;
        public string title;
        public AreaSO area;
        public TimeRange window;
        public int latePenalty = 5;

        public abstract IQuestRuntime CreateRuntime(QuestContext ctx);
    }
}

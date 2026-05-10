using System;
using TrainAI.Configs;
using TrainAI.Core.Time;

namespace TrainAI.Systems.Quest
{
    [Serializable]
    public class QuestRuntimeState
    {
        public string questId;
        public QuestStatus status;
        public GameTime startedAt;
        public GameTime completedAt;
        public int scoreEarned;
        public int penaltyApplied;
    }
}

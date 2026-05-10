using System;
using System.Collections.Generic;
using TrainAI.Configs;

namespace TrainAI.Systems.Save
{
    [Serializable]
    public class GameSaveData
    {
        public int saveFormatVersion = 1;
        public string playerName = "";
        public int day = 1;
        public int hour = 5;
        public int minute = 0;
        public int weekdayIndex = 1;     // DayOfWeek as int
        public int discipline = 100;
        public int academic = 0;
        public string currentSceneId = "";
        public int currentQuestIndex = 0;
        public List<DayHistory> history = new List<DayHistory>();
        public long savedAtUnix;
    }

    [Serializable]
    public class DayHistory
    {
        public int day;
        public List<QuestRecord> quests = new List<QuestRecord>();
    }

    [Serializable]
    public class QuestRecord
    {
        public string questId;
        public int statusIndex;          // QuestStatus as int
        public int scoreEarned;
        public int penaltyApplied;
    }
}

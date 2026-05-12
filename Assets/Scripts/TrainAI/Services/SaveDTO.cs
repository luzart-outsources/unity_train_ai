using System;
using System.Collections.Generic;

namespace TrainAI.Services
{
    [Serializable]
    public class SaveDTO
    {
        public int version = 1;
        public string savedAt;
        public string playerName;
        public int day = 1;
        public int weekday = 1;
        public int hour = 5;
        public int minute = 0;
        public int hocTap = 0;
        public int renLuyen = 100;
        public string currentScene = "10_World";
        public float posX, posY, posZ;
        public List<string> completedQuestIds = new();
        public List<string> missedQuestIds = new();
    }
}

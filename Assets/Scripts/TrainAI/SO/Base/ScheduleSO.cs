using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [Serializable]
    public struct ScheduleEntry
    {
        public int hour;
        public int minute;
        public AreaSO target;
    }

    [CreateAssetMenu(fileName = "Schedule_New", menuName = "TrainAI/Data/Schedule")]
    public class ScheduleSO : ScriptableObject
    {
        public List<ScheduleEntry> entries = new();

        public ScheduleEntry GetEntryAt(int day, int hour, int minute)
        {
            int t = hour * 60 + minute;
            ScheduleEntry best = default;
            int bestT = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                int et = e.hour * 60 + e.minute;
                if (et <= t && et > bestT) { bestT = et; best = e; }
            }
            return best;
        }
    }
}

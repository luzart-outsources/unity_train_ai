using System;
using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // Lich di chuyen cua NPC theo gio.
    [CreateAssetMenu(menuName = "TrainAI/NPC/Schedule", fileName = "NS_Schedule")]
    public class NPCScheduleSO : ScriptableObject
    {
        [Serializable]
        public struct ScheduleEntry
        {
            [Slider(0, 23)] public int hour;
            [Tooltip("Khop voi InteractableSO.key. NPC se di toi diem co key nay.")]
            public string locationKey;
        }

        [InfoBox("GDD: NPC hoc sinh di chuyen theo thoi gian bieu. Vd: 6h san van dong, 7h dop vesinh, ...")]
        public List<ScheduleEntry> entries = new List<ScheduleEntry>();

        public string GetLocationAtHour(int hour)
        {
            string current = "";
            int bestHour = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.hour <= hour && e.hour > bestHour)
                {
                    bestHour = e.hour;
                    current = e.locationKey;
                }
            }
            return current;
        }
    }
}

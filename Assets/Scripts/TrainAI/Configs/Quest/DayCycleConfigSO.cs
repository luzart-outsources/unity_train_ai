using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Quest/Day Cycle", fileName = "DayCycle")]
    public class DayCycleConfigSO : ScriptableObject
    {
        [InfoBox("Index 0 = ngay 1, ... GDD demo 7 ngay (overview noi 7-day demo).")]
        public List<DayPlanSO> dayPlans = new List<DayPlanSO>();

        public DayPlanSO GetPlanForDay(int day)
        {
            for (int i = 0; i < dayPlans.Count; i++)
            {
                var p = dayPlans[i];
                if (p != null && p.dayNumber == day) return p;
            }
            return null;
        }

        public int Count => dayPlans != null ? dayPlans.Count : 0;
    }
}

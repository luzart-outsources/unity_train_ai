using System.Collections.Generic;
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "Day_New", menuName = "TrainAI/Data/Day")]
    public class DaySO : ScriptableObject
    {
        public int dayIndex;
        public Weekday weekday;
        public List<QuestSO> quests = new();
    }
}

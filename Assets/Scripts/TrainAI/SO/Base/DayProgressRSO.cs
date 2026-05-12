using System.Collections.Generic;
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "DayProgressRSO", menuName = "TrainAI/Runtime/Day Progress")]
    public class DayProgressRSO : RuntimeSO
    {
        public List<string> completedToday = new();
        public List<string> missedToday = new();

        public override void Reset()
        {
            completedToday.Clear();
            missedToday.Clear();
        }
    }
}

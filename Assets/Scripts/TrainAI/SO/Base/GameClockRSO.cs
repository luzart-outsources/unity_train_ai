using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "GameClockRSO", menuName = "TrainAI/Runtime/Game Clock")]
    public class GameClockRSO : RuntimeSO
    {
        public int day;
        public int hour;
        public int minute;
        public Weekday weekday;
        public bool frozen;

        public override void Reset()
        {
            day = 1;
            hour = 5;
            minute = 0;
            weekday = Weekday.Monday;
            frozen = false;
        }
    }
}

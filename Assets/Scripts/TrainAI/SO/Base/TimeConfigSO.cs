using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "TimeConfig", menuName = "TrainAI/Config/Time")]
    public class TimeConfigSO : ScriptableObject
    {
        public float gameHourPerRealMinute = 1f / 3f;
        public float idlePenaltyMinutes = 15f;
    }
}

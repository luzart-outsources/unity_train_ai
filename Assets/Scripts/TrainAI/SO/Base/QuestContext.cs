using UnityEngine;

namespace TrainAI.SO.Base
{
    public class QuestContext
    {
        public Transform player;
        public System.Action<int> awardScoreDelta;
        public System.Action<bool> markComplete;
    }
}

using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "ActiveQuestRSO", menuName = "TrainAI/Runtime/Active Quest")]
    public class ActiveQuestRSO : RuntimeSO
    {
        public QuestSO current;
        [System.NonSerialized] public IQuestRuntime runtimeInstance;
        public float deadlineRemainingSec;

        public override void Reset()
        {
            current = null;
            runtimeInstance = null;
            deadlineRemainingSec = 0f;
        }
    }
}

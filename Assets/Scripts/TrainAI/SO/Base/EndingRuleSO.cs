using UnityEngine;

namespace TrainAI.SO.Base
{
    public enum EndingGrade { XuatSac = 0, Tot = 1, TrungBinh = 2 }

    public abstract class EndingRuleSO : ScriptableObject
    {
        public abstract EndingGrade Evaluate(PlayerStateRSO state);
    }
}

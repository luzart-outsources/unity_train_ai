using UnityEngine;

namespace TrainAI.SO.Base
{
    public class ScoreContext
    {
        public int scoreDelta;
        public string sourceQuestId;
    }

    public abstract class ScoreRuleSO : ScriptableObject
    {
        public abstract void Apply(ScoreContext ctx, PlayerStateRSO state);
    }
}

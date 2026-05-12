using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Rule_LatePenalty", menuName = "TrainAI/Rule/Late Penalty")]
    public class LatePenaltyRuleSO : ScoreRuleSO
    {
        public int defaultPenalty = 5;

        public override void Apply(ScoreContext ctx, PlayerStateRSO state)
        {
            if (ctx.scoreDelta < 0) state.renLuyen += ctx.scoreDelta;
            state.renLuyen = Mathf.Max(0, state.renLuyen);
        }
    }
}

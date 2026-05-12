using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_GotoConfirm_New", menuName = "TrainAI/Quest/Goto + Confirm")]
    public class GotoConfirmQuestSO : QuestSO
    {
        public string confirmText = "Ban dang thuc hien hanh dong.";
        public int skipToHour = 6;
        public int skipToMinute = 0;

        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new GotoConfirmQuestRuntime(this, ctx);
    }

    internal class GotoConfirmQuestRuntime : IQuestRuntime
    {
        readonly GotoConfirmQuestSO _so;
        readonly QuestContext _ctx;

        public GotoConfirmQuestRuntime(GotoConfirmQuestSO so, QuestContext ctx)
        {
            _so = so;
            _ctx = ctx;
        }

        public void Begin() { }
        public void Tick(float dt) { }

        public void OnComplete(bool success)
        {
            if (!success) _ctx.awardScoreDelta?.Invoke(-_so.latePenalty);
        }
    }
}

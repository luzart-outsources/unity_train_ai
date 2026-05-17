using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_Sleep_New", menuName = "TrainAI/Quest/Sleep")]
    public class SleepQuestSO : QuestSO
    {
        public string confirmText = "Di ngu";
        public string loadingText = "Sang ngay hom sau...";
        // Sleeping restores discipline (renLuyen). Without this, a single
        // missed quest series in the middle of the run would drop renLuyen to
        // zero and there was no in-game way to recover, leaving the player
        // stuck with permanent Expel pressure.
        public int renLuyenRestore = 30;

        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new SleepQuestRuntime(this, ctx);
    }

    internal class SleepQuestRuntime : IQuestRuntime
    {
        readonly SleepQuestSO _so;
        readonly QuestContext _ctx;

        public SleepQuestRuntime(SleepQuestSO so, QuestContext ctx) { _so = so; _ctx = ctx; }
        public void Begin() { }
        public void Tick(float dt) { }
        public void OnComplete(bool success)
        {
            // QuestContext.awardScoreDelta routes through ScoreSystem.ApplyDelta(0, d, ...),
            // so a positive value heals renLuyen (the only stat the quest layer touches).
            if (success) _ctx.awardScoreDelta?.Invoke(_so.renLuyenRestore);
            else _ctx.awardScoreDelta?.Invoke(-_so.latePenalty);
        }
    }
}

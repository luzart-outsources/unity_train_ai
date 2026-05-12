using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_Sleep_New", menuName = "TrainAI/Quest/Sleep")]
    public class SleepQuestSO : QuestSO
    {
        public string confirmText = "Di ngu";
        public string loadingText = "Sang ngay hom sau...";

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
        public void OnComplete(bool success) { }
    }
}

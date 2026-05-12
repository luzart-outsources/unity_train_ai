using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_SceneTransition_New", menuName = "TrainAI/Quest/Scene Transition")]
    public class SceneTransitionQuestSO : QuestSO
    {
        public SceneRefSO subScene;
        public string transitionText = "Dang chuyen canh...";

        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new SceneTransitionQuestRuntime(this, ctx);
    }

    internal class SceneTransitionQuestRuntime : IQuestRuntime
    {
        readonly SceneTransitionQuestSO _so;
        readonly QuestContext _ctx;

        public SceneTransitionQuestRuntime(SceneTransitionQuestSO so, QuestContext ctx) { _so = so; _ctx = ctx; }
        public void Begin() { }
        public void Tick(float dt) { }
        public void OnComplete(bool success)
        {
            if (!success) _ctx.awardScoreDelta?.Invoke(-_so.latePenalty);
        }
    }
}

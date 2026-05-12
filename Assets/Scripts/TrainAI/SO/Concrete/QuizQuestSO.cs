using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_Quiz_New", menuName = "TrainAI/Quest/Quiz")]
    public class QuizQuestSO : QuestSO
    {
        public QuizSetSO quizSet;
        public SceneRefSO classroomScene;

        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new QuizQuestRuntime(this, ctx);
    }

    internal class QuizQuestRuntime : IQuestRuntime
    {
        readonly QuizQuestSO _so;
        readonly QuestContext _ctx;

        public QuizQuestRuntime(QuizQuestSO so, QuestContext ctx) { _so = so; _ctx = ctx; }
        public void Begin() { }
        public void Tick(float dt) { }
        public void OnComplete(bool success)
        {
            if (!success) _ctx.awardScoreDelta?.Invoke(-_so.latePenalty);
        }
    }
}

using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_OpenQuiz_New", menuName = "TrainAI/Interaction/Open Quiz")]
    public class OpenQuizInteractionSO : InteractionSO
    {
        public QuizSetSO quizSet;
        public bool completeQuestAfter = true;
        // Per GDD "Khi thực hiện hết Quiz sẽ ra khỏi Scene lớp học và trở về
        // Scene World." Set to SceneRef_10_World to auto-exit after the quiz.
        public SceneRefSO returnScene;
        public string returnTransitionText = "Roi lop hoc...";

        public override async UniTask Execute(InteractionContext ctx)
        {
            if (ctx.showQuiz == null || quizSet == null) return;
            var result = await ctx.showQuiz(quizSet);
            if (!result.cancelled && result.total > 0)
            {
                int gain = Mathf.RoundToInt((float)result.correct / result.total
                                            * (quizSet.subject != null ? quizSet.subject.maxPointsPerLesson : 40));
                ctx.applyScoreDelta?.Invoke(gain, 0, quizSet.subject != null ? quizSet.subject.id : "quiz");
            }
            if (completeQuestAfter) ctx.completeCurrentQuest?.Invoke(true);
            if (returnScene != null && ctx.loadReplacing != null)
                await ctx.loadReplacing(returnScene, returnTransitionText);
        }
    }
}

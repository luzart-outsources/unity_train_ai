using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_OpenQuiz_New", menuName = "TrainAI/Interaction/Open Quiz")]
    public class OpenQuizInteractionSO : InteractionSO
    {
        public QuizSetSO quizSet;

        public override async UniTask Execute(InteractionContext ctx)
        {
            await UniTask.Yield();
            Debug.Log($"[OpenQuiz] open UIQuiz set={(quizSet != null ? quizSet.name : "null")} (UIRouter wired in scene)");
        }
    }
}

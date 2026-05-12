using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class UIRouter : IUIRouter
    {
        public UniTask<bool> ShowConfirm(string text)
        {
            Debug.Log($"[UIRouter stub] ShowConfirm: {text}");
            return UniTask.FromResult(true);
        }

        public UniTask<QuizResult> ShowQuiz(QuizSetSO set)
        {
            Debug.Log($"[UIRouter stub] ShowQuiz: {(set != null ? set.name : "null")}");
            return UniTask.FromResult(new QuizResult { correctCount = 0, totalCount = set != null ? set.questions.Count : 0 });
        }

        public async UniTask ShowLoading(string text, float seconds = 3f)
        {
            Debug.Log($"[UIRouter stub] ShowLoading: {text}");
            await UniTask.Delay((int)(seconds * 1000));
        }

        public UniTask ShowDialogue(NPCSO npc)
        {
            Debug.Log($"[UIRouter stub] ShowDialogue: {(npc != null ? npc.displayName : "null")}");
            return UniTask.CompletedTask;
        }

        public void ShowEnding(EndingGrade grade)
            => Debug.Log($"[UIRouter stub] ShowEnding: {grade}");

        public void ShowExpel()
            => Debug.Log($"[UIRouter stub] ShowExpel");
    }
}

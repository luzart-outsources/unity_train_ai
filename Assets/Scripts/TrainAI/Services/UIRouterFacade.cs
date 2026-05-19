using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    /// <summary>
    /// Facade so that services constructed at Bootstrap time keep a stable
    /// IUIRouter ref, while UIRouterMono can swap the inner backend later
    /// (when scene-side Canvas finishes Awake). Prevents "stub UI is still
    /// being called after UIRouterMono overrode" bug.
    ///
    /// Fail-CLOSED policy on confirm: if Inner is not wired yet, ShowConfirm
    /// resolves to FALSE (user-pressed-Cancel), not TRUE. Resolving to TRUE
    /// turned an unwired UI chain into a silent auto-confirm — that's how
    /// SleepInteraction "đến KTX là chuyển sang ngày khác luôn" happened
    /// (no popup shown, but ok==true, so day advanced anyway). Returning
    /// false makes a broken UI surface as "interaction does nothing" which
    /// is loud + safe.
    /// </summary>
    public class UIRouterFacade : IUIRouter
    {
        public IUIRouter Inner { get; set; }

        public UniTask<bool> ShowConfirm(string text)
        {
            if (Inner != null) return Inner.ShowConfirm(text);
            Debug.LogError("[UIRouterFacade] ShowConfirm: Inner not wired — returning Cancel (false) to fail-closed");
            return UniTask.FromResult(false);
        }
        public UniTask<QuizResult> ShowQuiz(QuizSetSO set)
            => Inner != null ? Inner.ShowQuiz(set) : UniTask.FromResult(new QuizResult { totalCount = set != null ? set.questions.Count : 0, cancelled = true });
        public UniTask ShowLoading(string text, float seconds = 3f)
            => Inner != null ? Inner.ShowLoading(text, seconds) : UniTask.Delay((int)(seconds * 1000));
        public UniTask ShowDialogue(NPCSO npc)
            => Inner != null ? Inner.ShowDialogue(npc) : UniTask.CompletedTask;
        public void ShowEnding(EndingGrade grade) { Inner?.ShowEnding(grade); }
        public void ShowExpel() { Inner?.ShowExpel(); }
    }
}

using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;

namespace TrainAI.Services
{
    /// <summary>
    /// Facade so that services constructed at Bootstrap time keep a stable
    /// IUIRouter ref, while UIRouterMono can swap the inner backend later
    /// (when scene-side Canvas finishes Awake). Prevents "stub UI is still
    /// being called after UIRouterMono overrode" bug.
    /// </summary>
    public class UIRouterFacade : IUIRouter
    {
        public IUIRouter Inner { get; set; }

        public UniTask<bool> ShowConfirm(string text)
            => Inner != null ? Inner.ShowConfirm(text) : UniTask.FromResult(true);
        public UniTask<QuizResult> ShowQuiz(QuizSetSO set)
            => Inner != null ? Inner.ShowQuiz(set) : UniTask.FromResult(new QuizResult { totalCount = set != null ? set.questions.Count : 0 });
        public UniTask ShowLoading(string text, float seconds = 3f)
            => Inner != null ? Inner.ShowLoading(text, seconds) : UniTask.Delay((int)(seconds * 1000));
        public UniTask ShowDialogue(NPCSO npc)
            => Inner != null ? Inner.ShowDialogue(npc) : UniTask.CompletedTask;
        public void ShowEnding(EndingGrade grade) { Inner?.ShowEnding(grade); }
        public void ShowExpel() { Inner?.ShowExpel(); }
    }
}

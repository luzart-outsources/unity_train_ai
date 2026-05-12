using Cysharp.Threading.Tasks;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIRouterMono : MonoBehaviour, IUIRouter
    {
        [SerializeField] ServiceLocatorSO services;

        [Header("Screens (Canvas children)")]
        [SerializeField] UIConfirmController confirm;
        [SerializeField] UIDialogueController dialogue;
        [SerializeField] UILoadingController loading;
        [SerializeField] UIEndingController ending;
        [SerializeField] UIExpelController expel;

        void Awake() { if (services != null) services.OverrideUI(this); }

        public UniTask<bool> ShowConfirm(string text)
            => confirm != null ? confirm.ShowAsync(text) : UniTask.FromResult(true);

        public UniTask<QuizResult> ShowQuiz(QuizSetSO set)
            => UniTask.FromResult(new QuizResult { correctCount = 0, totalCount = set != null ? set.questions.Count : 0 });

        public UniTask ShowLoading(string text, float seconds = 3f)
            => loading != null ? loading.ShowAsync(text, seconds) : UniTask.Delay((int)(seconds * 1000));

        public UniTask ShowDialogue(NPCSO npc)
        {
            if (dialogue == null || services == null) return UniTask.CompletedTask;
            return dialogue.ShowAsync(npc, input => services.Dialogue.Reply(npc, input));
        }

        public void ShowEnding(EndingGrade grade) { if (ending != null) ending.ShowGrade(grade); }

        public void ShowExpel() { if (expel != null) expel.Show(); }
    }
}

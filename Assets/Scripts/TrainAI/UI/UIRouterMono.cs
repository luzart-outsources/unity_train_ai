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
        [SerializeField] UIQuizController quiz;

        void Awake()
        {
            if (services != null) services.OverrideUI(this);
            // Survive scene loads as a single root so HUDCanvas + ModalCanvas children stay together.
            var root = transform.root.gameObject;
            DontDestroyOnLoad(root);
        }

        public UniTask<bool> ShowConfirm(string text)
        {
            // Fail-closed when the confirm panel isn't wired — previously
            // returned FromResult(true) which silently auto-OK'd. Matches
            // the user-reported bug "đến KTX là chuyển sang ngày khác luôn"
            // pattern: bed interaction proceeded to advanceDay without ever
            // showing the popup. See SleepInteractionSO comments.
            if (confirm != null) return confirm.ShowAsync(text);
            Debug.LogError("[UIRouterMono] ShowConfirm: confirm panel not assigned in inspector — returning Cancel (false)");
            return UniTask.FromResult(false);
        }

        public UniTask<QuizResult> ShowQuiz(QuizSetSO set)
            => quiz != null ? quiz.ShowAsync(set)
                : UniTask.FromResult(new QuizResult { correctCount = 0, totalCount = set != null ? set.questions.Count : 0, cancelled = true });

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

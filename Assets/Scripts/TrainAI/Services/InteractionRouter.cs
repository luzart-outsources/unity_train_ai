using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class InteractionRouter : IInteractionRouter
    {
        readonly IQuestRouter _quests;
        readonly IUIRouter _ui;
        readonly ISceneRouter _scenes;
        readonly IGameClock _clock;
        readonly IScoreSystem _score;

        public InteractionRouter(IQuestRouter quests, IUIRouter ui,
                                  ISceneRouter scenes, IGameClock clock, IScoreSystem score)
        {
            _quests = quests;
            _ui = ui;
            _scenes = scenes;
            _clock = clock;
            _score = score;
        }

        public async UniTask HandlePressed(InteractableSO interactable)
        {
            if (interactable == null || interactable.onInteract == null) return;
            if (!_quests.IsInteractableAllowed(interactable))
            {
                if (_ui != null) await _ui.ShowConfirm("Chua toi gio lam viec nay");
                return;
            }
            var ctx = new InteractionContext
            {
                area = interactable.area,
                showConfirm = _ui != null ? _ui.ShowConfirm : null,
                loadAdditive = _scenes != null ? _scenes.LoadAdditive : null,
                unloadAdditive = _scenes != null ? _scenes.UnloadAdditive : null,
                loadReplacing = _scenes != null ? _scenes.LoadReplacing : null,
                skipTimeTo = _clock != null ? _clock.SkipTo : null,
                completeCurrentQuest = success => _quests?.Complete(_quests.Current, success),
                showDialogue = _ui != null ? _ui.ShowDialogue : null,
                showQuiz = _ui != null ? AdaptQuiz : null,
                applyScoreDelta = _score != null ? _score.ApplyDelta : null,
                showLoading = _ui != null ? (text, sec) => _ui.ShowLoading(text, sec) : null,
                advanceDay = _clock != null ? AdvanceDayAndReset : null,
            };
            await interactable.onInteract.Execute(ctx);
        }

        async UniTask<(int correct, int total, bool cancelled)> AdaptQuiz(QuizSetSO set)
        {
            if (_ui == null) return (0, 0, true);
            var r = await _ui.ShowQuiz(set);
            return (r.correctCount, r.totalCount, r.cancelled);
        }

        void AdvanceDayAndReset()
        {
            _clock?.AdvanceDay();
            _clock?.SkipTo(4, 30);
        }
    }
}

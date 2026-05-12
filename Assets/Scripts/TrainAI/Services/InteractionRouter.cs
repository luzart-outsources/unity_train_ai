using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class InteractionRouter : IInteractionRouter
    {
        readonly IQuestRouter _quests;
        readonly IUIRouter _ui;

        public InteractionRouter(IQuestRouter quests, IUIRouter ui)
        {
            _quests = quests;
            _ui = ui;
        }

        public async UniTask HandlePressed(InteractableSO interactable)
        {
            if (interactable == null) return;
            if (!_quests.IsInteractableAllowed(interactable))
            {
                if (_ui != null) await _ui.ShowConfirm("Chua toi gio lam viec nay");
                return;
            }
            var ctx = new InteractionContext { area = interactable.area };
            await interactable.onInteract.Execute(ctx);
        }
    }
}

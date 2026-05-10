using Luzart.NewBase;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.HUD
{
    // GDD: nut tuong tac sang khi player gan + camera huong toi 1 InteractableSO,
    // toi khi quay di. Dung SelectToggleImage de switch sprite sang/toi.
    public class InteractButton : MonoBehaviour
    {
        [SerializeField] private SelectToggleImage state;   // toggle: sprite[0] = toi, sprite[1] = sang
        [SerializeField] private Button button;

        private void OnEnable()
        {
            if (button != null) button.onClick.AddListener(OnClick);
            if (GameServices.Interaction != null)
            {
                GameServices.Interaction.OnEnter += OnEnter;
                GameServices.Interaction.OnExit += OnExit;
            }
            SetState(false);
        }

        private void OnDisable()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
            if (GameServices.Interaction != null)
            {
                GameServices.Interaction.OnEnter -= OnEnter;
                GameServices.Interaction.OnExit -= OnExit;
            }
        }

        private void OnEnter(InteractableSO data) => SetState(true);
        private void OnExit() => SetState(false);

        private void OnClick()
        {
            GameServices.Interaction?.Activate();
        }

        private void SetState(bool active)
        {
            if (state != null) state.Select(active);
            if (button != null) button.interactable = active;
        }
    }
}

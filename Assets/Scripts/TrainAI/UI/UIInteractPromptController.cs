using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIInteractPromptController : MonoBehaviour
    {
        [SerializeField] TMP_Text promptText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] string fallbackPrompt = "Bam E de tuong tac";

        void OnEnable()
        {
            BroadcastService.Subscribe<InteractZoneEnteredMsg>(OnEnter);
            BroadcastService.Subscribe<InteractZoneExitedMsg>(OnExit);
            BroadcastService.Subscribe<InteractPressedMsg>(OnPressed);
            SetVisible(false);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<InteractZoneEnteredMsg>(OnEnter);
            BroadcastService.Unsubscribe<InteractZoneExitedMsg>(OnExit);
            BroadcastService.Unsubscribe<InteractPressedMsg>(OnPressed);
        }

        void OnEnter(InteractZoneEnteredMsg m)
        {
            var inter = m.target as InteractableSO;
            if (inter == null) return;
            if (promptText != null)
                promptText.text = inter.onInteract != null && !string.IsNullOrEmpty(inter.onInteract.promptText)
                    ? inter.onInteract.promptText : fallbackPrompt;
            SetVisible(true);
        }

        void OnExit(InteractZoneExitedMsg _) => SetVisible(false);
        void OnPressed(InteractPressedMsg _) => SetVisible(false);

        void SetVisible(bool v)
        {
            if (canvasGroup != null) { canvasGroup.alpha = v ? 1 : 0; }
            else gameObject.SetActive(v);
        }
    }
}

using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    /// Touch-friendly "E" button that mirrors the desktop interact key.
    /// Subscribes to InteractZone messages to know when there is a valid target,
    /// auto-hides when out of range, and re-broadcasts InteractPressedMsg on tap.
    public class UIInteractButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] TMP_Text label;

        UnityEngine.Object _currentTarget;

        void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (button != null) button.onClick.AddListener(OnTap);
            SetVisible(false);
        }

        void OnEnable()
        {
            BroadcastService.Subscribe<InteractZoneEnteredMsg>(OnEnter);
            BroadcastService.Subscribe<InteractZoneExitedMsg>(OnExit);
            BroadcastService.Subscribe<InteractPressedMsg>(OnPressed);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<InteractZoneEnteredMsg>(OnEnter);
            BroadcastService.Unsubscribe<InteractZoneExitedMsg>(OnExit);
            BroadcastService.Unsubscribe<InteractPressedMsg>(OnPressed);
        }

        void OnEnter(InteractZoneEnteredMsg m)
        {
            if (m.target == null) return;
            _currentTarget = m.target;
            SetVisible(true);
        }

        void OnExit(InteractZoneExitedMsg m)
        {
            if (_currentTarget == m.target) _currentTarget = null;
            if (_currentTarget == null) SetVisible(false);
        }

        void OnPressed(InteractPressedMsg _)
        {
            // After a press, target stays valid until OnExit; nothing to do here.
        }

        void OnTap()
        {
            if (_currentTarget == null) return;
            BroadcastService.Send(new InteractPressedMsg(_currentTarget));
        }

        void SetVisible(bool v)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = v ? 1f : 0f;
                canvasGroup.blocksRaycasts = v;
                canvasGroup.interactable = v;
            }
            else
            {
                gameObject.SetActive(v);
            }
        }
    }
}

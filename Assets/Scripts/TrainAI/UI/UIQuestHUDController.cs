using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIQuestHUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text windowText;
        [SerializeField] CanvasGroup canvasGroup;

        void OnEnable()
        {
            BroadcastService.Subscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Subscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Subscribe<QuestMissedMsg>(OnEnded);
            SetVisible(false);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Unsubscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Unsubscribe<QuestMissedMsg>(OnEnded);
        }

        void OnActivated(QuestActivatedMsg msg)
        {
            var q = msg.quest as QuestSO;
            if (q == null) { SetVisible(false); return; }
            if (titleText != null) titleText.text = q.title;
            if (windowText != null) windowText.text = $"{q.window.startHour:00}:{q.window.startMinute:00} - {q.window.endHour:00}:{q.window.endMinute:00}";
            SetVisible(true);
        }

        void OnEnded<T>(T _) => SetVisible(false);

        void SetVisible(bool v)
        {
            if (canvasGroup != null) { canvasGroup.alpha = v ? 1 : 0; }
            else gameObject.SetActive(v);
        }
    }
}

using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIClockHUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text dayText;
        [SerializeField] TMP_Text timeText;
        [SerializeField] GameClockRSO clock;

        void OnEnable()
        {
            BroadcastService.Subscribe<TimeTickMsg>(OnTick);
            BroadcastService.Subscribe<DayStartedMsg>(OnDayStarted);
            RefreshDay();
        }
        void OnDisable()
        {
            BroadcastService.Unsubscribe<TimeTickMsg>(OnTick);
            BroadcastService.Unsubscribe<DayStartedMsg>(OnDayStarted);
        }

        // Day text only changes once per game-day, so refresh on the event
        // instead of formatting a new string into a TMP buffer every Update
        // (the per-frame cost was small but constant; TMP re-tessellation
        // adds up at 60+ FPS when nothing meaningful changed).
        void OnDayStarted(DayStartedMsg _) => RefreshDay();

        void RefreshDay()
        {
            if (clock != null && dayText != null)
                dayText.text = $"Ngay {clock.day} ({clock.weekday})";
        }

        void OnTick(TimeTickMsg m)
        {
            if (timeText != null) timeText.text = $"{m.hour:00}:{m.minute:00}";
        }
    }
}

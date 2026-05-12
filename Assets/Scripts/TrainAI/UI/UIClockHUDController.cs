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

        void OnEnable() => BroadcastService.Subscribe<TimeTickMsg>(OnTick);
        void OnDisable() => BroadcastService.Unsubscribe<TimeTickMsg>(OnTick);

        void Update()
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

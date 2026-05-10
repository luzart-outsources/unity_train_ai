using TMPro;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using TrainAI.Core.Time;
using UnityEngine;

namespace TrainAI.UI.HUD
{
    public class ClockHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtClock;
        [SerializeField] private TextMeshProUGUI txtDay;

        private void OnEnable()
        {
            GameEvents.TimeTick += OnTick;
            GameEvents.NewDay += OnNewDay;
            if (GameServices.Time != null) RenderTime(GameServices.Time.Now);
        }

        private void OnDisable()
        {
            GameEvents.TimeTick -= OnTick;
            GameEvents.NewDay -= OnNewDay;
        }

        private void OnTick(GameTime t) => RenderTime(t);
        private void OnNewDay(int d)
        {
            if (GameServices.Time != null) RenderTime(GameServices.Time.Now);
        }

        private void RenderTime(GameTime t)
        {
            if (txtClock != null) txtClock.text = $"{t.hour:D2}:{t.minute:D2}";
            if (txtDay != null) txtDay.text = $"{WeekdayShort(t.weekday)}, Ngay {t.day}";
        }

        private static string WeekdayShort(System.DayOfWeek d)
        {
            switch (d)
            {
                case System.DayOfWeek.Monday: return "T2";
                case System.DayOfWeek.Tuesday: return "T3";
                case System.DayOfWeek.Wednesday: return "T4";
                case System.DayOfWeek.Thursday: return "T5";
                case System.DayOfWeek.Friday: return "T6";
                case System.DayOfWeek.Saturday: return "T7";
                default: return "CN";
            }
        }
    }
}

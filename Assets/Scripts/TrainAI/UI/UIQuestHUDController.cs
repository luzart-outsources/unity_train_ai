using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    /// Top-right quest panel: shows the active quest's title, a concrete objective hint
    /// (e.g. "Den San Van Dong"), the time window, and a live countdown that ticks down
    /// each minute via TimeTickMsg.
    public class UIQuestHUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text objectiveText;
        [SerializeField] TMP_Text windowText;
        [SerializeField] TMP_Text countdownText;
        [SerializeField] CanvasGroup canvasGroup;

        QuestSO _active;

        void OnEnable()
        {
            BroadcastService.Subscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Subscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Subscribe<QuestMissedMsg>(OnEnded);
            BroadcastService.Subscribe<TimeTickMsg>(OnTick);
            SetVisible(false);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Unsubscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Unsubscribe<QuestMissedMsg>(OnEnded);
            BroadcastService.Unsubscribe<TimeTickMsg>(OnTick);
        }

        void OnActivated(QuestActivatedMsg msg)
        {
            _active = msg.quest as QuestSO;
            if (_active == null) { SetVisible(false); return; }

            if (titleText != null) titleText.text = _active.title;
            if (objectiveText != null) objectiveText.text = BuildObjectiveText(_active);
            if (windowText != null) windowText.text =
                $"{_active.window.startHour:00}:{_active.window.startMinute:00}" +
                $" - {_active.window.endHour:00}:{_active.window.endMinute:00}";
            if (countdownText != null) countdownText.text = "";
            SetVisible(true);
        }

        void OnEnded<T>(T _) { _active = null; SetVisible(false); }

        void OnTick(TimeTickMsg m)
        {
            if (_active == null || countdownText == null) return;
            int nowMin = m.hour * 60 + m.minute;
            int endMin = _active.window.endHour * 60 + _active.window.endMinute;
            int remaining = endMin - nowMin;
            countdownText.text = remaining > 0 ? $"Con {remaining} phut" : "He't gio";
        }

        // Quest classes don't share an "objective" field, so we synthesize one from the
        // available info (target area + quest type guessed by class name suffix). This
        // keeps the HUD informative without touching every concrete QuestSO subclass.
        static string BuildObjectiveText(QuestSO q)
        {
            string area = q.area != null ? Humanize(q.area.id) : null;
            string typeName = q.GetType().Name;

            if (typeName.StartsWith("GotoConfirm")) return area != null ? $"Den {area} va xac nhan" : "Den dia diem va xac nhan";
            if (typeName.StartsWith("Quiz"))         return area != null ? $"Den {area} de lam bai" : "Lam bai kiem tra";
            if (typeName.StartsWith("SceneTransition")) return area != null ? $"Den {area} de vao trong" : "Vao khu vuc";
            if (typeName.StartsWith("Sleep"))        return area != null ? $"Den {area} de di ngu" : "Di ngu";
            if (typeName.StartsWith("FreeRoam"))     return "Tu do trong khoang nay";
            return area != null ? $"Den {area}" : "";
        }

        static string Humanize(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            // "SanVanDong" -> "San Van Dong"; "LopHoc_Door" -> "Lop Hoc"
            var s = id.Replace("_Door", "").Replace("_", " ");
            var sb = new System.Text.StringBuilder(s.Length + 4);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && !char.IsWhiteSpace(s[i - 1]) && !char.IsUpper(s[i - 1]))
                    sb.Append(' ');
                sb.Append(c);
            }
            return sb.ToString();
        }

        void SetVisible(bool v)
        {
            if (canvasGroup != null) { canvasGroup.alpha = v ? 1 : 0; }
            else gameObject.SetActive(v);
        }
    }
}

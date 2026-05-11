using System.Collections.Generic;
using TrainAI.Configs;
using TrainAI.Core.Events;
using TrainAI.Core.Time;
using UnityEngine;

namespace TrainAI.Systems.Quest
{
    public class QuestManager : ITimeTickListener
    {
        private readonly DayCycleConfigSO _cfg;
        private readonly GameClock _time;

        private DayPlanSO _todayPlan;
        private int _currentQuestIndex;
        private readonly List<QuestRuntimeState> _todayStates = new List<QuestRuntimeState>();

        public DayPlanSO TodayPlan => _todayPlan;
        public int CurrentQuestIndex => _currentQuestIndex;

        public QuestDefSO CurrentQuest =>
            _todayPlan != null && _currentQuestIndex >= 0 && _currentQuestIndex < _todayPlan.quests.Count
                ? _todayPlan.quests[_currentQuestIndex]
                : null;

        public QuestRuntimeState CurrentState =>
            _currentQuestIndex >= 0 && _currentQuestIndex < _todayStates.Count
                ? _todayStates[_currentQuestIndex]
                : null;

        public IReadOnlyList<QuestRuntimeState> TodayStates => _todayStates;

        public QuestManager(DayCycleConfigSO cfg, GameClock time)
        {
            _cfg = cfg;
            _time = time;
            if (_time != null) _time.OnTick += OnTick;
            if (_time != null) _time.OnNewDay += OnNewDay;
        }

        public void Dispose()
        {
            if (_time != null)
            {
                _time.OnTick -= OnTick;
                _time.OnNewDay -= OnNewDay;
            }
        }

        public void StartDay(int day)
        {
            _todayPlan = _cfg != null ? _cfg.GetPlanForDay(day) : null;
            _currentQuestIndex = 0;
            _todayStates.Clear();
            if (_todayPlan == null)
            {
                Debug.LogWarning($"[QuestManager] No DayPlan for day {day}.");
                return;
            }
            for (int i = 0; i < _todayPlan.quests.Count; i++)
            {
                var q = _todayPlan.quests[i];
                _todayStates.Add(new QuestRuntimeState
                {
                    questId = q != null ? q.id : null,
                    status = QuestStatus.NotStarted,
                });
            }
            GameEvents.RaiseQuestChanged(CurrentQuest);
        }

        private void OnNewDay(int newDay)
        {
            StartDay(newDay);
        }

        public void OnTick(GameTime now)
        {
            GameEvents.RaiseTimeTick(now);
            CheckLateAndMissed(now);
        }

        private void CheckLateAndMissed(GameTime now)
        {
            var q = CurrentQuest;
            var st = CurrentState;
            if (q == null || st == null) return;
            if (st.status == QuestStatus.Active || st.status == QuestStatus.Completed) return;
            if (_time == null || _time.Config == null) return;

            int nowTotal = now.hour * 60 + now.minute;
            int deadlineTotal = q.deadlineHour * 60 + q.deadlineMinute;
            int skipTotal = q.skipToHour * 60 + q.skipToMinute;
            int lateAfter = _time.Config.lateAfterMinutes;

            // Late: qua deadline + lateAfterMinutes (1 lan).
            int lateThreshold = deadlineTotal + lateAfter;
            if (st.status == QuestStatus.NotStarted && nowTotal >= lateThreshold && st.penaltyApplied == 0)
            {
                st.status = QuestStatus.Late;
                st.penaltyApplied = q.penaltyOnLate;
                var sm = TrainAI.Core.Bootstrap.GameServices.Score;
                sm?.PenalizeDiscipline(q.penaltyOnLate);
                GameEvents.RaiseQuestLate(q);
            }

            // Missed: qua skipToTime ma chua Active -> auto skip.
            if ((st.status == QuestStatus.NotStarted || st.status == QuestStatus.Late) &&
                nowTotal >= skipTotal && skipTotal > deadlineTotal)
            {
                MarkMissed();
            }
        }

        public void StartCurrentQuest()
        {
            var q = CurrentQuest;
            var st = CurrentState;
            if (q == null || st == null) return;
            if (st.status == QuestStatus.Active || st.status == QuestStatus.Completed) return;

            st.status = QuestStatus.Active;
            st.startedAt = _time != null ? _time.Now : default;
            GameEvents.RaiseQuestStarted(q);
        }

        public void CompleteCurrentQuest(int scoreEarned)
        {
            var q = CurrentQuest;
            var st = CurrentState;
            if (q == null || st == null) return;

            st.status = QuestStatus.Completed;
            st.scoreEarned = scoreEarned;
            st.completedAt = _time != null ? _time.Now : default;

            if (scoreEarned > 0)
            {
                var sm = TrainAI.Core.Bootstrap.GameServices.Score;
                sm?.AddAcademic(scoreEarned);
            }

            GameEvents.RaiseQuestCompleted(q, scoreEarned);

            // Skip thoi gian theo GDD.
            if (_time != null) _time.JumpTo(q.skipToHour, q.skipToMinute);

            AdvanceToNextQuest();
        }

        public void MarkMissed()
        {
            var q = CurrentQuest;
            var st = CurrentState;
            if (q == null || st == null) return;
            if (st.status == QuestStatus.Completed) return;

            st.status = QuestStatus.Missed;
            if (st.penaltyApplied == 0)
            {
                st.penaltyApplied = q.penaltyOnMissed;
                var sm = TrainAI.Core.Bootstrap.GameServices.Score;
                sm?.PenalizeDiscipline(q.penaltyOnMissed);
            }
            GameEvents.RaiseQuestMissed(q);
            AdvanceToNextQuest();
        }

        public void AdvanceToNextQuest()
        {
            _currentQuestIndex++;
            GameEvents.RaiseQuestChanged(CurrentQuest);
        }

        public bool IsLastQuestCompletedToday()
        {
            if (_todayPlan == null) return false;
            return _currentQuestIndex >= _todayPlan.quests.Count;
        }
    }
}

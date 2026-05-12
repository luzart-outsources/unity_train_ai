using System.Collections.Generic;
using System.Linq;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class QuestRouter : IQuestRouter
    {
        readonly DayDB _dayDB;
        readonly ActiveQuestRSO _activeQuest;
        readonly GameClockRSO _clock;
        readonly DayProgressRSO _dayProgress;
        readonly IScoreSystem _scoreSystem;

        DaySO _currentDay;
        readonly Queue<QuestSO> _pendingToday = new();

        public QuestRouter(DayDB dayDB, ActiveQuestRSO activeQuest, GameClockRSO clock,
                           DayProgressRSO dayProgress, IScoreSystem scoreSystem)
        {
            _dayDB = dayDB;
            _activeQuest = activeQuest;
            _clock = clock;
            _dayProgress = dayProgress;
            _scoreSystem = scoreSystem;
            BroadcastService.Subscribe<DayStartedMsg>(OnDayStarted);
        }

        void OnDayStarted(DayStartedMsg msg) => StartDay(msg.day);

        public QuestSO Current => _activeQuest.current;

        public void StartDay(int day)
        {
            _currentDay = _dayDB != null ? _dayDB.ByIndex(day) : null;
            _pendingToday.Clear();
            if (_currentDay != null)
            {
                foreach (var q in _currentDay.quests.OrderBy(QuestStartMinutes))
                    if (q != null) _pendingToday.Enqueue(q);
            }
            _dayProgress.Reset();
            CheckActivate();
        }

        static int QuestStartMinutes(QuestSO q) => q.window.startHour * 60 + q.window.startMinute;

public void Tick(float dt)
        {
            // Auto-init Day 1 quests on first tick — Bootstrap doesn't fire DayStartedMsg for the starting day.
            if (_currentDay == null && _dayDB != null) StartDay(_clock.day);

            CheckActivate();
            if (_activeQuest.current != null)
            {
                int now = _clock.hour * 60 + _clock.minute;
                int deadline = _activeQuest.current.window.endHour * 60 + _activeQuest.current.window.endMinute;
                if (now >= deadline)
                {
                    MissCurrent();
                }
            }
        }

        void CheckActivate()
        {
            if (_activeQuest.current != null) return;
            if (_pendingToday.Count == 0) return;

            var next = _pendingToday.Peek();
            int start = QuestStartMinutes(next);
            int now = _clock.hour * 60 + _clock.minute;

            if (now >= start)
            {
                _pendingToday.Dequeue();
                _activeQuest.current = next;
                _activeQuest.runtimeInstance = next.CreateRuntime(new QuestContext
                {
                    awardScoreDelta = d => _scoreSystem?.ApplyDelta(0, d, next.id),
                    markComplete = s => Complete(next, s)
                });
                _activeQuest.runtimeInstance?.Begin();
                BroadcastService.Send(new QuestActivatedMsg(next));
            }
        }

void MissCurrent()
        {
            var q = _activeQuest.current;
            if (q == null) return;
            // Funnel through Complete() to keep day-progress + score handling in one place.
            // Complete handles the missedToday list and the runtime callback both.
            Complete(q, false);
            BroadcastService.Send(new QuestMissedMsg(q));
            CheckActivate();
        }

public void Complete(QuestSO quest, bool success)
        {
            if (quest == null || _activeQuest.current != quest) return;
            if (success)
            {
                if (!_dayProgress.completedToday.Contains(quest.id))
                    _dayProgress.completedToday.Add(quest.id);
            }
            else
            {
                if (!_dayProgress.missedToday.Contains(quest.id))
                    _dayProgress.missedToday.Add(quest.id);
            }
            _activeQuest.runtimeInstance?.OnComplete(success);
            BroadcastService.Send(new QuestCompletedMsg(quest, success, 0));
            _activeQuest.current = null;
            _activeQuest.runtimeInstance = null;
            CheckActivate();
        }

        public bool IsInteractableAllowed(InteractableSO interactable)
        {
            if (interactable == null) return false;
            if (_activeQuest.current == null) return true;
            return interactable.area == _activeQuest.current.area;
        }

        public string GetTodaySummary()
        {
            if (_currentDay == null || _currentDay.quests == null || _currentDay.quests.Count == 0)
                return "khong co lich";
            var titles = _currentDay.quests.Where(q => q != null && !string.IsNullOrEmpty(q.title)).Select(q => q.title);
            return string.Join(", ", titles);
        }
    }
}

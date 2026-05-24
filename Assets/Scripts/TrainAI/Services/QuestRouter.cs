using System;
using System.Collections.Generic;
using System.Linq;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;
using UnityEngine;

namespace TrainAI.Services
{
    public class QuestRouter : IQuestRouter, IDisposable
    {
        readonly DayDB _dayDB;
        readonly ActiveQuestRSO _activeQuest;
        readonly GameClockRSO _clock;
        readonly DayProgressRSO _dayProgress;
        readonly IScoreSystem _scoreSystem;
        // Clock service injected so we can auto-advance the in-game clock to
        // the next quest's start when the player completes the current one
        // early. Without this, a player who finishes "An sang" at 07:05 has
        // to idle wait until 07:30 game-time for the next quest — which is
        // why the chain felt broken.
        readonly IGameClock _clockService;

        DaySO _currentDay;
        readonly Queue<QuestSO> _pendingToday = new();

        public QuestRouter(DayDB dayDB, ActiveQuestRSO activeQuest, GameClockRSO clock,
                           DayProgressRSO dayProgress, IScoreSystem scoreSystem,
                           IGameClock clockService = null)
        {
            _dayDB = dayDB;
            _activeQuest = activeQuest;
            _clock = clock;
            _dayProgress = dayProgress;
            _scoreSystem = scoreSystem;
            _clockService = clockService;
            BroadcastService.Subscribe<DayStartedMsg>(OnDayStarted);
        }

        // Unsubscribe so the static BroadcastService doesn't accumulate stale
        // handlers across play-mode restarts (each restart was creating a fresh
        // service but leaving the old subscriber registered, doubling message
        // dispatch each session — a memory + correctness leak).
        public void Dispose() => BroadcastService.Unsubscribe<DayStartedMsg>(OnDayStarted);

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
            // Clear any quest still 'active' from the previous day before
            // running CheckActivate. Without this, AdvanceDay's broadcast
            // path (StartDay called with the clock still at last night's
            // 18:30+) would activate Day-N's first quest immediately, and the
            // next time the clock advanced past its 5:15 deadline (e.g. the
            // very next Tick after SkipTo(5,0) on day-start logic) the quest
            // would be marked missed despite never being run. Clearing here
            // guarantees CheckActivate below picks the quest fresh against
            // the current (post-SkipTo) clock.
            _activeQuest.current = null;
            _activeQuest.runtimeInstance = null;
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
                    // Free-roam slots (Nghi trua, Tu do) are intentionally
                    // non-tasks per GDD — "tự do đi lại trong trại, không phải
                    // nhiệm vụ". Marking them as MISS when the window ends
                    // dirties missedToday + fires QuestMissedMsg (which flicks
                    // the arrow off mid-roam) for what should be a quiet
                    // transition. Treat the window-end of a FreeRoam quest as
                    // graceful success so the chain advances cleanly into the
                    // next real quest (Hoc chieu / Di ngu).
                    if (_activeQuest.current is FreeRoamQuestSO)
                        Complete(_activeQuest.current, true);
                    else
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

            // Auto-advance clock to the next quest's start when nothing is
            // active and the next pending quest is still in the future. This
            // makes quest chains feel continuous — finish "An sang" at 07:05
            // and "Hoc LichSu" (07:30) fires immediately instead of forcing
            // the player to idle for 25 in-game minutes. Outside quest
            // windows there is nothing meaningful to do, so the skip is the
            // intended UX (and matches what Playtest30DaySim simulates).
            if (now < start && _clockService != null)
            {
                _clockService.SkipTo(next.window.startHour, next.window.startMinute);
                now = start;
            }

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
            // Broadcast QuestMissedMsg BEFORE Complete() runs CheckActivate().
            // Order matters: Complete() may immediately activate the next quest
            // (auto-advance clock pattern) and fire QuestActivatedMsg. If we
            // broadcast QuestMissedMsg AFTER that, the UI's OnEnded handler
            // (which uses a generic <T> signature, doesn't compare quest
            // identity) would re-hide the freshly-shown next quest. Sending
            // Missed first means: hide old → Complete hides via Completed →
            // CheckActivate activates next → HUD shows. Final state visible.
            BroadcastService.Send(new QuestMissedMsg(q));
            // Funnel through Complete() to keep day-progress + score handling in one place.
            // Complete handles the missedToday list and the runtime callback both.
            // It also calls CheckActivate internally, so we don't call it again here.
            Complete(q, false);
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
            // CheckActivate now self-skips the clock when the next pending
            // quest is in the future, so chains stay continuous without a
            // separate auto-advance step here.
            CheckActivate();
        }

        public bool IsInteractableAllowed(InteractableSO interactable)
        {
            if (interactable == null) return false;
            // In-sub-scene interactables (eg dining table inside NhaAn) opt
            // out of the area-match gate so the player can always finish or
            // leave once they've entered the building — even if AutoChain
            // advanced the clock to a different quest while inside.
            if (interactable.bypassQuestGate) return true;
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

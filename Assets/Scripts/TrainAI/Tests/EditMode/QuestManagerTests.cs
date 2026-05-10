using System;
using NUnit.Framework;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using TrainAI.Core.Time;
using TrainAI.Systems.Quest;
using TrainAI.Systems.Score;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class QuestManagerTests
    {
        private TimeConfigSO _timeCfg;
        private ScoreConfigSO _scoreCfg;
        private DayCycleConfigSO _dayCycle;
        private DayPlanSO _day1;
        private QuestDefSO _qExercise;
        private QuestDefSO _qStudy;
        private TimeManager _time;
        private ScoreManager _score;
        private QuestManager _quest;

        [SetUp]
        public void Setup()
        {
            _timeCfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            _timeCfg.secondsPerGameHour = 60f;
            _timeCfg.lateAfterMinutes = 15;
            _timeCfg.firstDay = 1;
            _timeCfg.skipWeekend = false;
            _timeCfg.weekStart = DayOfWeek.Monday;

            _scoreCfg = ScriptableObject.CreateInstance<ScoreConfigSO>();
            _scoreCfg.startingDiscipline = 100;
            _scoreCfg.maxDiscipline = 100;

            _qExercise = ScriptableObject.CreateInstance<QuestDefSO>();
            _qExercise.id = "Q_Exercise_0500";
            _qExercise.type = QuestType.Exercise;
            _qExercise.startHour = 5;
            _qExercise.startMinute = 0;
            _qExercise.deadlineHour = 5;
            _qExercise.deadlineMinute = 15;
            _qExercise.skipToHour = 6;
            _qExercise.skipToMinute = 0;
            _qExercise.penaltyOnLate = 5;
            _qExercise.penaltyOnMissed = 5;

            _qStudy = ScriptableObject.CreateInstance<QuestDefSO>();
            _qStudy.id = "Q_Study_0730";
            _qStudy.type = QuestType.StudyMorning;
            _qStudy.startHour = 7;
            _qStudy.startMinute = 30;
            _qStudy.deadlineHour = 7;
            _qStudy.deadlineMinute = 45;
            _qStudy.skipToHour = 11;
            _qStudy.skipToMinute = 30;

            _day1 = ScriptableObject.CreateInstance<DayPlanSO>();
            _day1.dayNumber = 1;
            _day1.quests = new System.Collections.Generic.List<QuestDefSO> { _qExercise, _qStudy };

            _dayCycle = ScriptableObject.CreateInstance<DayCycleConfigSO>();
            _dayCycle.dayPlans = new System.Collections.Generic.List<DayPlanSO> { _day1 };

            _time = new TimeManager(_timeCfg);
            _score = new ScoreManager(_scoreCfg);
            // QuestManager goi PenalizeDiscipline qua GameServices.Score.
            GameServices.Reset();
            GameServices.Score = _score;
            _quest = new QuestManager(_dayCycle, _time);
            _quest.StartDay(1);
            GameEvents.ClearAll();
        }

        [TearDown]
        public void Teardown()
        {
            _quest.Dispose();
            ScriptableObject.DestroyImmediate(_qExercise);
            ScriptableObject.DestroyImmediate(_qStudy);
            ScriptableObject.DestroyImmediate(_day1);
            ScriptableObject.DestroyImmediate(_dayCycle);
            ScriptableObject.DestroyImmediate(_timeCfg);
            ScriptableObject.DestroyImmediate(_scoreCfg);
            GameServices.Reset();
            GameEvents.ClearAll();
        }

        [Test]
        public void StartDay_LoadsFirstQuest()
        {
            Assert.That(_quest.CurrentQuest, Is.SameAs(_qExercise));
            Assert.That(_quest.CurrentQuestIndex, Is.EqualTo(0));
        }

        [Test]
        public void StartCurrentQuest_SetsActive()
        {
            _quest.StartCurrentQuest();
            Assert.That(_quest.CurrentState.status, Is.EqualTo(QuestStatus.Active));
        }

        [Test]
        public void CompleteCurrentQuest_AdvancesIndex_AndAwardsScore()
        {
            _quest.StartCurrentQuest();
            _quest.CompleteCurrentQuest(7);
            Assert.That(_quest.CurrentQuestIndex, Is.EqualTo(1));
            Assert.That(_quest.CurrentQuest, Is.SameAs(_qStudy));
            Assert.That(_score.Academic, Is.EqualTo(7));
        }

        [Test]
        public void CompleteCurrentQuest_JumpsTime()
        {
            _quest.StartCurrentQuest();
            _quest.CompleteCurrentQuest(0);
            Assert.That(_time.Now.hour, Is.EqualTo(_qExercise.skipToHour));
            Assert.That(_time.Now.minute, Is.EqualTo(_qExercise.skipToMinute));
        }

        [Test]
        public void OnTick_Late_FiresAfterLateThreshold()
        {
            // Quest deadline 5:15, lateAfter = 15p -> Late fires luc 5:30+
            _time.SetTime(5, 35);
            int prevDiscipline = _score.Discipline;

            // Simulate manager goi OnTick.
            _quest.OnTick(_time.Now);

            Assert.That(_quest.CurrentState.status, Is.EqualTo(QuestStatus.Late));
            Assert.That(_score.Discipline, Is.EqualTo(prevDiscipline - 5));
        }

        [Test]
        public void Late_AppliesPenaltyOnce_NotEveryTick()
        {
            _time.SetTime(5, 35);
            _quest.OnTick(_time.Now);
            int after1 = _score.Discipline;

            _quest.OnTick(_time.Now);
            _quest.OnTick(_time.Now);

            Assert.That(_score.Discipline, Is.EqualTo(after1)); // khong tru them
        }

        [Test]
        public void IsLastQuestCompletedToday_TrueAfterAllQuests()
        {
            _quest.StartCurrentQuest();
            _quest.CompleteCurrentQuest(0);
            _quest.StartCurrentQuest();
            _quest.CompleteCurrentQuest(0);
            Assert.That(_quest.IsLastQuestCompletedToday(), Is.True);
        }
    }
}

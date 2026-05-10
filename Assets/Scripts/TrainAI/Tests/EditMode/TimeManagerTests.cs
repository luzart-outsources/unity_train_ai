using System;
using NUnit.Framework;
using TrainAI.Configs;
using TrainAI.Core.Time;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class TimeManagerTests
    {
        private TimeConfigSO _cfg;
        private TimeManager _tm;

        [SetUp]
        public void Setup()
        {
            _cfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            _cfg.secondsPerGameHour = 60f;          // 1s = 1 phut game cho test gon
            _cfg.tickEveryGameMinutes = 1;
            _cfg.firstDay = 1;
            _cfg.totalDays = 30;
            _cfg.skipWeekend = true;
            _cfg.weekStart = DayOfWeek.Monday;
            _cfg.dayStartOffsetMinutesBeforeFirstQuest = 30;
            _cfg.lateAfterMinutes = 15;
            _cfg.defaultDayStartHour = 5;
            _cfg.defaultDayStartMinute = 0;
            _tm = new TimeManager(_cfg);
        }

        [TearDown]
        public void Teardown()
        {
            ScriptableObject.DestroyImmediate(_cfg);
        }

        [Test]
        public void Tick_AdvancesMinute_AfterSecondsPerMinute()
        {
            // 60s/h => 1s = 1 minute game.
            int prevMin = _tm.Now.minute;
            _tm.Tick(1.0f);
            Assert.That(_tm.Now.minute, Is.EqualTo(prevMin + 1));
        }

        [Test]
        public void Tick_DoesNotAdvance_WhenFrozen()
        {
            _tm.Freeze();
            int prevMin = _tm.Now.minute;
            _tm.Tick(5f);
            Assert.That(_tm.Now.minute, Is.EqualTo(prevMin));
        }

        [Test]
        public void Tick_Resumes_AfterUnfreeze()
        {
            _tm.Freeze();
            _tm.Tick(5f);
            _tm.Resume();
            int prevMin = _tm.Now.minute;
            _tm.Tick(2f);
            Assert.That(_tm.Now.minute, Is.EqualTo(prevMin + 2));
        }

        [Test]
        public void Tick_Wraps_HourTo24Resets_AndAdvancesDay()
        {
            _tm.SetTime(23, 59);
            int prevDay = _tm.Now.day;
            _tm.Tick(1.0f); // +1 phut
            Assert.That(_tm.Now.hour, Is.EqualTo(0));
            Assert.That(_tm.Now.minute, Is.EqualTo(0));
            Assert.That(_tm.Now.day, Is.EqualTo(prevDay + 1));
        }

        [Test]
        public void NextDay_SkipsWeekend_FromSaturdayToMonday()
        {
            // Setup: ngay 6 = Saturday
            // Cach: gan day=5, weekday=Friday, goi NextDay() -> day=6 = Sat -> skip 2 -> day=8 = Mon
            // Hoac don gian: tang den khi day = 6 + ay la T7
            // Test: tu Friday, NextDay() -> Saturday detected -> skip 2 days -> Monday
            // Set: weekday=Friday
            // Voi cau truc internal AdvanceDayInternal: day++, weekday next; neu Sat -> day+=2, weekday=Mon
            // Khoi dau: day=1 Monday. Tien tu Mon -> Tue -> Wed -> Thu -> Fri (4 NextDay).
            for (int i = 0; i < 4; i++) _tm.NextDay();
            Assert.That(_tm.Now.weekday, Is.EqualTo(DayOfWeek.Friday));

            int dayBefore = _tm.Now.day; // = 5
            _tm.NextDay(); // Fri -> Sat detected -> skip to Mon, day +=2
            Assert.That(_tm.Now.weekday, Is.EqualTo(DayOfWeek.Monday));
            Assert.That(_tm.Now.day, Is.EqualTo(dayBefore + 3));
        }

        [Test]
        public void NextDay_DoesNotSkip_WhenSkipWeekendFalse()
        {
            _cfg.skipWeekend = false;
            for (int i = 0; i < 4; i++) _tm.NextDay();
            int dayBefore = _tm.Now.day;
            _tm.NextDay();
            Assert.That(_tm.Now.weekday, Is.EqualTo(DayOfWeek.Saturday));
            Assert.That(_tm.Now.day, Is.EqualTo(dayBefore + 1));
        }

        [Test]
        public void JumpTo_AdvancesToTargetTime()
        {
            _tm.SetTime(5, 0);
            _tm.JumpTo(6, 30);
            Assert.That(_tm.Now.hour, Is.EqualTo(6));
            Assert.That(_tm.Now.minute, Is.EqualTo(30));
        }

        [Test]
        public void JumpTo_Wraps_PastMidnight()
        {
            _tm.SetTime(23, 0);
            int prevDay = _tm.Now.day;
            _tm.JumpTo(1, 0); // jump 2h forward across midnight
            Assert.That(_tm.Now.hour, Is.EqualTo(1));
            Assert.That(_tm.Now.day, Is.EqualTo(prevDay + 1));
        }

        [Test]
        public void OnTick_FiresEvent()
        {
            int count = 0;
            _tm.OnTick += _ => count++;
            _tm.Tick(2.0f); // 2 phut
            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void OnNewDay_Fires_OnceWhenAdvanceDay()
        {
            int dayCount = 0;
            int newDayValue = 0;
            _tm.OnNewDay += d => { dayCount++; newDayValue = d; };
            _tm.NextDay();
            Assert.That(dayCount, Is.EqualTo(1));
            Assert.That(newDayValue, Is.EqualTo(2));
        }

        [Test]
        public void NextDay_WithReset_SetsTime()
        {
            _tm.NextDay(7, 30);
            Assert.That(_tm.Now.hour, Is.EqualTo(7));
            Assert.That(_tm.Now.minute, Is.EqualTo(30));
        }
    }
}

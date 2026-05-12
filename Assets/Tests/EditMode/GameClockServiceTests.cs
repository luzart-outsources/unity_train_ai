using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;
using TrainAI.SO.Base;
using TrainAI.Services;

namespace TrainAI.EditMode.Tests
{
    public class GameClockServiceTests
    {
        [Test]
        public void Tick_3RealMin_AdvancesGame1Hour()
        {
            var cfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            cfg.gameHourPerRealMinute = 1f / 3f;
            var state = ScriptableObject.CreateInstance<GameClockRSO>();
            var gc = ScriptableObject.CreateInstance<GameConfigSO>();
            int startHour = state.hour;
            int startMinute = state.minute;
            var svc = new GameClockService(cfg, state, gc);

            for (int i = 0; i < 180; i++) svc.Tick(1f);

            int startTotal = startHour * 60 + startMinute;
            int nowTotal = state.hour * 60 + state.minute;
            Assert.AreEqual(60, nowTotal - startTotal, $"after 180s, expected +60 min, got {nowTotal - startTotal}");

            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(state);
            Object.DestroyImmediate(gc);
        }

        [Test]
        public void SkipTo_SetsHourMinute()
        {
            var cfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            var state = ScriptableObject.CreateInstance<GameClockRSO>();
            var gc = ScriptableObject.CreateInstance<GameConfigSO>();
            var svc = new GameClockService(cfg, state, gc);

            svc.SkipTo(14, 30);
            Assert.AreEqual(14, state.hour);
            Assert.AreEqual(30, state.minute);

            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(state);
            Object.DestroyImmediate(gc);
        }

        [Test]
        public void Freeze_StopsTick()
        {
            var cfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            cfg.gameHourPerRealMinute = 1f;
            var state = ScriptableObject.CreateInstance<GameClockRSO>();
            var gc = ScriptableObject.CreateInstance<GameConfigSO>();
            var svc = new GameClockService(cfg, state, gc);

            int h0 = state.hour, m0 = state.minute;
            svc.Freeze();
            for (int i = 0; i < 10; i++) svc.Tick(60f);
            Assert.AreEqual(h0, state.hour);
            Assert.AreEqual(m0, state.minute);

            Object.DestroyImmediate(cfg);
            Object.DestroyImmediate(state);
            Object.DestroyImmediate(gc);
        }
    }
}

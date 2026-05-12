using System.IO;
using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;
using TrainAI.SO.Base;
using TrainAI.Services;

namespace TrainAI.EditMode.Tests
{
    public class SaveServiceTests
    {
        string _tempDir;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "trainai_savetest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void Teardown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        [Test]
        public void SaveLoad_RoundTrip_RestoresState()
        {
            var player = ScriptableObject.CreateInstance<PlayerStateRSO>();
            var clock = ScriptableObject.CreateInstance<GameClockRSO>();
            var dp = ScriptableObject.CreateInstance<DayProgressRSO>();

            player.playerName = "TestPlayer";
            player.hocTap = 234;
            player.renLuyen = 78;
            player.currentScene = "10_World";
            player.lastWorldPos = new Vector3(1.5f, 0f, 2.5f);
            clock.day = 7;
            clock.hour = 14;
            clock.minute = 30;
            clock.weekday = Weekday.Wednesday;

            var svc = new SaveService(player, clock, dp, _tempDir);
            Assert.IsTrue(svc.Save(slot: 1));

            player.playerName = "DIRTY";
            player.hocTap = 0;
            clock.day = 0;

            Assert.IsTrue(svc.TryLoad(1, out var dto));
            Assert.NotNull(dto);

            Assert.AreEqual("TestPlayer", player.playerName);
            Assert.AreEqual(234, player.hocTap);
            Assert.AreEqual(78, player.renLuyen);
            Assert.AreEqual(7, clock.day);
            Assert.AreEqual(14, clock.hour);
            Assert.AreEqual(Weekday.Wednesday, clock.weekday);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(clock);
            Object.DestroyImmediate(dp);
        }

        [Test]
        public void TryLoad_NoFile_ReturnsFalse()
        {
            var player = ScriptableObject.CreateInstance<PlayerStateRSO>();
            var clock = ScriptableObject.CreateInstance<GameClockRSO>();
            var dp = ScriptableObject.CreateInstance<DayProgressRSO>();
            var svc = new SaveService(player, clock, dp, _tempDir);

            Assert.IsFalse(svc.TryLoad(slot: 99, out var dto));
            Assert.IsNull(dto);

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(clock);
            Object.DestroyImmediate(dp);
        }
    }
}

using System.IO;
using NUnit.Framework;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Time;
using TrainAI.Systems.Save;
using TrainAI.Systems.Score;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class SaveManagerTests
    {
        private SaveManager _save;
        private TimeConfigSO _timeCfg;
        private ScoreConfigSO _scoreCfg;

        [SetUp]
        public void Setup()
        {
            _timeCfg = ScriptableObject.CreateInstance<TimeConfigSO>();
            _scoreCfg = ScriptableObject.CreateInstance<ScoreConfigSO>();
            _scoreCfg.startingDiscipline = 100;
            _scoreCfg.maxDiscipline = 100;
            _scoreCfg.maxAcademic = 480;

            GameServices.Reset();
            GameServices.Time = new TimeManager(_timeCfg);
            GameServices.Score = new ScoreManager(_scoreCfg);
            GameServices.Player = new PlayerData("Test");
            _save = new SaveManager();
            // Cleanup tu lan truoc neu co.
            if (File.Exists(_save.SavePath)) File.Delete(_save.SavePath);
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_save.SavePath)) File.Delete(_save.SavePath);
            if (File.Exists(_save.SaveBackup)) File.Delete(_save.SaveBackup);
            ScriptableObject.DestroyImmediate(_timeCfg);
            ScriptableObject.DestroyImmediate(_scoreCfg);
            GameServices.Reset();
        }

        [Test]
        public void Save_WritesJsonFile()
        {
            _save.Save();
            Assert.That(File.Exists(_save.SavePath), Is.True);
            string json = File.ReadAllText(_save.SavePath);
            Assert.That(json, Does.Contain("\"playerName\""));
            Assert.That(json, Does.Contain("Test"));
        }

        [Test]
        public void TryLoad_RestoresFromFile()
        {
            GameServices.Time.SetTime(8, 30);
            GameServices.Score.AddAcademic(40);
            GameServices.Player.Name = "Quyen";
            _save.Save();

            // Reset state.
            GameServices.Time = new TimeManager(_timeCfg);
            GameServices.Score = new ScoreManager(_scoreCfg);
            GameServices.Player = new PlayerData("");

            bool ok = _save.TryLoad(out var data);
            Assert.That(ok, Is.True);
            Assert.That(data.hour, Is.EqualTo(8));
            Assert.That(data.minute, Is.EqualTo(30));
            Assert.That(data.academic, Is.EqualTo(40));
            Assert.That(GameServices.Player.Name, Is.EqualTo("Quyen"));
        }

        [Test]
        public void TryLoad_ReturnsFalse_WhenNoFile()
        {
            if (File.Exists(_save.SavePath)) File.Delete(_save.SavePath);
            bool ok = _save.TryLoad(out var data);
            Assert.That(ok, Is.False);
        }
    }
}

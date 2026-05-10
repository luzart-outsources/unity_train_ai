using NUnit.Framework;
using TrainAI.Configs;
using TrainAI.Core.Events;
using TrainAI.Systems.Score;
using UnityEngine;

namespace TrainAI.Tests.EditMode
{
    public class ScoreManagerTests
    {
        private ScoreConfigSO _cfg;
        private ScoreManager _sm;

        [SetUp]
        public void Setup()
        {
            _cfg = ScriptableObject.CreateInstance<ScoreConfigSO>();
            _cfg.startingDiscipline = 100;
            _cfg.startingAcademic = 0;
            _cfg.maxDiscipline = 100;
            _cfg.maxAcademic = 480;
            _cfg.pointsPerQuizQuestion = 1f;
            _cfg.penaltyLate = 5;
            _cfg.penaltyMissed = 5;
            _cfg.kickOutThreshold = 0;
            _cfg.disciplineGrades = new System.Collections.Generic.List<GradeThreshold>
            {
                new GradeThreshold { label = "Xuat sac", minScoreInclusive = 90, maxScoreInclusive = 100 },
                new GradeThreshold { label = "Tot",      minScoreInclusive = 60, maxScoreInclusive = 89 },
                new GradeThreshold { label = "TB",       minScoreInclusive = 0,  maxScoreInclusive = 59 },
            };
            _cfg.academicGrades = new System.Collections.Generic.List<GradeThreshold>
            {
                new GradeThreshold { label = "Xuat sac", minScoreInclusive = 432, maxScoreInclusive = 480 },
                new GradeThreshold { label = "Tot",      minScoreInclusive = 288, maxScoreInclusive = 431 },
                new GradeThreshold { label = "TB",       minScoreInclusive = 0,   maxScoreInclusive = 287 },
            };
            _sm = new ScoreManager(_cfg);
            GameEvents.ClearAll(); // reset events giua test
        }

        [TearDown]
        public void Teardown()
        {
            ScriptableObject.DestroyImmediate(_cfg);
            GameEvents.ClearAll();
        }

        [Test]
        public void Init_HasStartingValues()
        {
            Assert.That(_sm.Discipline, Is.EqualTo(100));
            Assert.That(_sm.Academic, Is.EqualTo(0));
        }

        [Test]
        public void PenalizeDiscipline_Subtracts()
        {
            _sm.PenalizeDiscipline(5);
            Assert.That(_sm.Discipline, Is.EqualTo(95));
        }

        [Test]
        public void PenalizeDiscipline_ClampsAtZero()
        {
            _sm.PenalizeDiscipline(150);
            Assert.That(_sm.Discipline, Is.EqualTo(0));
        }

        [Test]
        public void AddAcademic_Adds()
        {
            _sm.AddAcademic(7);
            Assert.That(_sm.Academic, Is.EqualTo(7));
        }

        [Test]
        public void AddAcademic_ClampsAtMax()
        {
            _sm.AddAcademic(500);
            Assert.That(_sm.Academic, Is.EqualTo(480));
        }

        [Test]
        public void DisciplineEvent_FiresWithCorrectDelta()
        {
            int delta = 0;
            int newVal = 0;
            GameEvents.DisciplineChanged += (v, d) => { newVal = v; delta = d; };
            _sm.PenalizeDiscipline(5);
            Assert.That(delta, Is.EqualTo(-5));
            Assert.That(newVal, Is.EqualTo(95));
        }

        [Test]
        public void KickedOut_FiresAtZero()
        {
            bool fired = false;
            GameEvents.KickedOut += () => fired = true;
            _sm.PenalizeDiscipline(100);
            Assert.That(fired, Is.True);
        }

        [Test]
        public void GradeFor_AcademicExcellent()
        {
            _sm.AddAcademic(440);
            Assert.That(_sm.AcademicGrade(), Is.EqualTo("Xuat sac"));
        }

        [Test]
        public void GradeFor_AcademicGood()
        {
            _sm.AddAcademic(300);
            Assert.That(_sm.AcademicGrade(), Is.EqualTo("Tot"));
        }

        [Test]
        public void GradeFor_AcademicTB()
        {
            _sm.AddAcademic(100);
            Assert.That(_sm.AcademicGrade(), Is.EqualTo("TB"));
        }

        [Test]
        public void GradeFor_DisciplineDecreasesGrade()
        {
            _sm.PenalizeDiscipline(50); // 100 - 50 = 50 -> TB
            Assert.That(_sm.DisciplineGrade(), Is.EqualTo("TB"));
        }
    }
}

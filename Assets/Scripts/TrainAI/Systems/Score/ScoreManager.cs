using TrainAI.Configs;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.Systems.Score
{
    public class ScoreManager
    {
        private readonly ScoreConfigSO _cfg;

        public int Discipline { get; private set; }
        public int Academic { get; private set; }
        public ScoreConfigSO Config => _cfg;

        public ScoreManager(ScoreConfigSO cfg)
        {
            _cfg = cfg;
            ResetToStart();
        }

        public void ResetToStart()
        {
            if (_cfg == null) { Discipline = 100; Academic = 0; return; }
            Discipline = _cfg.startingDiscipline;
            Academic = _cfg.startingAcademic;
        }

        public void PenalizeDiscipline(int amount)
        {
            if (amount <= 0) return;
            int prev = Discipline;
            int max = _cfg != null ? _cfg.maxDiscipline : 100;
            Discipline -= amount;
            if (Discipline < 0) Discipline = 0;
            if (Discipline > max) Discipline = max;
            int delta = Discipline - prev;
            GameEvents.RaiseDisciplineChanged(Discipline, delta);

            int kickThr = _cfg != null ? _cfg.kickOutThreshold : 0;
            if (Discipline <= kickThr)
                GameEvents.RaiseKickedOut();
        }

        public void RewardDiscipline(int amount)
        {
            if (amount <= 0) return;
            int prev = Discipline;
            int max = _cfg != null ? _cfg.maxDiscipline : 100;
            Discipline += amount;
            if (Discipline > max) Discipline = max;
            int delta = Discipline - prev;
            if (delta != 0) GameEvents.RaiseDisciplineChanged(Discipline, delta);
        }

        public void AddAcademic(int amount)
        {
            if (amount == 0) return;
            int prev = Academic;
            int max = _cfg != null ? _cfg.maxAcademic : 480;
            Academic += amount;
            if (Academic < 0) Academic = 0;
            if (Academic > max) Academic = max;
            int delta = Academic - prev;
            GameEvents.RaiseAcademicChanged(Academic, delta);
        }

        public string DisciplineGrade()
        {
            if (_cfg == null) return "-";
            return _cfg.GradeFor(Discipline, _cfg.disciplineGrades);
        }

        public string AcademicGrade()
        {
            if (_cfg == null) return "-";
            return _cfg.GradeFor(Academic, _cfg.academicGrades);
        }

        public Color DisciplineColor()
        {
            if (_cfg == null) return Color.white;
            return _cfg.GradeColor(Discipline, _cfg.disciplineGrades);
        }

        public Color AcademicColor()
        {
            if (_cfg == null) return Color.white;
            return _cfg.GradeColor(Academic, _cfg.academicGrades);
        }
    }
}

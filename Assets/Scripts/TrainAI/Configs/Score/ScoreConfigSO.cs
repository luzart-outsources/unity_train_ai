using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Score/Score Config", fileName = "ScoreConfig")]
    public class ScoreConfigSO : ScriptableObject
    {
        [Header("Starting values")]
        [InfoBox("GDD: ban dau co 100 diem ren luyen va 0 diem hoc tap.")]
        [Slider(0, 100)] public int startingDiscipline = 100;
        [Slider(0, 480)] public int startingAcademic = 0;

        [Header("Maximum (theo GDD)")]
        [InfoBox("GDD: 100d ren luyen toi da, 480d hoc tap toi da.")]
        [ReadOnly] public int maxDiscipline = 100;
        [ReadOnly] public int maxAcademic = 480;

        [Header("Quiz scoring")]
        [InfoBox("GDD: hoan thanh 66% bai hoc -> ~7d, 92% -> ~9d. = correctCount * pointsPerQuiz, lam tron.")]
        [Slider(0.5f, 5f)] public float pointsPerQuizQuestion = 1f;

        [Header("Discipline penalty (GDD)")]
        [InfoBox("GDD: di tre / khong hoan thanh 1 nhiem vu -> -5d.", InfoBoxType.Warning)]
        [Slider(0, 20)] public int penaltyLate = 5;
        [Slider(0, 20)] public int penaltyMissed = 5;

        [Header("Game-over (GDD)")]
        [InfoBox("Discipline <= threshold -> hien KickedOut popup. GDD: het 100d ren luyen.", InfoBoxType.Warning)]
        [Slider(0, 50)] public int kickOutThreshold = 0;

        [Header("Grade thresholds (% of max) - GDD specs")]
        [InfoBox("GDD: hoc tap >=432/480 = XS, 288-431 = Tot, <288 = TB.\n" +
                 "Ren luyen >=90/100 = XS, 60-90 = Tot, <60 = TB.")]
        public List<GradeThreshold> disciplineGrades = new List<GradeThreshold>();
        public List<GradeThreshold> academicGrades = new List<GradeThreshold>();

        [Button("Apply GDD Defaults")]
        private void ApplyGDDDefaults()
        {
            disciplineGrades = new List<GradeThreshold>
            {
                new GradeThreshold { label = "Xuat sac", minScoreInclusive = 90,  maxScoreInclusive = 100, uiColor = new Color(0.2f, 0.8f, 0.2f) },
                new GradeThreshold { label = "Tot",      minScoreInclusive = 60,  maxScoreInclusive = 89,  uiColor = new Color(0.2f, 0.6f, 1f)   },
                new GradeThreshold { label = "Trung binh", minScoreInclusive = 0, maxScoreInclusive = 59,  uiColor = new Color(0.9f, 0.6f, 0.2f) },
            };
            academicGrades = new List<GradeThreshold>
            {
                new GradeThreshold { label = "Xuat sac", minScoreInclusive = 432, maxScoreInclusive = 480, uiColor = new Color(0.2f, 0.8f, 0.2f) },
                new GradeThreshold { label = "Tot",      minScoreInclusive = 288, maxScoreInclusive = 431, uiColor = new Color(0.2f, 0.6f, 1f)   },
                new GradeThreshold { label = "Trung binh", minScoreInclusive = 0, maxScoreInclusive = 287, uiColor = new Color(0.9f, 0.6f, 0.2f) },
            };
        }

        public string GradeFor(int score, List<GradeThreshold> table)
        {
            if (table == null) return "-";
            for (int i = 0; i < table.Count; i++)
            {
                var t = table[i];
                if (score >= t.minScoreInclusive && score <= t.maxScoreInclusive)
                    return t.label;
            }
            return "-";
        }

        public Color GradeColor(int score, List<GradeThreshold> table)
        {
            if (table == null) return Color.white;
            for (int i = 0; i < table.Count; i++)
            {
                var t = table[i];
                if (score >= t.minScoreInclusive && score <= t.maxScoreInclusive)
                    return t.uiColor;
            }
            return Color.white;
        }
    }
}

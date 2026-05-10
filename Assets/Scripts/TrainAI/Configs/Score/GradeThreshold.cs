using System;
using UnityEngine;

namespace TrainAI.Configs
{
    // 1 nguong xep hang. List trong ScoreConfigSO theo thu tu giam dan diem.
    [Serializable]
    public class GradeThreshold
    {
        public string label = "Xuat sac";       // GDD: Xuat sac / Tot / Trung binh
        public int minScoreInclusive = 432;     // Vd academic: 432/480 = XS
        public int maxScoreInclusive = 480;
        public Color uiColor = Color.green;
    }
}

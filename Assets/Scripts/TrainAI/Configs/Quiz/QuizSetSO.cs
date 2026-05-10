using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // 1 bo de = 1 buoi hoc trong GDD. GDD: 10 cau / buoi.
    [CreateAssetMenu(menuName = "TrainAI/Quiz/Quiz Set", fileName = "QS_QuizSet")]
    public class QuizSetSO : ScriptableObject
    {
        public string id = "QS_LichSu_Bai01";
        public string title = "Lich Su Viet Nam - Buoi 1";

        [InfoBox("GDD: 10 cau hoi / buoi hoc.")]
        public List<QuestionSO> questions = new List<QuestionSO>();

        [InfoBox("GDD: 15s / cau. Khong chon = bo qua = sai.")]
        [Slider(5f, 60f)] public float secondsPerQuestion = 15f;

        public int Count => questions != null ? questions.Count : 0;
    }
}

using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Quiz/Question", fileName = "Q_Question")]
    public class QuestionSO : ScriptableObject
    {
        [TextArea(2, 4)] public string stem = "";

        [InfoBox("4 dap an. Phai dien du 4. Index 0..3.")]
        public string[] options = new string[4];

        [Slider(0, 3)] public int correctIndex = 0;

        [TextArea(2, 4)]
        [InfoBox("Optional. Hien sau khi user chon dap an.")]
        public string explanation = "";

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(stem)) return false;
            if (options == null || options.Length != 4) return false;
            for (int i = 0; i < 4; i++)
                if (string.IsNullOrEmpty(options[i])) return false;
            if (correctIndex < 0 || correctIndex > 3) return false;
            return true;
        }
    }
}

using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "Quiz_Question_New", menuName = "TrainAI/Data/Quiz Question")]
    public class QuizQuestionSO : ScriptableObject
    {
        [TextArea] public string question;
        public string[] answers = new string[4];
        public int correctIndex;
    }
}

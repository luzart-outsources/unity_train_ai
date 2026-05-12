using System.Collections.Generic;
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "QuizSet_New", menuName = "TrainAI/Data/Quiz Set")]
    public class QuizSetSO : ScriptableObject
    {
        [Required] public SubjectSO subject;
        public List<QuizQuestionSO> questions = new();
        public float perQuestionSec = 15f;
    }
}

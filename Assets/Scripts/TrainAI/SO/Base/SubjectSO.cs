using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "Subject_New", menuName = "TrainAI/Data/Subject")]
    public class SubjectSO : ScriptableObject
    {
        [Required] public string id;
        public string displayName;
        public int maxPointsPerLesson = 40;
    }
}

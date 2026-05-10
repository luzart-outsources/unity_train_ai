using System.Collections.Generic;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Quiz/Subject", fileName = "S_Subject")]
    public class SubjectSO : ScriptableObject
    {
        public string id = "S_LichSu";
        public string displayName = "Lich Su";
        public Sprite icon;

        [Tooltip("Map session index -> bo de. Co the de trong neu QuestDef tu ref QuizSet truc tiep.")]
        public List<QuizSetSO> sessions = new List<QuizSetSO>();
    }
}

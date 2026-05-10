using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/Quest/Day Plan", fileName = "Day")]
    public class DayPlanSO : ScriptableObject
    {
        [Slider(1, 30)] public int dayNumber = 1;

        public string label = "Ngay 1 - chao nhap ngu";

        [InfoBox("Quest theo thu tu se active trong ngay. GDD: 5h tap, 6h ve sinh, 7h an, 7h30 hoc, ...")]
        public List<QuestDefSO> quests = new List<QuestDefSO>();

        [Header("Optional opening")]
        [Tooltip("Cutscene mo dau ngay (vd ngay dau tien).")]
        public AudioCueId startMusic = AudioCueId.None;

        public QuestDefSO FirstQuest => quests != null && quests.Count > 0 ? quests[0] : null;
    }
}

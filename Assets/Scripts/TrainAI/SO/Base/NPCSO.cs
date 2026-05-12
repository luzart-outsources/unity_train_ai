using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "NPC_New", menuName = "TrainAI/Data/NPC")]
    public class NPCSO : ScriptableObject
    {
        [Required] public string id;
        public string displayName;
        public Sprite portrait;
        public Vector3 spawnPos;
        public MovementStrategySO movement;
        public DialogueStrategySO dialogue;
        public ScheduleSO schedule;
    }
}

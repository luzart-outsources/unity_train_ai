using System.Collections.Generic;
using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    [CreateAssetMenu(menuName = "TrainAI/NPC/Profile", fileName = "NPC_Profile")]
    public class NPCProfileSO : ScriptableObject
    {
        [Header("Identity")]
        public string id = "NPC_DaiDoiTruong";
        public string displayName = "Dai doi truong";
        public Sprite portrait;

        [Header("AI mode")]
        [InfoBox("Phase A = Sentis chat (NPC dung yen). Phase B = PPO movement (NPC tu di).")]
        public NPCAIMode aiMode = NPCAIMode.SentisChat;

        [Header("Visual prefab override (optional)")]
        [Tooltip("Neu null, dung NPC prefab default.")]
        public GameObject visualOverride;

        [Header("Dialogue (Phase A)")]
        [ShowIf("aiMode", NPCAIMode.SentisChat)]
        [TextArea(2, 4)]
        public string greetingTemplate = "Chao {playerName}, co chuyen gi vay?";

        [ShowIf("aiMode", NPCAIMode.SentisChat)]
        [InfoBox("Fallback responses neu Sentis model fail hoac chua wire.")]
        public List<string> fallbackResponses = new List<string>
        {
            "Toi dang ban, hoi sau nhe.",
            "Em ve di nghi cho khoe.",
            "Co gang hoan thanh nhiem vu nhe!",
        };

        [Header("Movement (Phase B)")]
        [ShowIf("aiMode", NPCAIMode.Movement)]
        public NPCScheduleSO schedule;

        [Slider(0.5f, 5f)] public float moveSpeed = 2f;

        [Header("Audio")]
        public AudioCueId interactSound = AudioCueId.UI_Click;
    }
}

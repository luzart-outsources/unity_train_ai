using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // 1 diem co the tuong tac trong scene - san van dong, ban hoc, NPC, cua scene...
    [CreateAssetMenu(menuName = "TrainAI/Scene/Interactable", fileName = "I_Interactable")]
    public class InteractableSO : ScriptableObject
    {
        [InfoBox("Khop voi QuestDefSO.interactableLocationKey. Vd: SanVanDong, DonVeSinh, CuaLopHoc.")]
        public string key = "SanVanDong";

        public string displayLabel = "San van dong";

        public InteractableKind kind = InteractableKind.QuestPoint;

        [Header("Door target (chi cho kind=SceneDoor)")]
        [ShowIf("kind", InteractableKind.SceneDoor)]
        public SceneRouteSO doorTarget;

        [Header("NPC profile (chi cho kind=NPC)")]
        [ShowIf("kind", InteractableKind.NPC)]
        public NPCProfileSO npcProfile;

        [Header("Visual")]
        [InfoBox("GDD: nut sang khi camera quay den, toi khi quay di.\n" +
                 "Threshold dot >= X: 0 = bat ki goc nao, 0.7 = phai gan thang huong.")]
        [Slider(-1f, 1f)] public float aimDotThreshold = 0.5f;

        [Slider(0.5f, 10f)] public float interactRadius = 2f;
    }
}

using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_OpenDialogue_New", menuName = "TrainAI/Interaction/Open Dialogue")]
    public class OpenDialogueInteractionSO : InteractionSO
    {
        public NPCSO npc;

        public override async UniTask Execute(InteractionContext ctx)
        {
            await UniTask.Yield();
            Debug.Log($"[OpenDialogue] NPC={(npc != null ? npc.id : "null")} (UIRouter wired in scene)");
        }
    }
}

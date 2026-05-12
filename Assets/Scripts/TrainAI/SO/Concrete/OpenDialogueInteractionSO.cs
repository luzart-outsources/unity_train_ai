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
            if (ctx.showDialogue == null || npc == null) return;
            await ctx.showDialogue(npc);
        }
    }
}

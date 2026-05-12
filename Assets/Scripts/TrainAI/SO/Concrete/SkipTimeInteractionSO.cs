using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_SkipTime_New", menuName = "TrainAI/Interaction/Skip Time")]
    public class SkipTimeInteractionSO : InteractionSO
    {
        public int skipToHour = 6;
        public int skipToMinute = 0;
        public bool completeQuestAfter = true;

        public override UniTask Execute(InteractionContext ctx)
        {
            ctx.skipTimeTo?.Invoke(skipToHour, skipToMinute);
            if (completeQuestAfter) ctx.completeCurrentQuest?.Invoke(true);
            return UniTask.CompletedTask;
        }
    }
}

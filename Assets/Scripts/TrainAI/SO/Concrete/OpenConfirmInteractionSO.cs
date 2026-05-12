using UnityEngine;
using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_Confirm_New", menuName = "TrainAI/Interaction/Open Confirm")]
    public class OpenConfirmInteractionSO : InteractionSO
    {
        public string confirmText = "Ban dang lam gi do.";

        [Header("On confirm OK")]
        public bool skipTime = true;
        public int skipToHour = -1;
        public int skipToMinute = 0;
        public bool completeQuestOnConfirm = true;

        public override async UniTask Execute(InteractionContext ctx)
        {
            bool ok = true;
            if (ctx.showConfirm != null) ok = await ctx.showConfirm(confirmText);
            if (!ok) return;
            if (skipTime && skipToHour >= 0) ctx.skipTimeTo?.Invoke(skipToHour, skipToMinute);
            if (completeQuestOnConfirm) ctx.completeCurrentQuest?.Invoke(true);
        }
    }
}

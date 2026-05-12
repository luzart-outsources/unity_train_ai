using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_Sleep_New", menuName = "TrainAI/Interaction/Sleep")]
    public class SleepInteractionSO : InteractionSO
    {
        public string confirmText = "Di ngu";
        public string loadingText = "Sang ngay hom sau...";
        public float loadingSeconds = 2f;

        public override async UniTask Execute(InteractionContext ctx)
        {
            bool ok = true;
            if (ctx.showConfirm != null) ok = await ctx.showConfirm(confirmText);
            if (!ok) return;
            if (ctx.showLoading != null) await ctx.showLoading(loadingText, loadingSeconds);
            ctx.completeCurrentQuest?.Invoke(true);
            ctx.advanceDay?.Invoke();
        }
    }
}

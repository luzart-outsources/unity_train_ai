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
        // If set, after the day-advance the player is sent back to this scene
        // (typically 10_World). Lets the dorm sleep interactable double as
        // the "wake up tomorrow morning at the world spawn" trigger per GDD.
        public SceneRefSO returnScene;
        public string returnTransitionText = "Sang ngay moi...";

        public override async UniTask Execute(InteractionContext ctx)
        {
            // Per GDD: bed interaction MUST show UIConfirm "Đi ngủ" → user
            // clicks OK → THEN day advances. Previously this method had
            // `bool ok = true; if (ctx.showConfirm != null) ok = await ...`
            // which silently treated a null callback as "OK clicked" and
            // jumped straight to day-advance — exactly the bug the user
            // reported: "đến KTX là chuyển sang ngày khác luôn". Requiring
            // an explicit confirmation makes a broken UI chain fail-closed
            // (no day advance) instead of fail-open (silent day advance).
            if (ctx.showConfirm == null)
            {
                Debug.LogError("[SleepInteraction] showConfirm callback null — aborting sleep to avoid silent day-advance");
                return;
            }
            bool ok = await ctx.showConfirm(confirmText);
            if (!ok) return;
            if (ctx.showLoading != null) await ctx.showLoading(loadingText, loadingSeconds);
            ctx.completeCurrentQuest?.Invoke(true);
            ctx.advanceDay?.Invoke();
            if (returnScene != null && ctx.loadReplacing != null)
                await ctx.loadReplacing(returnScene, returnTransitionText);
        }
    }
}

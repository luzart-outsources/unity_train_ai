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
        // Optional scene return — set to SceneRef_10_World for the
        // "in NhaAn after eating → back outside" GDD flow.
        public SceneRefSO returnScene;
        public string returnTransitionText = "Quay tro lai san...";

        public override async UniTask Execute(InteractionContext ctx)
        {
            // Fail-closed: a missing showConfirm callback must NOT default to
            // "OK". Otherwise broken UI chains silently complete the quest +
            // skip clock + swap scenes — same family bug as SleepInteractionSO
            // (user reported "đến KTX là chuyển sang ngày khác luôn"). Log
            // loud so it's traceable when this happens.
            if (ctx.showConfirm == null)
            {
                Debug.LogError($"[OpenConfirmInteraction:{name}] showConfirm callback null — aborting to avoid silent quest completion");
                return;
            }
            bool ok = await ctx.showConfirm(confirmText);
            if (!ok) return;
            if (skipTime && skipToHour >= 0) ctx.skipTimeTo?.Invoke(skipToHour, skipToMinute);
            if (completeQuestOnConfirm) ctx.completeCurrentQuest?.Invoke(true);
            if (returnScene != null && ctx.loadReplacing != null)
                await ctx.loadReplacing(returnScene, returnTransitionText);
        }
    }
}

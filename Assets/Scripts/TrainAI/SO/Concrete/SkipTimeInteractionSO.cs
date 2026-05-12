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

        public override async UniTask Execute(InteractionContext ctx)
        {
            await UniTask.Yield();
            Debug.Log($"[SkipTime] -> {skipToHour:00}:{skipToMinute:00}");
        }
    }
}

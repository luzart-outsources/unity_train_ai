using UnityEngine;
using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_Confirm_New", menuName = "TrainAI/Interaction/Open Confirm")]
    public class OpenConfirmInteractionSO : InteractionSO
    {
        public string confirmText = "Ban dang lam gi do.";

        public override async UniTask Execute(InteractionContext ctx)
        {
            Debug.Log($"[Confirm] {confirmText}");
            await UniTask.Yield();
        }
    }
}

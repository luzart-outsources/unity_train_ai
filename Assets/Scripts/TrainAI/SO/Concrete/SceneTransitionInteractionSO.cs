using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Interact_SceneTransition_New", menuName = "TrainAI/Interaction/Scene Transition")]
    public class SceneTransitionInteractionSO : InteractionSO
    {
        public SceneRefSO targetScene;
        public string transitionText = "Dang chuyen canh...";

        public override async UniTask Execute(InteractionContext ctx)
        {
            await UniTask.Yield();
            Debug.Log($"[SceneTransition] -> {(targetScene != null ? targetScene.sceneName : "null")}");
        }
    }
}

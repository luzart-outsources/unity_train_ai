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
        public bool completeQuestAfter = true;

        public override async UniTask Execute(InteractionContext ctx)
        {
            if (targetScene != null && ctx.loadAdditive != null)
                await ctx.loadAdditive(targetScene, transitionText);
            if (completeQuestAfter) ctx.completeCurrentQuest?.Invoke(true);
        }
    }
}

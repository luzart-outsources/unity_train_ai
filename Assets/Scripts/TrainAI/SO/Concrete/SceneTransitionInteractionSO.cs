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
        // Default true: load target then unload current (frees world memory
        // when entering a building). Set false for overlay-style transitions
        // where the previous scene should stay loaded underneath (rare).
        public bool replaceCurrentScene = true;

        public override async UniTask Execute(InteractionContext ctx)
        {
            if (targetScene != null)
            {
                if (replaceCurrentScene && ctx.loadReplacing != null)
                    await ctx.loadReplacing(targetScene, transitionText);
                else if (ctx.loadAdditive != null)
                    await ctx.loadAdditive(targetScene, transitionText);
            }
            if (completeQuestAfter) ctx.completeCurrentQuest?.Invoke(true);
        }
    }
}

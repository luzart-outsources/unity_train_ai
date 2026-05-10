using SoCollection;
using System.Collections.Generic;
using UnityEngine;


public class FestivalLocation: ScriptableObject,IBlackboardConditional
{
    //The identifier for this festival location configuration
    [SerializeField] string id; 
    public SceneTransitionManager.Location location;
    //The conditions needed for this festival location to trigger
    [SerializeField] BlackboardCondition[] blackboardConditions;

    public SoCollection<FestivalNPCBehaviour> npcs;

    //Check if the condition is met
    public bool CheckConditions(out int score)
    {
        IBlackboardConditional conditional = this;
        return conditional.CheckConditions(blackboardConditions, out score);
    }
}

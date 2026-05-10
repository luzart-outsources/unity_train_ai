using SoCollection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Cutscene", menuName ="Cutscene/Cutscene")]
public class Cutscene : ScriptableObject, IBlackboardConditional
{
    public BlackboardCondition[] conditions;
    //Whether this event cutscene can play again once it has occurred. 
    public bool recurring;

    public SoCollection<CutsceneAction> action;
   
    //Check if the condition is met
    public bool CheckConditions(out int score)
    {
        IBlackboardConditional conditional = this;
        return conditional.CheckConditions(conditions, out score); 
    }

}

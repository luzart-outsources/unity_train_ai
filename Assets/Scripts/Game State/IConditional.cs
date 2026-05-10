using UnityEngine;

//Interface for blackboard conditionals
public interface IBlackboardConditional
{
    
    public bool CheckConditions(BlackboardCondition[] conditions, out int conditionsMet)
    {
        conditionsMet = 0;
        //Get the game blackboard
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();

        //Ensure every condition is met
        foreach (BlackboardCondition condition in conditions)
        {
            if (!blackboard.CompareValue(condition))
            {
                return false;
            }
            conditionsMet++;
        }

        return true;
    }
}

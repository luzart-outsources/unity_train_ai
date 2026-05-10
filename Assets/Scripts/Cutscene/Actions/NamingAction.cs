using UnityEngine;

public class NamingAction : CutsceneAction
{
    //The message to display 
    [SerializeField]
    private string namingPrompt;
    //The key to update in the blackboard
    [SerializeField]
    private string blackboardKeyToUpdate;
    public override void Execute()
    {
        UIManager.Instance.TriggerNamingPrompt(namingPrompt, CompleteNaming);
    }

    void CompleteNaming(string input)
    {
        //Retrieve Blackboard
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();

        blackboard.SetValue(blackboardKeyToUpdate, input);
        onExecutionComplete?.Invoke();
    }
    
}

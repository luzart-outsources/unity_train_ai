using UnityEngine;

public class SceneChangeAction:CutsceneAction
{
    public SceneTransitionManager.Location locationToSwitch; 
    public override void Execute()
    {
        SceneTransitionManager.Instance.SwitchLocation(locationToSwitch);
        onExecutionComplete?.Invoke();
    }




}

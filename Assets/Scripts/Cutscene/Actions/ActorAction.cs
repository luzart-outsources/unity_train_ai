using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Add or move an actor
public class ActorAction : CutsceneAction
{
    public enum ActionType
    {
        Create, Move, Remove
    }
    //The type of action that is to  be done
    public ActionType actionType; 
    //Can be player or NPC
    public string actorName;
    public Vector3 position;

    public override void Execute()
    {
        Debug.Log("Cutscene: Executing actor action");
        //If the action type is of type 'Remove' 
        if (actionType == ActionType.Remove)
        {
            Remove();
            onExecutionComplete?.Invoke();
            return;
        }
        CreateOrMove();


    }

   
    

    //Handles the creation or moving of the actor
    void CreateOrMove()
    {
        CutsceneManager.Instance.AddOrMoveActor(actorName, position, onExecutionComplete);
    }
    //Handles the removal of the actor
    void Remove()
    {
        CutsceneManager.Instance.RemoveActor(actorName);
    }

}

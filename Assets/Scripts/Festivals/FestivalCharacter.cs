using System.Collections.Generic;
using UnityEngine;

public class FestivalCharacter : InteractableCharacter
{

    FestivalNPCBehaviour behaviour;
    PlayerController player;


    Queue<CutsceneAction> actionsToExecute;


    /// <summary>
    /// Convert the InteractableCharacter to a FestivalCharacter instance
    /// </summary>
    /// <param name="interactableCharacter">The interactable class</param>
    /// <returns>The converted instance</returns>
    public static FestivalCharacter ConvertToFestival(InteractableCharacter interactableCharacter)
    {
        CharacterData characterData = interactableCharacter.characterData;
        FestivalCharacter festivalCharacter = interactableCharacter.gameObject.AddComponent<FestivalCharacter>();
        festivalCharacter.characterData = characterData;
        Destroy(interactableCharacter);

        return festivalCharacter;
    }


    /// <summary>
    /// Initialise the behaviour 
    /// </summary>
    /// <param name="behaviour">The behaviour ScriptableObject</param>
    public void SetBehaviour(FestivalNPCBehaviour behaviour)
    {
        this.behaviour = behaviour;
    }

    /// <summary>
    /// Override the dialogue system to trigger the event logic
    /// </summary>
    protected override void TriggerDialogue()
    {
        LookAtPlayer();
        StartNPCEvent(); 

    }

    void StartNPCEvent()
    {
        movement.ToggleMovement(false);
        if (behaviour == null) { throw new System.Exception("No behaviour found");
        }
        //Convert behaviour logic to queue
        actionsToExecute = new Queue<CutsceneAction>(behaviour.onInteract);
        player = FindAnyObjectByType<PlayerController>();
        player.enabled = false;
        UpdateNPCEvent();
    }


    void UpdateNPCEvent()
    {
        if (actionsToExecute.Count == 0)
        {
            EndNPCEvent();
            return;
        }

        //Dequeue
        CutsceneAction actionToExecute = actionsToExecute.Dequeue();
        //Start the action sequence and have it call this function once done
        actionToExecute.Init(() => { UpdateNPCEvent(); });
    }

    void EndNPCEvent()
    {
        player = FindAnyObjectByType<PlayerController>();
        player.enabled = true;
        movement.ToggleMovement(true); 
    }
   


}

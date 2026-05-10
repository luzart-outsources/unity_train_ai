using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AnimalMovement))]
public class AnimalBehaviour : InteractableObject
{
    protected AnimalRelationshipState relationship;
    protected AnimalMovement movement;
    protected AnimalRenderer animalRenderer;
    [SerializeField]
    protected WorldBubble speechBubble;

    protected virtual void Start()
    { 
        movement = GetComponent<AnimalMovement>();
    }

    public void LoadRelationship(AnimalRelationshipState relationship)
    {
        this.relationship = relationship;
        animalRenderer = GetComponent<AnimalRenderer>();
        animalRenderer.RenderAnimal(relationship.age, relationship.animalType);
    }

    public override void Pickup()
    {
        if (relationship == null)
        {
            Debug.LogError("Relationship not set");
            return;
        }
        TriggerDialogue();
    }

    void TriggerDialogue()
    {
        movement.ToggleMovement(false);

        //Get the mood
        int mood = relationship.Mood;

        //The dialogue string to output in the end 
        string dialogueLine = $"{relationship.name} seems ";

        //The action to execute upon dialogue end
        System.Action onDialogueEnd = () =>
        {
            movement.ToggleMovement(true);
        };

        //Check if the player has talked with the animal
        if (!relationship.hasTalkedToday)
        {
            onDialogueEnd += OnFirstConversation; 
        }

        if(mood >= 200 && mood <= 255)
        {
            dialogueLine += "really happy today!";
        } else if(mood >= 30 && mood < 200)
        {
            dialogueLine += "fine.";
        } else {
            dialogueLine += "sad"; 
        }

        DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage(dialogueLine), onDialogueEnd);
    }

    void OnFirstConversation()
    {
        relationship.Mood += 30;
        relationship.hasTalkedToday = true;

        //Set the speech bubble to true
        speechBubble.gameObject.SetActive(true);

        WorldBubble.Emote emote = WorldBubble.Emote.Thinking;


        switch (relationship.Mood)
        {
            case int n when (n >= 200):
                emote = WorldBubble.Emote.Heart;
                break;
            case int n when (n < 30):
                emote = WorldBubble.Emote.Sad;
                break;
            case int n when (n >= 30 && n < 60):
                emote = WorldBubble.Emote.BadMood;
                break;
            default:
                emote = WorldBubble.Emote.Happy;
                break;

        }

        speechBubble.Display(emote, 3f);

        Debug.Log($"{relationship.name} is now of mood {relationship.Mood}");

    }
}

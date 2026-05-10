using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterMovement))]
public class InteractableCharacter : InteractableObject
{
    public CharacterData characterData;

    //Cache the relationship data of the NPC so we can access it
    NPCRelationshipState relationship;

    //The rotation it should be facing by default
    Quaternion defaultRotation;
    //Check if the LookAt coroutine is currently being executed
    bool isTurning = false;

    protected CharacterMovement movement; 

    private void Start()
    {
        relationship = RelationshipStats.GetRelationship(characterData);

        movement = GetComponent<CharacterMovement>(); 

        //Cache the original rotation of the characters
        defaultRotation = transform.rotation;

        //Add listener
        GameStateManager.Instance.onIntervalUpdate.AddListener(OnIntervalUpdate);
    }

    void OnIntervalUpdate()
    {
        //Get data on its location
        NPCLocationState locationState = NPCManager.Instance.GetNPCLocation(characterData.name);
        movement.MoveTo(locationState);
        StartCoroutine(LookAt(Quaternion.Euler(locationState.facing)));

    }

    public override void Pickup()
    {
        LookAtPlayer();
        TriggerDialogue(); 
    }

    #region Rotation
    protected void LookAtPlayer()
    {
        //Get the player's transform
        Transform player = FindObjectOfType<PlayerController>().transform;

        //Get a vector for the direction towards the player
        Vector3 dir = player.position - transform.position;
        //Lock the y axis of the vector so the npc doesn't look up or down to face the player
        dir.y = 0;
        //Convert the direction vector into a quaternion
        Quaternion lookRot = Quaternion.LookRotation(dir);
        StartCoroutine(LookAt(lookRot));

    }
    //Coroutine for the character to progressively turn towards a rotation
    IEnumerator LookAt(Quaternion lookRot)
    {
        //Check if the coroutine is already running
        if (isTurning)
        {
            //Stop the coroutine 
            isTurning = false;
        }
        else
        {
            isTurning = true; 
        }

        while(transform.rotation != lookRot)
        {
            if (!isTurning)
            {
                //Stop coroutine execution
                yield break; 
            }

            //Dont do anything if moving
            
            if(!movement.IsMoving())transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, 720 * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate(); 
        }
        isTurning = false; 
    }

    //Rotate back to its original rotation
    void ResetRotation()
    {
        StartCoroutine(LookAt(defaultRotation)); 
    }
    #endregion

    #region Conversation Interactions
    protected virtual void TriggerDialogue()
    {
        movement.ToggleMovement(false);
        //Check if the player is holding anything 
        if (InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Item))
        {
            //Switch over to the Gift Dialogue function
            GiftDialogue();
            return; 
        }

        List<DialogueLine> dialogueToHave = characterData.defaultDialogue;

        System.Action onDialogueEnd = () =>
        {
            //Allow for movement
            movement.ToggleMovement(true);
            //Continue going to its destination if it was on the way/Reset its initial position
            OnIntervalUpdate(); 
        };

        //Have the character reset their rotation after the conversation is over
        //onDialogueEnd += ResetRotation; 

        //Do the checks to determine which dialogue to put out

        //Filter through the available dialogues through arbitration
        dialogueToHave = DialogueManager.SelectDialogue(dialogueToHave, characterData.dialogues);

        //Is the player meeting for the first time? (Highest Priority)
        if (RelationshipStats.FirstMeeting(characterData))
        {
            //Assign the first meet dialogue
            dialogueToHave = characterData.onFirstMeet;
            onDialogueEnd += OnFirstMeeting;

        }

        if (RelationshipStats.IsFirstConversationOfTheDay(characterData))
        {
            onDialogueEnd += OnFirstConversation;
        }

        DialogueManager.Instance.StartDialogue(dialogueToHave, onDialogueEnd);
    }

    //Handle Gift Giving
    void GiftDialogue()
    {
        if (!EligibleForGift()) return;

        //Get the ItemSlotData of what the player is holding
        ItemSlotData handSlot = InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item);

        List<DialogueLine> dialogueToHave = characterData.neutralGiftDialogue;

        System.Action onDialogueEnd = () =>
        {
            //Allow for movement
            movement.ToggleMovement(true);
            //Continue going to its destination if it was on the way/Reset its initial position
            OnIntervalUpdate();
            //Mark gift as given for today
            relationship.giftGivenToday = true;

            //Remove the item from the player's hand
            InventoryManager.Instance.ConsumeItem(handSlot); 
        };

        //Have the character reset their rotation after the conversation is over 
        //onDialogueEnd += ResetRotation;

        bool isBirthday = RelationshipStats.IsBirthday(characterData);

        //The friendship points to add from the gift
        int pointsToAdd = 0;
        //Do the checks to determine which dialogue to put out
        switch(RelationshipStats.GetReactionToGift(characterData, handSlot.itemData))
        {
            case RelationshipStats.GiftReaction.Like:
                dialogueToHave = characterData.likedGiftDialogue;
                //80
                pointsToAdd = 80;
                if (isBirthday) dialogueToHave = characterData.birthdayLikedGiftDialogue;
                break;

            case RelationshipStats.GiftReaction.Dislike:
                dialogueToHave = characterData.dislikedGiftDialogue;
                //-20
                pointsToAdd = -20;
                if (isBirthday) dialogueToHave = characterData.birthdayDislikedGiftDialogue;
                break;
            case RelationshipStats.GiftReaction.Neutral:
                dialogueToHave = characterData.neutralGiftDialogue;
                //20
                pointsToAdd = 20;
                if (isBirthday) dialogueToHave = characterData.birthdayNeutralGiftDialogue;

                break; 
        }
        //Birthday multiplier
        if (isBirthday) pointsToAdd *= 8;


        RelationshipStats.AddFriendPoints(characterData, pointsToAdd);

        DialogueManager.Instance.StartDialogue(dialogueToHave, onDialogueEnd);
    }

    //Check if the character can be given a gift
    bool EligibleForGift()
    {
        //Reject condition: Player has not unlocked this character yet
        if (RelationshipStats.FirstMeeting(characterData))
        {
            DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage("You have not unlocked this character yet.")); 
            return false; 
        }

        //Reject condition: Player has already given this character a gift today
        if (RelationshipStats.GiftGivenToday(characterData))
        {
            DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage($"You have already given {characterData.name} a gift today."));
            return false; 
        }


        return true; 
    }

    void OnFirstMeeting()
    {
        //Unlock the character on the relationships
        RelationshipStats.UnlockCharacter(characterData);
        //Update the relationship data
        relationship = RelationshipStats.GetRelationship(characterData);
    }

    void OnFirstConversation()
    {
        Debug.Log("This is the first conversation of the day");
        //Add 20 friend points 
        RelationshipStats.AddFriendPoints(characterData, 20);
 
        relationship.hasTalkedToday = true;
    }
    #endregion


}

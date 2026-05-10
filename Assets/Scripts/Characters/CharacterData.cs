using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Character/Character")]
public class CharacterData : ScriptableObject
{
    public Sprite portrait; 
    public GameTimestamp birthday;
    public List<ItemData> likes;
    public List<ItemData> dislikes;
    public GameObject prefab; 

    [Header("Dialogue")]
    //The dialogue to have when the player first meets this character
    public List<DialogueLine> onFirstMeet;
    //What the character says by default
    public List<DialogueLine> defaultDialogue;

    //Gift reactions
    public List<DialogueLine> likedGiftDialogue; 
    public List<DialogueLine> dislikedGiftDialogue; 
    public List<DialogueLine> neutralGiftDialogue;

    //Birthdays
    public List<DialogueLine> birthdayLikedGiftDialogue;
    public List<DialogueLine> birthdayDislikedGiftDialogue;
    public List<DialogueLine> birthdayNeutralGiftDialogue;

    public DialogueCondition[] dialogues; 
}

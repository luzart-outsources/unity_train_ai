using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelationshipStats : MonoBehaviour
{

    const string RELATIONSHIP_PREFIX = "NPCRelationship_";
    //The relationship data of all the NPCs that the player has met in the game
    public static List<NPCRelationshipState> relationships = new();
    
    public enum GiftReaction
    {
        Like, Dislike, Neutral
    }

    
    public static void LoadStats()
    {
        relationships = new List<NPCRelationshipState>();
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        foreach (CharacterData c in NPCManager.Instance.Characters()) {
            if (blackboard.TryGetValue(RELATIONSHIP_PREFIX + c.name, out NPCRelationshipState rs))
            {
                 relationships.Add(rs);
            }
            
        }

        /*
        if (relationshipsToLoad == null)
        {
            relationships = new List<NPCRelationshipState>();
            return; 
        }
        relationships = relationshipsToLoad;*/
    }

    //Check if the player has met this NPC. 
    public static bool FirstMeeting(CharacterData character)
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        return !blackboard.ContainsKey(RELATIONSHIP_PREFIX + character.name); 
        //return !relationships.Exists(i => i.name == character.name);
    }

    //Get relationship information about a character
    public static NPCRelationshipState GetRelationship(CharacterData character)
    {
        //Check if it is the first meeting
        if (FirstMeeting(character)) return null;
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        if (blackboard.TryGetValue(RELATIONSHIP_PREFIX + character.name, out NPCRelationshipState relationship)) ; 
        return relationship;


        //return relationships.Find(i => i.name == character.name);
    }

    //Add the character to the relationships data
    public static void UnlockCharacter(CharacterData character)
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        NPCRelationshipState relationship = new NPCRelationshipState(character.name); 
        blackboard.SetValue(RELATIONSHIP_PREFIX + character.name, relationship);
        relationships.Add(relationship);
    }

    //Improve the relationship with an NPC
    public static void AddFriendPoints(CharacterData character, int points)
    {
        if (FirstMeeting(character))
        {
            Debug.LogError("The player has not met this character yet!"); 
            return; 
        }

        GetRelationship(character).friendshipPoints += points;
    }

    //Check if this is the first conversation the player is having with the NPC today
    public static bool IsFirstConversationOfTheDay(CharacterData character)
    {
        //If the player is meeting him for the first time, definitely is
        if (FirstMeeting(character)) return true;

        NPCRelationshipState npc = GetRelationship(character);
        return !npc.hasTalkedToday; 
    }

    //Check if the player has already given this character a gift today
    public static bool GiftGivenToday(CharacterData character)
    {
        NPCRelationshipState npc = GetRelationship(character);
        return npc.giftGivenToday; 
    }

    public static GiftReaction GetReactionToGift(CharacterData character, ItemData item)
    {
        //If it's in the list of liked items, it means the character likes it
        if (character.likes.Contains(item)) return GiftReaction.Like;
        //If it's in the list of disliked items, it means the character dislikes it
        if (character.dislikes.Contains(item)) return GiftReaction.Dislike;

        return GiftReaction.Neutral; 

    }

    //Check if it's the character's birthday
    public static bool IsBirthday(CharacterData character)
    {
        GameTimestamp birthday = character.birthday;
        GameTimestamp today = TimeManager.Instance.GetGameTimestamp();

        return (today.day == birthday.day) && (today.season == birthday.season);
    }

    public static bool IsBirthday(CharacterData character, GameTimestamp today)
    {
        GameTimestamp birthday = character.birthday;
        

        return (today.day == birthday.day) && (today.season == birthday.season);
    }

    public static CharacterData WhoseBirthday(GameTimestamp timestamp)
    {
        
        foreach (CharacterData c in NPCManager.Instance.Characters())
        {
            if (IsBirthday(c, timestamp))
            {
                return c; 
            }
        }
        return null; 
    }

}

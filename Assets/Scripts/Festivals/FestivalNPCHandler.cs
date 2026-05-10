using System.Collections.Generic;
using UnityEngine;
using SoCollection; 
public class FestivalNPCHandler
{
    List<FestivalCharacter> charactersInScene;

    public FestivalNPCHandler()
    {
        charactersInScene = new List<FestivalCharacter>();
    }


    /// <summary>
    /// Clear record of the NPCs
    /// </summary>
    public void ClearNPCs()
    {
        charactersInScene.Clear();
    }

    /// <summary>
    /// Load in festival data and execute accordingly
    /// </summary>
    /// <param name=""></param>
    public void LoadFestivalData(FestivalData festival)
    {
        //Clear NPCs
        ClearNPCs();

        //Current location
        SceneTransitionManager.Location location = SceneTransitionManager.Instance.currentLocation;
        //Current time
        GameTimestamp timestamp = TimeManager.Instance.GetGameTimestamp();

        
        //Find all candidate location data
        List<FestivalLocation> candidates = festival.LocationConfiguration().FindAll(x => (x.location == location));

        int highestScore = -1;
        FestivalLocation npcLocationDataToLoad = null; 
        foreach (FestivalLocation candidate in candidates) { 
        
            if(candidate.CheckConditions(out int score))
            {
                if (score > highestScore) {
                    highestScore = score; 
                    npcLocationDataToLoad = candidate;
                }
            }
        }

        if (npcLocationDataToLoad == null) return; 

        LoadNPCData(npcLocationDataToLoad);

    }


    /// <summary>
    /// Load in the NPCs at the festival event with the special festival logic loaded in
    /// </summary>
    /// <param name="npcLocationData"></param>
    void LoadNPCData(FestivalLocation npcLocationData)
    {
        

        foreach (FestivalNPCBehaviour npc in npcLocationData.npcs)
        {

            CharacterData character = npc.character;
            if (character == null) continue;

            GameObject characterGameObject = MonoBehaviour.Instantiate(character.prefab, npc.position, Quaternion.Euler(npc.rotation)) ;

            //Convert the interactable character into the FestivalCharacter class 
            FestivalCharacter festivalCharacter = FestivalCharacter.ConvertToFestival(characterGameObject.GetComponent<InteractableCharacter>());
            //Load in the behaviour onto the festivalCharacter
            festivalCharacter.SetBehaviour(npc); 
            //The character in question will be added to the list
            charactersInScene.Add(festivalCharacter); 
            

        }
    }


}

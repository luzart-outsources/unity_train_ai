using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class AnimalStats : MonoBehaviour
{

    public const string ANIMAL_COUNT = "AnimalCount";
    public const string ANIMAL_DATA = "AnimalRelationship"; 

    //The relationship data of all the NPCs that the player has met in the game
    public static List<AnimalRelationshipState> animalRelationships = new List<AnimalRelationshipState>();

    //Load all the animal data scriptable objects
    static List<AnimalData> animals = Resources.LoadAll<AnimalData>("Animals").ToList();

    //To be fired up when a new animal is born or purchased
    public static void StartAnimalCreation(AnimalData animalType)
    {
        
        //Handle Animal spawning here
        UIManager.Instance.TriggerNamingPrompt($"Give your new {animalType.name} a name.", (inputString) => {
            //Retrieve Blackboard
            GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
            //Initialise animal count parameter
            if (!blackboard.TryGetValue(ANIMAL_COUNT, out int animalCount))
            {
                blackboard.SetValue(ANIMAL_COUNT, 0);
                animalCount = 0;
            }
            //Handle stats on animal type
            if (!blackboard.TryGetValue(ANIMAL_COUNT + animalType.name, out int animalTypeCount))
            {
                blackboard.SetValue(ANIMAL_COUNT + animalType.name, 0);
                animalTypeCount = 0;
            }

            //Create a new animal and add it to the animal relationships data
            AnimalRelationshipState animalRelationshipData =  new AnimalRelationshipState(animalCount, inputString, animalType);

            //Save the entry of the relationship to the blackboard
            //Animal entries are set by number, e.g. if this is the first animal it will be of id 0 
            blackboard.SetValue(ANIMAL_DATA + animalCount, animalRelationshipData);

            //Statistics update
            animalCount++;
            animalTypeCount++; 
            blackboard.SetValue(ANIMAL_COUNT, animalCount);
            blackboard.SetValue(ANIMAL_COUNT + animalType.name, animalTypeCount);

            //Add it to our local cache
            animalRelationships.Add(animalRelationshipData);
            
        });
    }

    //Load in the animal relationships
    public static void LoadStats()
    {
        //Load from the Blackboard data 
        animalRelationships = new List<AnimalRelationshipState>();
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();

        //Get the animal count
        if(blackboard.TryGetValue(ANIMAL_COUNT, out int animalCount)){
            for (int i = 0; i < animalCount; i++)
            {
                if (!blackboard.TryGetValue(ANIMAL_DATA + i, out AnimalRelationshipState animalRelationship)) continue;

                animalRelationships.Add(animalRelationship); 

            }
        }
        

        /*
        Debug.Log("Animals: " + relationshipsToLoad.Count); 
        if(relationshipsToLoad == null)
        {
            animalRelationships = new List<AnimalRelationshipState>();
            return; 
        }
        animalRelationships = relationshipsToLoad;
        */
    }

    //Get the animals by type
    public static List<AnimalRelationshipState> GetAnimalsByType(string animalTypeName)
    {

        return animalRelationships.FindAll(x => x.animalType == animalTypeName);
    }

    public static List<AnimalRelationshipState> GetAnimalsByType(AnimalData animalType)
    {
        return GetAnimalsByType(animalType.name);
    }

    public static void OnDayReset()
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        //Reset animal relationship states
        foreach (AnimalRelationshipState animal in AnimalStats.animalRelationships)
        {
            //Increase friendship if player has spoken with the animal
            if (animal.hasTalkedToday)
            {
                animal.friendshipPoints += 30;
            } else
            {
                animal.friendshipPoints -= (10 - (animal.friendshipPoints / 200));
            }

            //Feeding
            if (animal.giftGivenToday)
            {
                animal.Mood += 15;
            }
            else
            {
                animal.Mood -= 100;
                animal.friendshipPoints -= 20; 
            }

            animal.hasTalkedToday = false;
            //Gift given refers to whether the animal has been fed
            animal.giftGivenToday = false;
            animal.givenProduceToday = false;

            //Advance the age of the animal 
            animal.age++;

            //Update its value in the blackboard
            blackboard.SetValue(ANIMAL_DATA + animal.id, animal); 
            
        }
    }

    //Get the animal data type from a string
    public static AnimalData GetAnimalTypeFromString(string name)
    {
        return animals.Find(i => i.name == name);
    }

}

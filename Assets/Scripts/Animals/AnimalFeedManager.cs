using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimalFeedManager : MonoBehaviour
{
    //All the feedboxes for the different animals in existence
    public static Dictionary<AnimalData, bool[]> feedboxStatus = new Dictionary<AnimalData, bool[]>();

    public Feedbox[] feedboxes;

    //The associated 
    public AnimalData animal;

    private void OnEnable()
    {
        feedboxes = GetComponentsInChildren<Feedbox>();
        RegisterFeedboxes();
        LoadFeedboxData();
    }

    public static void ResetFeedboxes()
    {
        feedboxStatus = new Dictionary<AnimalData, bool[]>();
    }

    public void FeedAnimal(int id)
    {
        //Get the list of all animals associated with the feedboxes
        List<AnimalRelationshipState> eligibleAnimals = AnimalStats.GetAnimalsByType(animal);

        //Find the first animal we haven't fed yet
        foreach(AnimalRelationshipState a in eligibleAnimals)
        {
            if (!a.giftGivenToday)
            {
                a.giftGivenToday = true;
                Debug.Log(a.name + " is fed.");
                break; 
            }
        }

        //Update the status accordingly
        feedboxStatus[animal][id] = true;
        //Make sure it's represented
        LoadFeedboxData();
    }

    //Assign each feedbox an ID
    void RegisterFeedboxes()
    {
        for (int i = 0; i < feedboxes.Length; i++)
        {
            feedboxes[i].id = i;
        }
    }

    void LoadFeedboxData()
    {
        //Check if feedboxStatus contains the dictionary entry for our animal
        if (!feedboxStatus.ContainsKey(animal))
        {
            //If not, create a new entry with default feedbox status (false)
            feedboxStatus.Add(animal, new bool[feedboxes.Length]);
        }

        //Get the current feedbox status
        bool[] currentFeedboxStatus = feedboxStatus[animal];

        //Have the information represented in the scene
        for(int i =0; i<feedboxes.Length; i++)
        {
            feedboxes[i].SetFeedState(currentFeedboxStatus[i]);
        }
    }
}

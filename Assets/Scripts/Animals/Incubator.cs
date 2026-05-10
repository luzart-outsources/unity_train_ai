using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Incubator : InteractableObject
{
    //Whether it has an egg placed inside
    bool containsEgg;
    //The time it takes to incubate the egg in minutes
    int timeToIncubate;
    
    //Egg to display when the player has placed an egg in the incubator
    public GameObject displayEgg;

    //The ID of the incubator instance 
    public int incubationID;

    public override void Pickup()
    {
        if (CanIncubate())
        {
            StartIncubation(); 
        }

        
    }

    //Check if the player is able to throw in an egg to start incubation
    bool CanIncubate()
    {
        //Get the item data the player
        ItemData handSlotItem = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Item);

        //If the player is not holding anything or if there is already an egg, he cannot incubate anything
        if (handSlotItem == null || containsEgg) return false;

        //Make sure you set the item to egg
        if (handSlotItem.name != item.name) return false;

        return true; 
    }

    //Starts the incubation
    void StartIncubation()
    {
        //Consume the item in the player's hand (put the egg in the incubator)
        InventoryManager.Instance.ConsumeItem(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item));
        int hours = GameTimestamp.DaysToHours(IncubationManager.daysToIncubate); 
        SetIncubationState(true, GameTimestamp.HoursToMinutes(hours));

        //Register it with the list of incubating eggs
        IncubationManager.eggsIncubating.Add(new EggIncubationSaveState(incubationID, timeToIncubate)); 
    }

    //Update the values of the incubation state
    public void SetIncubationState(bool containsEgg, int timeToIncubate)
    {
        //Set the values accordingly
        this.containsEgg = containsEgg;
        this.timeToIncubate = timeToIncubate;

        //Render the egg in the scene if it has an egg
        displayEgg.SetActive(containsEgg);
    }
}

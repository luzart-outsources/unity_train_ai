using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Feedbox : InteractableObject
{
    bool containsFeed;
    public GameObject displayFeed;
    public int id;

    public override void Pickup()
    {
        if (CanFeed())
        {
            //Feed the animal
            FeedAnimal(); 
        }
    }

    void FeedAnimal()
    {
        //Consume the item in the player's hand (put the feed in )
        InventoryManager.Instance.ConsumeItem(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item));
        SetFeedState(true);

        FindObjectOfType<AnimalFeedManager>().FeedAnimal(id); 
        
    }

    public void SetFeedState(bool feed)
    {
        containsFeed = feed;
        //Render the feed in the scene if the player has placed it
        displayFeed.SetActive(feed); 
    }

    //Check if the player is able to throw in the animal feed 
    bool CanFeed()
    {
        //Get the item data the player
         ItemData handSlotItem = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Item);

        //If the player is not holding anything or if there is already the feed, he cannot place another
        if (handSlotItem == null || containsFeed) return false;

        //Make sure you set the item to feed 
        if (handSlotItem.name != item.name) return false;
        return true;
    }
}

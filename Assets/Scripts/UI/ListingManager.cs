using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ListingManager<T> : MonoBehaviour
{
    //Listing Entry Prefab to instantiate
    public GameObject listingEntryPrefab;
    //The transform of the grid to instantiate the entries on
    public Transform listingGrid;

    public void Render(List<T> listingItems)
    {
        //Reset the listings if there was a previous one
        if (listingGrid.childCount > 0)
        {
            foreach (Transform child in listingGrid)
            {
                Destroy(child.gameObject);
            }
        }

        //Create a new listing for every item
        foreach (T listingItem in listingItems)
        {
            //Instantiate a shop listing prefab for the item
            GameObject listingGameObject = Instantiate(listingEntryPrefab, listingGrid);

            DisplayListing(listingItem, listingGameObject); 
        }
    }
    
    //Handle how each listing is going to be displayed
    protected abstract void DisplayListing(T listingItem, GameObject listingGameObject); 
}

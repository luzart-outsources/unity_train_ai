using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquipmentData;

public class PlayerInteraction : MonoBehaviour
{
    PlayerToolController toolControl; 

    //The land the player is currently selecting
    Land selectedLand = null;

    //The interactable object the player is currently selecting
    InteractableObject selectedInteractable = null; 

    // Start is called before the first frame update
    void Start()
    {
        toolControl = GetComponentInParent<PlayerToolController>();
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit; 
        if(Physics.Raycast(transform.position, Vector3.down,out hit,  1))
        {
            OnInteractableHit(hit);
        }
    }

    //Handles what happens when the interaction raycast hits something interactable
    void OnInteractableHit(RaycastHit hit)
    {
        Collider other = hit.collider;
        
        //Check if the player is going to interact with land
        if(other.tag == "Land")
        {
            //Get the land component
            Land land = other.GetComponent<Land>();
            SelectLand(land);
            return; 
        }

        //Check if the player is going to interact with an Item
        if(other.tag == "Item")
        {
            //Set the interactable to the currently selected interactable
            selectedInteractable = other.GetComponent<InteractableObject>();
            selectedInteractable.OnHover(); 
            return; 
        }

        //Deselect the interactable if the player is not standing on anything at the moment
        if(selectedInteractable != null)
        {
            selectedInteractable.OnMoveAway(); 
            selectedInteractable = null; 
        }

        //Deselect the land if the player is not standing on any land at the moment
        if(selectedLand != null)
        {
            selectedLand.Select(false);
            selectedLand = null;
        }
    }

    //Handles the selection process of the land
    void SelectLand(Land land)
    {
        //Set the previously selected land to false (If any)
        if (selectedLand != null)
        {
            selectedLand.Select(false);
        }
        
        //Set the new selected land to the land we're selecting now. 
        selectedLand = land; 
        land.Select(true);
    }

    //Triggered when the player presses the tool button
    public void Interact()
    {
        //Use the tool 
        toolControl.StartToolUse(selectedLand);
    }

    //Triggered when the player presses the item interact button
    public void ItemInteract()
    {

        if (InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Item))
        {
            //If the player is holding an item 
            ItemData itemSlot = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Item);
            //Check if it is consumable
            if(itemSlot is FoodData)
            {
                FoodData foodData = itemSlot as FoodData;
                foodData.OnConsume();
                //Consume it 
                InventoryManager.Instance.ConsumeItem(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item)); 
            }
        }
        //If the player isn't holding anything, pick up an item
        

        //Check if there is an interactable selected
        if (selectedInteractable != null)
        {
            //Pick it up
            selectedInteractable.Pickup();
        }

    }

    
    public void ItemKeep()
    {
        //If the player is holding something, keep it in his inventory
        if (InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Item))
        {
            InventoryManager.Instance.HandToInventory(InventorySlot.InventoryType.Item);
            return;
        }
    }
}

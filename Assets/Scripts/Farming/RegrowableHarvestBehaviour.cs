using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RegrowableHarvestBehaviour : InteractableObject
{
    CropBehaviour parentCrop; 

    //Sets the parent crop
    public void SetParent(CropBehaviour parentCrop)
    {
        this.parentCrop = parentCrop;
    }

    public override void Pickup()
    {
        //Do an additional check to see if it can be harvested
        if(parentCrop.cropState != CropBehaviour.CropState.Harvestable)
        {
            UIManager.Instance.DeactivateInteractPrompt();
            return; 
        }
        //Call the OnInteract Callback
        onInteract?.Invoke();
        
        //Check if the player is holding on to an item
        if (InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Item))
        {
            //Send the item to inventory before equipping
            InventoryManager.Instance.HandToInventory(InventorySlot.InventoryType.Item);
        }

        //Set the player's inventory to the item
        InventoryManager.Instance.EquipHandSlot(item);

        //Update the changes in the scene
        InventoryManager.Instance.RenderHand();

        //Set the parent crop back to seedling to regrow it
        parentCrop.Regrow();
        UIManager.Instance.DeactivateInteractPrompt();

    }
}

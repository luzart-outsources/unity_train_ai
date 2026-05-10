using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour
{
    //The item information the GameObject is supposed to represent
    public ItemData item;
    public UnityEvent onInteract = new UnityEvent();

    [SerializeField]
    protected string interactText = "Interact";
    [SerializeField]
    protected float offset = 1.5f; 

    public virtual void Pickup()
    {
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

        //Disable the prompt
        OnMoveAway();
        //Destroy this instance so as to not have multiple copies
        GameStateManager.Instance.PersistentDestroy(gameObject); 
    }

    //When the player is hovering around the item
    public virtual void OnHover()
    {
        UIManager.Instance.InteractPrompt(transform, interactText, offset);
    }

    //What happens when the player is in front of the item
    public virtual void OnMoveAway()
    {
        UIManager.Instance.DeactivateInteractPrompt(); 
    }
}

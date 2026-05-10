using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; 

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    ItemData itemToDisplay;
    int quantity; 

    public Image itemDisplayImage;
    public TextMeshProUGUI quantityText; 

    public enum InventoryType
    {
        Item, Tool, Storage
    }
    //Determines which inventory section this slot is apart of.
    public InventoryType inventoryType;

    protected int slotIndex; 

    public void Display(ItemSlotData itemSlot)
    {
        //Set the variables accordingly 
        itemToDisplay = itemSlot.itemData;
        quantity = itemSlot.quantity;

        //By default, the quantity text should not show
        quantityText.text = "";

        //Check if there is an item to display
        if(itemToDisplay != null)
        {
            //Switch the thumbnail over
            itemDisplayImage.sprite = itemToDisplay.thumbnail;
            
            //Display the stack quantity if there is more than 1 in the stack
            if(quantity > 1)
            {
                quantityText.text = quantity.ToString();
            }

            itemDisplayImage.gameObject.SetActive(true);

            return; 
        }

        itemDisplayImage.gameObject.SetActive(false);

        
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        //Check if it is a right or left click
        bool isRightClick = (eventData.button == PointerEventData.InputButton.Right);
        bool shift = Input.GetKey(KeyCode.LeftShift);


        if (!isRightClick && shift)
        {
            MoveEntireStack();
        } else
        {
            InventoryManager.Instance.SelectSlot(slotIndex, inventoryType, isRightClick);
        }
    }

    /// <summary>
    /// Handles the movement of items by the entire stack. Triggered when the player holds left shift + left click
    /// </summary>
    protected virtual void MoveEntireStack()
    {
        //Different behaviour if the storage panel is open
        if (UIManager.Instance.IsStoragePanelOpen())
        {
            StorageManager.TransferItems(slotIndex, inventoryType);
            return; 
        }

        //Move item from inventory to hand
        InventoryManager.Instance.InventoryToHand(slotIndex, inventoryType);
    }

    //Set the Slot index
    public void AssignIndex(int slotIndex)
    {
        this.slotIndex = slotIndex;
    }


    //Display the item info on the item info box when the player mouses over
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(itemToDisplay);
    }

    //Reset the item info box when the player leaves
    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(null);
    }
}

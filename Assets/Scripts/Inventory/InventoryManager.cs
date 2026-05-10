using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        //If there is more than one instance, destroy the extra
        if(Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            //Set the static instance to this instance
            Instance = this; 
        }
    }

    List<ItemData> _itemIndex;

    public ItemData GetItemFromString(string name)
    {
        if (_itemIndex == null)
        {
            _itemIndex = Resources.LoadAll<ItemData>("").ToList();
        }
        return _itemIndex.Find(i => i.name == name);
    }

    [Header("Tools")]
    //Tool Slots
    [SerializeField]
    private ItemSlotData[] toolSlots = new ItemSlotData[8];
    //Tool in the player's hand
    [SerializeField]
    private ItemSlotData equippedToolSlot = null; 

    [Header("Items")]
    //Item Slots
    [SerializeField]
    private ItemSlotData[] itemSlots = new ItemSlotData[8];
    //Item in the player's hand
    [SerializeField]
    private ItemSlotData equippedItemSlot = null;

    //The transform for the player to hold items in the scene
    public Transform handPoint;


    //Inventory Selection System
    ItemSlotData selectedItemSlot = new ItemSlotData(null, 0);
    int originalIndex = -2;
    InventorySlot.InventoryType selectionInventoryType = InventorySlot.InventoryType.Storage; 

    

    //Load the inventory from a save 
    public void LoadInventory(ItemSlotData[] toolSlots, ItemSlotData equippedToolSlot, ItemSlotData[] itemSlots, ItemSlotData equippedItemSlot)
    {
        this.toolSlots = toolSlots;
        this.equippedToolSlot = equippedToolSlot;
        this.itemSlots = itemSlots;
        this.equippedItemSlot = equippedItemSlot;

        //Update the changes in the UI and scene
        UIManager.Instance.RenderInventory();
        RenderHand();
    }


    /// <summary>
    /// Handles the Selection of the Slot 
    /// </summary>
    /// <param name="slotIndex">The index of the slot</param>
    /// <param name="inventoryType">The inventory type</param>
    /// <param name="isRightClick">Whether it is a right click</param>
    public void SelectSlot(int slotIndex, InventorySlot.InventoryType inventoryType, bool isRightClick)
    {
        bool hasSelection = !selectedItemSlot.IsEmpty();


        //Get the slot in question 
        ItemSlotData slotToManipulate = GetSlotData(slotIndex, inventoryType);

        //Now that we have the slot in question, see if the slot is empty
        bool slotHasSomething = !slotToManipulate.IsEmpty();


        //Variables in play: 
        //selection: Whether we have anything selected
        //slotIsEmpty: Whether the slot we've selected is empty
        
        //Case 0: No selection, slot has nothing. Nothing to do here
        if (!hasSelection && !slotHasSomething) return;

        //Check if selection matches slots
        //Does not apply to Storage slots
        if (hasSelection && inventoryType != InventorySlot.InventoryType.Storage)
        {
            if (IsTool(selectedItemSlot.itemData) && inventoryType != InventorySlot.InventoryType.Tool) return;
            if (!IsTool(selectedItemSlot.itemData) && inventoryType != InventorySlot.InventoryType.Item) return;
        }


        //Case 1: No selection, slot filled with something
        if (!hasSelection && slotHasSomething)
        {
            Debug.Log("Selection Case 1");
            //Left Click
            if (!isRightClick)
            {
                SlotToSelection(slotIndex, inventoryType);


            } else
            {
                //Right Click
                //Check if the quantity is 1
                if(selectedItemSlot.quantity == 1)
                {
                    SlotToSelection(slotIndex, inventoryType);

                } else
                {
                    //Select half the slots
                    int quantityToTake = slotToManipulate.quantity / 2;
                    Debug.Log("Taking " + quantityToTake);
                    SlotToSelection(slotIndex, inventoryType, quantityToTake);
                }
            }

            //Render the inventory
            UIManager.Instance.RenderInventory();
            RenderHand();
            return; 
        }

        //Case 2: Selection has something, slot has nothing
        if(hasSelection && !slotHasSomething)
        {
            
            Debug.Log("Selection Case 2");
            //Left Click
            if (!isRightClick)
            {
                SelectionToSlot(slotIndex, inventoryType);

            }
            else
            {
                Debug.Log("Right click");
                //Right Click
                //If exactly 1, just transfer the whole thing
                if(selectedItemSlot.quantity == 1)
                {
                    Debug.Log("Transferring the whole thing");
                    SelectionToSlot(slotIndex, inventoryType);
                } else
                {
                    //Add 1 of the selection into the slot
                    selectedItemSlot.Remove();
                    //Cache this remaining value
                    ItemSlotData newSelectionValue = new ItemSlotData(selectedItemSlot);
                    
                    //Transfer exactly 1 to the inventory slot
                    selectedItemSlot.quantity = 1; 
                    SelectionToSlot(slotIndex, inventoryType);

                    //Put back the selection
                    selectedItemSlot = newSelectionValue;

                }

            }
            //Render the inventory
            UIManager.Instance.RenderInventory();
            RenderHand();
            return; 
        }

        //Case 3: Selection has something, slot has something
        if(hasSelection && slotHasSomething)
        {
            Debug.Log("Selection Case 3");
            //Left Click
            if (!isRightClick)
            {
                //Can they stack? 
                if (selectedItemSlot.Stackable(slotToManipulate))
                {
                    //If yes, stack
                    slotToManipulate.AddQuantity(selectedItemSlot.quantity);
                    selectedItemSlot.Empty(); 
                }
                else
                {
                    //If no, don't stack and just swap selection with item
                    SwapSelection(slotIndex, inventoryType);

                }

            }
            else
            {
                //Right Click
                //Can they stack? 
                if (selectedItemSlot.Stackable(slotToManipulate))
                {
                    //If yes, stack 1
                    //Add 1 of the selection into the slot
                    selectedItemSlot.Remove();
                    slotToManipulate.AddQuantity(1);
                }
                else
                {
                    //If no, don't stack and just swap selection with item
                    SwapSelection(slotIndex, inventoryType);
                }

            }
            //Render the inventory
            UIManager.Instance.RenderInventory();
            RenderHand();
            return; 
        }


    }

    //Cancel the selection
    public void CancelSelection()
    {
        if (selectedItemSlot.IsEmpty()) return;
        Debug.Log("CANCELLING SELECTION");
        
        SelectionToSlot(originalIndex, selectionInventoryType);
        UIManager.Instance.RenderInventory();
    }

    public ItemSlotData GetSelected()
    {
        return selectedItemSlot;
    }

    /// <summary>
    /// Handle moving slot data to the selection
    /// </summary>
    /// <param name="slotIndex"></param>
    /// <param name="inventoryType"></param>
    /// <param name="quantity">The amount to transfer. -1 for all</param>
    void SlotToSelection(int slotIndex, InventorySlot.InventoryType inventoryType, int quantity=-1)
    {
        ItemSlotData slotToManipulate = GetSlotData(slotIndex, inventoryType);

        //Set the quantity accordingly
        if (quantity == -1) quantity = slotToManipulate.quantity; 


        selectedItemSlot = new ItemSlotData(slotToManipulate.itemData, quantity);
        originalIndex = slotIndex;
        selectionInventoryType = inventoryType;
        
        switch(inventoryType)
        {
            case InventorySlot.InventoryType.Item:
                if (slotIndex == -1)
                {
                    equippedItemSlot.AddQuantity(-quantity);
                    break; 
                }
                itemSlots[slotIndex].AddQuantity(-quantity);
                break;
            case InventorySlot.InventoryType.Tool:
                if (slotIndex == -1)
                {
                    equippedToolSlot.AddQuantity(-quantity);
                    break;
                }
                toolSlots[slotIndex].AddQuantity(-quantity);
                break;
            case InventorySlot.InventoryType.Storage:
                StorageManager.ClearSlot(slotIndex);
                break;
        }

    }


    void SelectionToSlot(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        Debug.Log($"Selection to slot {slotIndex} of type {inventoryType}.");
        Debug.Log($"The selected item slot is {selectedItemSlot.itemData.name}");
        switch (inventoryType)
        {
            case InventorySlot.InventoryType.Item:
                if (slotIndex == -1)
                {
                    equippedItemSlot = new ItemSlotData(selectedItemSlot);
                } else
                {
                    itemSlots[slotIndex] = new ItemSlotData(selectedItemSlot);
                }
                
                break; 
            case InventorySlot.InventoryType.Tool:
                if (slotIndex == -1)
                {
                    equippedToolSlot = new ItemSlotData(selectedItemSlot);
                }
                else
                {
                    toolSlots[slotIndex] = new ItemSlotData(selectedItemSlot);
                }
                
                break;

            case InventorySlot.InventoryType.Storage:
                StorageManager.SetSlotData(slotIndex, selectedItemSlot);
                break;
        }
        
        originalIndex = -2;
        selectedItemSlot.Empty();
    }

    /// <summary>
    /// Get the data Item Slot data of the given slot. 
    /// </summary>
    /// <param name="slotIndex">The index of the slot. -1 for hand (equipped item) slot</param>
    /// <param name="inventoryType">The inventory type</param>
    /// <returns>The Item Slot associated with the slot index and type</returns>
    ItemSlotData GetSlotData(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        bool isHandSlot = slotIndex == -1;
        //Get the slot in question 
        ItemSlotData slotToManipulate = null;

        if (isHandSlot)
        {
            slotToManipulate = GetEquippedSlot(inventoryType);
            return slotToManipulate;
        }

        
        switch (inventoryType)
        {
            case InventorySlot.InventoryType.Item:
                slotToManipulate = itemSlots[slotIndex];
                break;

            case InventorySlot.InventoryType.Tool:
                slotToManipulate = toolSlots[slotIndex];
                break;
            case InventorySlot.InventoryType.Storage:
                slotToManipulate = StorageManager.GetSlotData(slotIndex);
                break; 

        }
        return slotToManipulate;
    }



    void SwapSelection(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        //Cache the selected slot
        ItemSlotData selectedSlotCache = new ItemSlotData(selectedItemSlot);

        //Retrieve the associated slot data
        ItemSlotData slotToManipulate = GetSlotData(slotIndex, inventoryType);

        selectedItemSlot = new ItemSlotData(slotToManipulate);

        

        switch (inventoryType)
        {
            case InventorySlot.InventoryType.Item:
                if (slotIndex == -1)
                {
                    equippedItemSlot = selectedSlotCache;
                    return; 
                }
                itemSlots[slotIndex] = selectedSlotCache;
                break;
            case InventorySlot.InventoryType.Tool:
                if (slotIndex == -1)
                {
                    equippedToolSlot = selectedSlotCache;
                    return;
                }
                toolSlots[slotIndex] = selectedSlotCache;
                break;
            case InventorySlot.InventoryType.Storage:
                StorageManager.SetSlotData(slotIndex, selectedSlotCache);
                break;
        }

    }


    //Equipping

    //Handles movement of item from Inventory to Hand
    public void InventoryToHand(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        //The slot to equip (Tool by default)
        ItemSlotData handToEquip = equippedToolSlot;
        //The array to change
        ItemSlotData[] inventoryToAlter = toolSlots; 
        
        if(inventoryType == InventorySlot.InventoryType.Item)
        {
            //Change the slot to item
            handToEquip = equippedItemSlot;
            inventoryToAlter = itemSlots;
        }

        //Check if stackable
        if (handToEquip.Stackable(inventoryToAlter[slotIndex]))
        {
            ItemSlotData slotToAlter = inventoryToAlter[slotIndex];

            //Add to the hand slot
            handToEquip.AddQuantity(slotToAlter.quantity);

            //Empty the inventory slot
            slotToAlter.Empty();


        } else
        {
            //Not stackable
            //Cache the Inventory ItemSlotData
            ItemSlotData slotToEquip = new ItemSlotData(inventoryToAlter[slotIndex]);

            //Change the inventory slot to the hands
            inventoryToAlter[slotIndex] = new ItemSlotData(handToEquip);

            //Check if the slot to equip is empty
            if (slotToEquip.IsEmpty())
            {
                //Empty the hand instead
                handToEquip.Empty(); 
            } else
            {
                EquipHandSlot(slotToEquip);
            }
            
        }

        //Update the changes in the scene
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            RenderHand();
        }

        //Update the changes to the UI
        UIManager.Instance.RenderInventory();

    }

    //Handles movement of item from Hand to Inventory
    public void HandToInventory(InventorySlot.InventoryType inventoryType)
    {
        //The slot to move from (Tool by default)
        ItemSlotData handSlot = equippedToolSlot;
        //The array to change
        ItemSlotData[] inventoryToAlter = toolSlots;

        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            handSlot = equippedItemSlot;
            inventoryToAlter = itemSlots;
        }

        //Try stacking the hand slot. 
        //Check if the operation failed
        if (!StackItemToInventory(handSlot, inventoryToAlter))
        {
            //Find an empty slot to put the item in 
            //Iterate through each inventory slot and find an empty slot
            for (int i = 0; i < inventoryToAlter.Length; i++)
            {
                if (inventoryToAlter[i].IsEmpty())
                {
                    //Send the equipped item over to its new slot
                    inventoryToAlter[i] = new ItemSlotData(handSlot);
                    //Remove the item from the hand
                    handSlot.Empty();
                    break;
                }
            }

        }

        //Update the changes in the scene
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            RenderHand();
        }

        //Update the changes to the UI
        UIManager.Instance.RenderInventory();

       
    }

    //Iterate through each of the items in the inventory to see if it can be stacked
    //Will perform the operation if found, returns false if unsuccessful
    public bool StackItemToInventory(ItemSlotData itemSlot, ItemSlotData[] inventoryArray)
    {
        
        for (int i = 0; i < inventoryArray.Length; i++)
        {
            if (inventoryArray[i].Stackable(itemSlot))
            {
                //Add to the inventory slot's stack
                inventoryArray[i].AddQuantity(itemSlot.quantity);
                //Empty the item slot
                itemSlot.Empty();
                return true; 
            }
        }

        //Can't find any slot that can be stacked
        return false; 
    }

    //Handles movement of item from Shop to Inventory
    public void ShopToInventory(ItemSlotData itemSlotToMove)
    {
        //The inventory array to change
        ItemSlotData[] inventoryToAlter = IsTool(itemSlotToMove.itemData) ? toolSlots : itemSlots; 

        //Try stacking the hand slot. 
        //Check if the operation failed
        if (!StackItemToInventory(itemSlotToMove, inventoryToAlter))
        {
            //Find an empty slot to put the item in 
            //Iterate through each inventory slot and find an empty slot
            for (int i = 0; i < inventoryToAlter.Length; i++)
            {
                if (inventoryToAlter[i].IsEmpty())
                {
                    //Send the equipped item over to its new slot
                    inventoryToAlter[i] = new ItemSlotData(itemSlotToMove);
                    break;
                }
            }

        }

        //Update the changes to the UI
        UIManager.Instance.RenderInventory();
        RenderHand();
    }
    //Render the player's equipped item in the scene
    public void RenderHand()
    {
        //Reset objects on the hand
        if(handPoint.childCount > 0)
        {
            Destroy(handPoint.GetChild(0).gameObject);
        }

        //Check if the player has anything equipped
        if(SlotEquipped(InventorySlot.InventoryType.Item))
        {
            //Instantiate the game model on the player's hand and put it on the scene
            GameObject itemModel = Instantiate(GetEquippedSlotItem(InventorySlot.InventoryType.Item).gameModel, handPoint);
            //So the player doesnt interact with it
            if(itemModel.TryGetComponent(out Collider col))
            {
                Destroy(col); 
            }
        }
        
    }

    //Inventory Slot Data 
    #region Gets and Checks
    //Get the slot item (ItemData) 
    public ItemData GetEquippedSlotItem(InventorySlot.InventoryType inventoryType)
    {
        if(inventoryType == InventorySlot.InventoryType.Item)
        {
            return equippedItemSlot.itemData;
        }
        return equippedToolSlot.itemData; 
    }

    //Get function for the slots (ItemSlotData)
    public ItemSlotData GetEquippedSlot(InventorySlot.InventoryType inventoryType)
    {
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            return equippedItemSlot;
        }
        return equippedToolSlot;
    }

    //Get function for the inventory slots
    public ItemSlotData[] GetInventorySlots(InventorySlot.InventoryType inventoryType)
    {
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            return itemSlots;
        }
        return toolSlots;
    }

    //Check if a hand slot has an item
    public bool SlotEquipped(InventorySlot.InventoryType inventoryType)
    {
        if (inventoryType == InventorySlot.InventoryType.Item)
        {
            return !equippedItemSlot.IsEmpty();
        }
        return !equippedToolSlot.IsEmpty();
    }

    //Check if the item is a tool
    public bool IsTool(ItemData item)
    {
        //Is it equipment? 
        //Try to cast it as equipment first
        EquipmentData equipment = item as EquipmentData;
        if(equipment != null)
        {
            return true; 
        }

        //Is it a seed?
        //Try to cast it as a seed
        SeedData seed = item as SeedData;
        //If the seed is not null it is a seed 
        return seed != null; 

    }

    #endregion

    //Equip the hand slot with an ItemData (Will overwrite the slot)
    public void EquipHandSlot(ItemData item)
    {
        if (IsTool(item))
        {
            equippedToolSlot = new ItemSlotData(item); 
        } else
        {
            equippedItemSlot = new ItemSlotData(item); 
        }

    }

    //Equip the hand slot with an ItemSlotData (Will overwrite the slot)
    public void EquipHandSlot(ItemSlotData itemSlot)
    {
        //Get the item data from the slot 
        ItemData item = itemSlot.itemData;
        
        if (IsTool(item))
        {
            equippedToolSlot = new ItemSlotData(itemSlot);
        }
        else
        {
            equippedItemSlot = new ItemSlotData(itemSlot);
        }
    }

    public void ConsumeItem(ItemSlotData itemSlot)
    {
        if (itemSlot.IsEmpty())
        {
            Debug.LogError("There is nothing to consume!");
            return; 
        }

        //Use up one of the item slots
        itemSlot.Remove();
        //Refresh inventory
        RenderHand();
        UIManager.Instance.RenderInventory(); 
    }


    #region Inventory Slot Validation
    private void OnValidate()
    {
        //Validate the hand slots
        ValidateInventorySlot(equippedToolSlot);
        ValidateInventorySlot(equippedItemSlot);

        //Validate the slots in the inventoryy
        ValidateInventorySlots(itemSlots);
        ValidateInventorySlots(toolSlots);

    }
    
    //When giving the itemData value in the inspector, automatically set the quantity to 1 
    void ValidateInventorySlot(ItemSlotData slot)
    {
        if(slot.itemData != null && slot.quantity == 0)
        {
            slot.quantity = 1;
        }
    }

    //Validate arrays
    void ValidateInventorySlots(ItemSlotData[] array)
    {
        foreach (ItemSlotData slot in array)
        {
            ValidateInventorySlot(slot);
        }
    }
    #endregion

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

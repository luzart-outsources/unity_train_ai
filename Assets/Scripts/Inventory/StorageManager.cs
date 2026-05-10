using NUnit.Framework.Constraints;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using static InventorySlot;

public static class StorageManager
{

    const string STORAGE_PREFIX = "Storage_";
    const int CAPACITY = 12; 

    public static StorageContainer.StorageType StorageType
    {
        get; private set; 
    }
    /// <summary>
    /// The main entry into opening the storage, opens the UI. 
    /// </summary>
    public static void OpenStorage(StorageContainer.StorageType storageType)
    {
        StorageType = storageType;
        UIManager.Instance.OpenStorage(storageType);
    }

    public static ItemSlotData GetSlotData(int slotIndex)
    {
        return GetStorageList(StorageType)[slotIndex];
    }

    public static void TransferItems(int slotIndex, InventorySlot.InventoryType inventoryType)
    {
        //Storage to Inventory
        if (inventoryType == InventorySlot.InventoryType.Storage)
        {
            StorageToInventory(slotIndex);
            return; 
        }
        //Inventory to Storage
        InventoryToStorage(slotIndex); 

        
    }

    static void InventoryToStorage(int slotIndex)
    {
        //Get the inventory 
        ItemSlotData[] inventoryToAlter = GetRelevantPlayerInventory(StorageType);
        ItemSlotData slotToMove = inventoryToAlter[slotIndex];

        //If there's nothing to move
        if (slotToMove.IsEmpty()) return; 

        List<ItemSlotData> storageSlots = GetStorageList(StorageType);

        //Check if there is an existing place to stack it
        int index = storageSlots.FindIndex(x => x.Stackable(slotToMove));
        if (index >=0)
        {
            //Index found we can just stack it
            storageSlots[index].AddQuantity(slotToMove.quantity);
            slotToMove.Empty();
            
        } else
        {
            //No index found, have the first empty slot to be filled
            index = storageSlots.FindIndex(x => x.IsEmpty()); 
            if(index >=0)
            {
                storageSlots[index] = new ItemSlotData(slotToMove);
                slotToMove.Empty();
            }
        }

        UIManager.Instance.RenderInventory();

    }

    static void StorageToInventory(int slotIndex)
    {
        ItemSlotData storageSlot = GetSlotData(slotIndex);
        //If there's nothing to move
        if(storageSlot.IsEmpty()) return;

        ItemSlotData[] inventoryToAlter = GetRelevantPlayerInventory(StorageType); 

        //Try stacking the hand slot. 
        //Check if the operation failed
        if (!InventoryManager.Instance.StackItemToInventory(storageSlot, inventoryToAlter))
        {
            //Find an empty slot to put the item in 
            //Iterate through each inventory slot and find an empty slot
            for (int i = 0; i < inventoryToAlter.Length; i++)
            {
                if (inventoryToAlter[i].IsEmpty())
                {
                    //Send the equipped item over to its new slot
                    inventoryToAlter[i] = new ItemSlotData(storageSlot);
                    //Remove the item from the hand
                    storageSlot.Empty();
                    break;
                }
            }

        }


        //Update the changes to the UI
        UIManager.Instance.RenderInventory();
    }

    /// <summary>
    /// Set the slot data of the currently open storage container
    /// </summary>
    /// <param name="index">The index to change</param>
    /// <param name="slotData">The slot data</param>
    public static void SetSlotData(int index, ItemSlotData slotData)
    {
        GetStorageList(StorageType)[index] = new ItemSlotData(slotData);
    }

    

    public static void ClearSlot(int index)
    {
        GetStorageList(StorageType)[index].Empty();
    }

    /// <summary>
    /// Get direct reference to storage list from the blackboard
    /// </summary>
    public static List<ItemSlotData> GetStorageList(StorageContainer.StorageType storageType)
    {
        string storageID = STORAGE_PREFIX + storageType.ToString();
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        List<ItemSlotData> list = blackboard.GetOrInitList<ItemSlotData>(storageID);
        //Check if the list is empty
        if(list.Count == 0)
        {
            for(int i=0; i<CAPACITY; i++)
            {
                list.Add(new ItemSlotData(null, 0)); 
            }
        }
        

        return list; 
    }

    /// <summary>
    /// Get the relevant player inventory from the storage type
    /// </summary>
    /// <returns></returns>
    public static ItemSlotData[] GetRelevantPlayerInventory(StorageContainer.StorageType storageType)
    {
        //Tool slots for tool
        if (storageType == StorageContainer.StorageType.Tools) return InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Tool);
        
        //Item slots for everything else
        return InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Item); 
    }



    /// <summary>
    /// Check if the item in question can be stored in the storage
    /// </summary>
    public static bool CanStoreInType(ItemData item, StorageContainer.StorageType storageType)
    {
        switch (storageType)
        {
            case StorageContainer.StorageType.Tools:
                //Tools
                return (item is EquipmentData || item is SeedData);
            case StorageContainer.StorageType.Item:
                //Non-Food items
                return (!(item is EquipmentData || item is SeedData) && !(item is FoodData));
            case StorageContainer.StorageType.Food:
                //Food items only
                return (item is FoodData);
            default: 
                return false;
        }

    }
}

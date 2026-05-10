using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventorySaveState 
{
    public ItemSlotSaveData[] toolSlots;
    public ItemSlotSaveData[] itemSlots;

    public ItemSlotSaveData equippedItemSlot;
    public ItemSlotSaveData equippedToolSlot;

    public InventorySaveState(
        ItemSlotData[] toolSlots,
        ItemSlotData[] itemSlots,
        ItemSlotData equippedItemSlot,
        ItemSlotData equippedToolSlot
        )
    {
        this.toolSlots = ItemSlotData.SerializeArray(toolSlots);
        this.itemSlots = ItemSlotData.SerializeArray(itemSlots);
        this.equippedItemSlot = ItemSlotData.SerializeData(equippedItemSlot);
        this.equippedToolSlot = ItemSlotData.SerializeData(equippedToolSlot);
    }

    //Retrieve information from the current game for saving purposes
    public static InventorySaveState Export()
    {
        //Retrieve Inventory Data 
        ItemSlotData[] toolSlots = InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Tool);
        ItemSlotData[] itemSlots = InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Item);

        ItemSlotData equippedToolSlot = InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Tool);
        ItemSlotData equippedItemSlot = InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item);
        return new InventorySaveState(toolSlots, itemSlots, equippedItemSlot, equippedToolSlot); 
    }

    //Put the save data into the game
    public void LoadData()
    {
        ItemSlotData[] toolSlots = ItemSlotData.DeserializeArray(this.toolSlots);
        ItemSlotData equippedToolSlot = ItemSlotData.DeserializeData(this.equippedToolSlot);
        ItemSlotData[] itemSlots = ItemSlotData.DeserializeArray(this.itemSlots);
        ItemSlotData equippedItemSlot = ItemSlotData.DeserializeData(this.equippedItemSlot);
        InventoryManager.Instance.LoadInventory(toolSlots, equippedToolSlot, itemSlots, equippedItemSlot);
    }
}

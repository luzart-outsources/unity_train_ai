using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandInventorySlot : InventorySlot
{
    private void Awake()
    {
        //Hand slot is always -1
        slotIndex = -1; 
    }


    protected override void MoveEntireStack()
    {
        //Move item from hand to inventory
        InventoryManager.Instance.HandToInventory(inventoryType);
    }
}

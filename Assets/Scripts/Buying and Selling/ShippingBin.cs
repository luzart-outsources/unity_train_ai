using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShippingBin : InteractableObject
{
    public static int hourToShip = 18;
    public static List<ItemSlotData> itemsToShip = new List<ItemSlotData>();

    const string ITEMS_SHIPPED_KEY = "ItemsShipped";
    const string TOTAL_SHIPPED = "TotalItemsShipped"; 
    public override void Pickup()
    {
        //Get the item data of the item the player is trying to throw in
        ItemData handSlotItem = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Item);

        //If the player is not holding anything, nothing should happen
        if (handSlotItem == null) return; 

        //Open Yes No prompt to confirm if the player wants to sell
        UIManager.Instance.TriggerYesNoPrompt($"Do you want to sell {handSlotItem.name} ? ", PlaceItemsInShippingBin);
    }


    void PlaceItemsInShippingBin()
    {
        //Get the ItemSlotData of what the player is holding
        ItemSlotData handSlot = InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item);

        //Add the items to the itemsToShipList
        itemsToShip.Add(new ItemSlotData(handSlot));

        //Empty out the hand slot since it's moved to the shipping bin
        handSlot.Empty();

        //Update the changes
        InventoryManager.Instance.RenderHand();

        foreach(ItemSlotData item in itemsToShip)
        {
            Debug.Log($"In the shipping bin: {item.itemData.name} x {item.quantity}");
        }
    }

    public static void ShipItems()
    {
        //Calculate how much the player should receive upon shipping the items
        int moneyToReceive = TallyItems(itemsToShip);
        //Convert the items to money
        PlayerStats.Earn(moneyToReceive);
        //Empty the shipping bin
        itemsToShip.Clear(); 
    }

    static int TallyItems(List<ItemSlotData> items)
    {
        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        List<(string, int)> itemsShipped = blackboard.GetOrInitList<(string, int)>(ITEMS_SHIPPED_KEY);
        int total = 0;
        foreach(ItemSlotData item in items)
        {
            //Update the blackboard with new data
            (string, int) entry = itemsShipped.Find(x => x.Item1 == item.itemData.name);
            entry.Item2 += item.quantity; 
            blackboard.IncreaseValue(TOTAL_SHIPPED, item.quantity);


            //Get the item quantity and multiply by the cost value
            total += item.quantity * item.itemData.cost; 
        }
        blackboard.Debug(); 
        return total; 
    }
}

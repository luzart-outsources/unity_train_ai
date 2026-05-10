using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shop : InteractableObject
{
    [SerializeField]
    CharacterData owner;

    public List<ItemData> shopItems;
    

    [Header("Dialogues")]
    public List<DialogueLine> dialogueOnShopOpen;

    public static void Purchase(ItemData item, int quantity)
    {
        int totalCost = item.cost * quantity; 

        if(PlayerStats.Money >= totalCost)
        {
            //Deduct from the player's money
            PlayerStats.Spend(totalCost);
            //Create an ItemSlotData for the purchased item
            ItemSlotData purchasedItem = new ItemSlotData(item, quantity);

            //Send it to the player's inventory
            InventoryManager.Instance.ShopToInventory(purchasedItem); 
        }
    }

    public override void Pickup()
    {
        //Check if the store is manned
        if (!IsStoreManned()) return; 

        DialogueManager.Instance.StartDialogue(dialogueOnShopOpen, OpenShop);
        
    }

    bool IsStoreManned()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 4);
        foreach (Collider col in colliders)
        {
            if (col.tag != "Item") continue;

            InteractableCharacter characterInteractable = col.gameObject.GetComponent<InteractableCharacter>();
            if(characterInteractable == null) continue;
            if (characterInteractable.characterData.name == owner.name) return true; 
        }
        return false; 
    }

    void OpenShop()
    {
        UIManager.Instance.OpenShop(shopItems);
    }

    public override void OnHover()
    {
        //If store is unmanned there is nothing to interact with
        if (!IsStoreManned()) return; 
        base.OnHover();
    }
}

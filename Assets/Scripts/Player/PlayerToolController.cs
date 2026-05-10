using System.Collections;
using UnityEngine;

//Handles the overall logic of tool usage
[RequireComponent(typeof(Animator))]
public class PlayerToolController : MonoBehaviour
{
    //Whether the Player is in the middle of a tool usage operation
    bool lockedIn = false;
    //The land the operation will be performed in
    Land selectedLand = null;
    
    Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void StartToolUse(Land selectedLand)
    {
        //The player shouldn't be able to use his tool when he has his hands full with an item
        if (InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Item))
        {
            return;
        }
        //Detecting what is the current tool that the player is holding
        ItemData toolSlot = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Tool);

        if (toolSlot == null) { return; }
        if (lockedIn) return; 
        //Acquire the lock and lock in the selected land
        lockedIn = true;
        this.selectedLand = selectedLand;

       
        //We don't have animations for planting seeds for now so we can complete it here. 
        if(toolSlot is SeedData){
            CompleteInteraction(); 
            return;
        }

        
        EquipmentData equipmentTool = toolSlot as EquipmentData;
        
        
        //Play the animations
        EquipmentData.ToolType toolType = equipmentTool.toolType;
        //Consume stamina
        PlayerStats.UseStamina(2);
        switch (toolType)
        {
            case EquipmentData.ToolType.WateringCan:

                animator.SetTrigger("Watering");

                return;

            case EquipmentData.ToolType.Hoe:

                animator.SetTrigger("Plowing");
                return;

            case EquipmentData.ToolType.Axe:
                
                animator.SetTrigger("Axe");
                return;

            case EquipmentData.ToolType.Pickaxe:
                animator.SetTrigger("Pickaxe");
                return;

            case EquipmentData.ToolType.Shovel:
                animator.SetTrigger("Shovel");
                return;

            default:
                //We don't have animations for the rest so we just complete it
                CompleteInteraction();
                break; 
        }

        Debug.Log("Not on any land!");
    }


    //To be called via the animator
    public void CompleteInteraction()
    {

        if(selectedLand != null)
        {
            selectedLand.Interact();
        }
        selectedLand = null;
        lockedIn = false; 
    }
}

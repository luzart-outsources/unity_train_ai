using UnityEngine;

public class AnimEventsTools : MonoBehaviour
{
    public Transform handPoint;

    GameObject spawnedObject; 

    //Spawn the tool gameobject
    public void SpawnTool()
    {
        //If there is already an instantiated object 
        if(spawnedObject != null)
        {
            Destroy(spawnedObject);
        }
        //Get the equipment data
        EquipmentData equipmentData = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Tool) as EquipmentData;
        if(equipmentData == null) return;
        

        //Check if there is an item assigned
        if(equipmentData.gameModel != null)
        {
            //Spawn the object and cache it
            spawnedObject = Instantiate(equipmentData.gameModel, handPoint); 
        }
    }
    
    //Despawn the tool gameobject once animation completes
    public void Despawn()
    {
        //If theres nothing to despawn so be it
        if(spawnedObject == null) return;   
        //Destroy the spawned object
        Destroy(spawnedObject); 
    }

}

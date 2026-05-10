using System.Collections.Generic;
using UnityEngine;

public class StorageContainer : InteractableObject
{
    public enum StorageType
    {
        Food,
        Item, 
        Tools
    }
    [Header("Configuration")]
    [SerializeField] private StorageType storageType;
    [SerializeField] private int capacity;

    //Identifier for the storage container
    private string storageID;
    //Storage contents
    List<ItemSlotData> contents; 
    private void Start()
    {
        AssignStorageID(); 
    }

    

    /// <summary>
    /// Generate the storage id based on type of storage for use in the blackboard
    /// </summary>
    void AssignStorageID()
    {
        //This assumes player has 1 of each storage type 
        storageID = "STORAGE_" + storageType.ToString(); 

    }
    public override void Pickup()
    {
        StorageManager.OpenStorage(storageType);
        
    }
}

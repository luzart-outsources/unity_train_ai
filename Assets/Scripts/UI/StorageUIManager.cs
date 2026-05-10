using TMPro;
using UnityEngine;
using static InventorySlot;


public class StorageUIManager : MonoBehaviour
{
    const string TOOLBOX_DISPLAY = "Toolbox";
    const string ITEM_DISPLAY = "Cabinet";
    const string FOOD_DISPLAY = "Fridge"; 

    [Header("Variable Texts")]
    [SerializeField] TextMeshProUGUI heading;
    [SerializeField] TextMeshProUGUI inventoryHeading;
    [SerializeField] TextMeshProUGUI storageHeading;

    StorageContainer.StorageType storageType;

    [Header("Slots")]
    [SerializeField] InventorySlot[] inventorySlots; 
    [SerializeField] InventorySlot[] storageSlots;
    [SerializeField] SelectedItemDisplay itemCursor; 



    //[SerializeField] InventorySlot inventorySlotPrefab; 


    ItemSlotData[] playerInventorySlotData;
    ItemSlotData[] storageContentsSlotData; 


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    /// <summary>
    /// Render the inventory cursor
    /// </summary>
    public void RenderInventoryCursor()
    {
        itemCursor.Render(InventoryManager.Instance.GetSelected());
    }

    /// <summary>
    /// Load in the UI of the 
    /// </summary>
    /// <param name="storageType"></param>
    public void OpenStorage(StorageContainer.StorageType storageType)
    {
        this.storageType = storageType;
        RenderUI(); 
    }

    public void CloseStorage()
    {
        UIManager.Instance.CloseStorage();
    }

    /// <summary>
    /// Render the UI based on the storage type 
    /// </summary>
    public void RenderUI()
    {
        RenderHeadings(); 
        RenderPlayerInventory();
        RenderStorageContents();
        RenderInventoryCursor();
    }

    void RenderHeadings()
    {
        //Main heading
        heading.text = GetHeadingString();
        
        inventoryHeading.text = $"{GetInventoryType()} Inventory";
        storageHeading.text = $"{GetHeadingString()} Items";
    }

    

    void RenderPlayerInventory()
    {
        InventorySlot.InventoryType inventoryType = GetInventoryType();
        //Get the player inventory
        playerInventorySlotData = StorageManager.GetRelevantPlayerInventory(storageType);

        //Ensure the length tallies
        if(playerInventorySlotData.Length > inventorySlots.Length)
        {
            //We can implement pagination in future
            Debug.LogWarning("Warning: Inventory has more slots than available");
        }

        for(int i = 0; i<inventorySlots.Length; i++)
        {
            //Enable by default
            inventorySlots[i].enabled = true; 
            inventorySlots[i].AssignIndex(i);
            inventorySlots[i].inventoryType = inventoryType;
            inventorySlots[i].Display(playerInventorySlotData[i]);
            //Disable if not compatible, unless empty slot
            if (!StorageManager.CanStoreInType(playerInventorySlotData[i].itemData, storageType) && !playerInventorySlotData[i].IsEmpty())
            {
                inventorySlots[i].enabled = false;

            }
        }


        /*
        //Clear the slots
        InventorySlot[] slots = inventorySlotParent.GetComponentsInChildren<InventorySlot>();
        foreach (InventorySlot slot in slots)
        {
            Destroy(slot.gameObject); 
        }

        //Instantiate a new slot UI entry 
        for(int i = 0; i < playerInventorySlotData.Length; i++)
        {
            InventorySlot newSlot = Instantiate(inventorySlotPrefab, inventorySlotParent);
            newSlot.Display(playerInventorySlotData[i]);
            newSlot.AssignIndex(i);
            newSlot.inventoryType = inventoryType;

            //Disable if not compatible
            if (!StorageManager.CanStoreInType(playerInventorySlotData[i].itemData, storageType))
            {
                newSlot.enabled = false;

            }
        }*/
       


    }

    void RenderStorageContents()
    {
        //Get the storage slots
        storageContentsSlotData = StorageManager.GetStorageList(storageType).ToArray();



        //Ensure the length tallies
        if (storageContentsSlotData.Length > storageSlots.Length)
        {
            //We can implement pagination in future
            Debug.LogWarning("Warning: Storage Slots has more slots than available for UI Display");
        }

        for (int i = 0; i < storageSlots.Length; i++)
        {
            storageSlots[i].AssignIndex(i);
            storageSlots[i].inventoryType = InventoryType.Storage;
            storageSlots[i].Display(storageContentsSlotData[i]);
        }


        /*//Clear the list
        InventorySlot[] slots = storageSlotParent.GetComponentsInChildren<InventorySlot>();

        foreach(InventorySlot slot in slots)
        {
            Destroy(slot.gameObject);
        }

        //Instantiate a new slot UI entry 
        for (int i = 0; i < storageContentsSlotData.Length; i++)
        {
            InventorySlot newSlot = Instantiate(inventorySlotPrefab, storageSlotParent);
            newSlot.Display(storageContentsSlotData[i]);
            newSlot.AssignIndex(i);
            newSlot.inventoryType = InventorySlot.InventoryType.Storage;
        }*/

    }


    /// <summary>
    /// Get what inventory type we are dealing with to set the inventory slot accordingly
    /// </summary>
    /// <returns>The Inventory Slot InventoryType</returns>
    InventorySlot.InventoryType GetInventoryType()
    {
        if (storageType == StorageContainer.StorageType.Tools) return InventorySlot.InventoryType.Tool;
        return InventorySlot.InventoryType.Item; 
    }

    string GetHeadingString()
    {
        switch(storageType){
            case StorageContainer.StorageType.Item:
                return ITEM_DISPLAY; 
            case StorageContainer.StorageType.Tools: 
                return TOOLBOX_DISPLAY;
            case StorageContainer.StorageType.Food:
                return FOOD_DISPLAY;
            default:
                return ITEM_DISPLAY; 
            
        }
    }

}

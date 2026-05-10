using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 


public class UIManager : MonoBehaviour, ITimeTracker
{
    public static UIManager Instance { get; private set; }

    [Header("Screen Management")]
    public GameObject menuScreen;
    public enum Tab
    {
        Inventory, Relationships, Animals
    }
    //The current selected tab
    public Tab selectedTab;

    [Header("Status Bar")]
    //Tool equip slot on the status bar
    public Image toolEquipSlot;
    //Tool Quantity text on the status bar 
    public TextMeshProUGUI toolQuantityText;
    //Time UI
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI dateText;


    [Header("Inventory System")]
    //The inventory panel
    public GameObject inventoryPanel;

    //The tool equip slot UI on the Inventory panel
    public HandInventorySlot toolHandSlot;

    //The tool slot UIs
    public InventorySlot[] toolSlots;

    //The item equip slot UI on the Inventory panel
    public HandInventorySlot itemHandSlot;

    //The item slot UIs
    public InventorySlot[] itemSlots;

    [SerializeField] SelectedItemDisplay itemCursor;

    [SerializeField] StorageUIManager storageUIManager;

    [Header("Item info box")]
    public GameObject itemInfoBox;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;

    [Header("Screen Transitions")]
    public GameObject fadeIn;
    public GameObject fadeOut;

    [Header("Prompts")]
    public YesNoPrompt yesNoPrompt;
    public NamingPrompt namingPrompt;
    [SerializeField] InteractBubble interactBubble;

    [Header("Player Stats")]
    public TextMeshProUGUI moneyText;

    [Header("Shop")]
    public ShopListingManager shopListingManager;

    [Header("Relationships")]
    public RelationshipListingManager relationshipListingManager;
    public AnimalListingManager animalRelationshipListingManager;

    [Header("Calendar")]
    public CalendarUIListing calendar;

    [Header("Stamina")]
    public Sprite[] staminaUI;
    public Image StaminaUIImage;
    public int staminaCount;

    [Header("Weather")]
    public Sprite[] weatherUI;
    public Image WeatherUIImage;


    private void Awake()
    {
        //If there is more than one instance, destroy the extra
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            //Set the static instance to this instance
            Instance = this;
        }
    }

    private void Start()
    {
        PlayerStats.RestoreStamina();
        RenderInventory();
        AssignSlotIndexes();
        RenderPlayerStats();
        DisplayItemInfo(null);
        ChangeWeatherUI();

        //Add UIManager to the list of objects TimeManager will notify when the time updates
        TimeManager.Instance.RegisterTracker(this);
    }

    #region Prompts

    public void TriggerNamingPrompt(string message, System.Action<string> onConfirmCallback)
    {
        //Check if another prompt is already in progress
        if (namingPrompt.gameObject.activeSelf)
        {
            //Queue the prompt
            namingPrompt.QueuePromptAction(() => TriggerNamingPrompt(message, onConfirmCallback));
            return;
        }

        //Open the panel
        namingPrompt.gameObject.SetActive(true);

        namingPrompt.CreatePrompt(message, onConfirmCallback);
    }

    public void TriggerYesNoPrompt(string message, System.Action onYesCallback)
    {
        //Set active the gameobject of the Yes No Prompt
        yesNoPrompt.gameObject.SetActive(true);

        yesNoPrompt.CreatePrompt(message, onYesCallback);
    }
    #endregion

    #region Tab Management
    public void ToggleMenuPanel()
    {
        menuScreen.SetActive(!menuScreen.activeSelf);

        OpenWindow(selectedTab);

        TabBehaviour.onTabStateChange?.Invoke();
    }

    //Manage the opening of windows associated with the tab
    public void OpenWindow(Tab windowToOpen)
    {
        //Disable all windows
        relationshipListingManager.gameObject.SetActive(false);
        inventoryPanel.SetActive(false);
        animalRelationshipListingManager.gameObject.SetActive(false);

        //Open the corresponding window and render it
        switch (windowToOpen)
        {
            case Tab.Inventory:
                inventoryPanel.SetActive(true);
                RenderInventory();
                break;
            case Tab.Relationships:
                relationshipListingManager.gameObject.SetActive(true);
                relationshipListingManager.Render(RelationshipStats.relationships);
                break;
            case Tab.Animals:
                animalRelationshipListingManager.gameObject.SetActive(true);
                animalRelationshipListingManager.Render(AnimalStats.animalRelationships);
                break;

        }

        //Set the selected tab
        selectedTab = windowToOpen;


    }
    #endregion

    #region Fadein Fadeout Transitions

    public void FadeOutScreen()
    {
        fadeOut.SetActive(true);
    }

    public void FadeInScreen()
    {
        fadeIn.SetActive(true);
    }

    public void OnFadeInComplete()
    {
        //Disable Fade in Screen when animation is completed
        fadeIn.SetActive(false);
    }

    //Reset the fadein fadeout screens to their default positions
    public void ResetFadeDefaults()
    {
        fadeOut.SetActive(false);
        fadeIn.SetActive(true);
    }



    #endregion

    #region Inventory
    //Iterate through the slot UI elements and assign it its reference slot index
    public void AssignSlotIndexes()
    {
        for (int i = 0; i < toolSlots.Length; i++)
        {
            toolSlots[i].AssignIndex(i);
            itemSlots[i].AssignIndex(i);
        }
    }


    //Render the inventory screen to reflect the Player's Inventory. 
    public void RenderInventory()
    {
        //Also render the storageui where applicable
        storageUIManager.RenderUI();
        itemCursor.Render(InventoryManager.Instance.GetSelected());
        //Get the respective slots to process
        ItemSlotData[] inventoryToolSlots = InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Tool);
        ItemSlotData[] inventoryItemSlots = InventoryManager.Instance.GetInventorySlots(InventorySlot.InventoryType.Item);

        //Render the Tool section
        RenderInventoryPanel(inventoryToolSlots, toolSlots);

        //Render the Item section
        RenderInventoryPanel(inventoryItemSlots, itemSlots);

        //Render the equipped slots
        toolHandSlot.Display(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Tool));
        itemHandSlot.Display(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Item));

        //Get Tool Equip from InventoryManager
        ItemData equippedTool = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Tool);

        //Text should be empty by default
        toolQuantityText.text = "";
        //Check if there is an item to display
        if (equippedTool != null)
        {
            //Switch the thumbnail over
            toolEquipSlot.sprite = equippedTool.thumbnail;

            toolEquipSlot.gameObject.SetActive(true);

            //Get quantity 
            int quantity = InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Tool).quantity;
            if (quantity > 1)
            {
                toolQuantityText.text = quantity.ToString();
            }
            return;
        }
        
        toolEquipSlot.gameObject.SetActive(false);

        
    }

    //Iterate through a slot in a section and display them in the UI
    void RenderInventoryPanel(ItemSlotData[] slots, InventorySlot[] uiSlots)
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            //Display them accordingly
            uiSlots[i].Display(slots[i]);
        }
    }

    public void ToggleInventoryPanel()
    {
        //If the panel is hidden, show it and vice versa
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        
        RenderInventory();
    }

    //Display Item info on the Item infobox
    public void DisplayItemInfo(ItemData data)
    {
        //If data is null, reset
        if (data == null)
        {
            itemNameText.text = "";
            itemDescriptionText.text = "";
            itemInfoBox.SetActive(false);
            return;
        }
        itemInfoBox.SetActive(true);
        itemNameText.text = data.name;
        itemDescriptionText.text = data.description;
    }
    #endregion

    #region Time
    //Callback to handle the UI for time
    public void ClockUpdate(GameTimestamp timestamp)
    {
        //Handle the time
        //Get the hours and minutes
        int hours = timestamp.hour;
        int minutes = timestamp.minute;

        //AM or PM
        string prefix = "AM ";

        //Convert hours to 12 hour clock
        if (hours >= 12)
        {
            //Time becomes PM 
            prefix = "PM ";
            //12 PM and later
            hours = hours - 12;
            Debug.Log(hours);
        }
        //Special case for 12am/pm to display it as 12 instead of 0
        hours = hours == 0 ? 12 : hours;

        //Format it for the time text display
        timeText.text = prefix + hours + ":" + minutes.ToString("00");

        //Handle the Date
        int day = timestamp.day;
        string season = timestamp.season.ToString();
        string dayOfTheWeek = timestamp.GetDayOfTheWeek().ToString();
        string phase = TimeManager.Instance != null ? TimeManager.Instance.currentPhase.ToString() : "";

        //Format it for the date text display
        dateText.text = dayOfTheWeek + " - " + phase;

    }
    #endregion

    //Render the UI of the player stats in the HUD
    public void RenderPlayerStats()
    {
        if (StatsManager.Instance != null)
        {
            moneyText.text = "Kỷ Luật: " + StatsManager.Instance.GetStat(StatsManager.STAT_KYLUAT) + "đ";
        }
        else
        {
            moneyText.text = PlayerStats.Money + PlayerStats.CURRENCY;
        }
        
        staminaCount = PlayerStats.Stamina;
        ChangeStaminaUI();

    }

    //Open the shop window with the shop items listed
    public void OpenShop(List<ItemData> shopItems)
    {
        //Set active the shop window
        shopListingManager.gameObject.SetActive(true);
        shopListingManager.Render(shopItems);
    }

    /// <summary>
    /// Open the storage ui with the associated storage type
    /// </summary>
    public void OpenStorage(StorageContainer.StorageType storageType)
    {
        storageUIManager.gameObject.SetActive(true);
        storageUIManager.OpenStorage(storageType); 

    }

    public bool IsStoragePanelOpen()
    {
        return storageUIManager.gameObject.activeSelf;
    }

    public void CloseStorage()
    {
        storageUIManager.gameObject.SetActive(false);
    }

    public void ToggleRelationshipPanel()
    {
        GameObject panel = relationshipListingManager.gameObject;
        panel.SetActive(!panel.activeSelf);

        //If open, render the screen
        if (panel.activeSelf)
        {
            relationshipListingManager.Render(RelationshipStats.relationships);
        }
    }

    public void InteractPrompt(Transform item, string message, float offset)
    {
        interactBubble.gameObject.SetActive(true);
        interactBubble.transform.position = item.transform.position + new Vector3(0, offset, 0);
        interactBubble.Display(message);
    }

    public void DeactivateInteractPrompt()
    {
        interactBubble.gameObject.SetActive(false);
    }

    public void ChangeStaminaUI()
    {
        if (staminaCount <= 45) StaminaUIImage.sprite = staminaUI[3]; // exhausted
        else if (staminaCount <= 80) StaminaUIImage.sprite = staminaUI[2]; // tired
        else if (staminaCount <= 115) StaminaUIImage.sprite = staminaUI[1]; // active
        else if (staminaCount <= 150) StaminaUIImage.sprite = staminaUI[0]; // energised

    }

    public void ChangeWeatherUI()
    {
        var WeatherToday = WeatherManager.Instance.WeatherToday;
        if (WeatherToday == WeatherData.WeatherType.Sunny) { WeatherUIImage.sprite = weatherUI[0]; }
        else if (WeatherToday == WeatherData.WeatherType.Rain) { WeatherUIImage.sprite = weatherUI[1]; }
        else if (WeatherToday == WeatherData.WeatherType.Snow) { WeatherUIImage.sprite = weatherUI[2]; }
        else if (WeatherToday == WeatherData.WeatherType.HeavySnow) { WeatherUIImage.sprite = weatherUI[3]; }
    }
}
    



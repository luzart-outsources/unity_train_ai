using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.Events; 

public class GameStateManager : MonoBehaviour, ITimeTracker
{
    //lights that turn on at night
    public GameObject[] nightLights;
    public static GameStateManager Instance { get; private set; }

    //Check if the screen has finished fading out
    bool screenFadedOut;

    //To track interval updates
    private int minutesElapsed = 0;

    //Event triggered every 15 minutes
    public UnityEvent onIntervalUpdate;

    [SerializeField]
    GameBlackboard blackboard = new GameBlackboard();
    //This blackboard will not be saved 
    GameBlackboard sceneItemsBoard = new GameBlackboard();

    const string TIMESTAMP = "Timestamp";
    public GameBlackboard GetBlackboard()
    {
        return blackboard;
    }

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

    // Start is called before the first frame update
    void Start()
    {
        //Add this to TimeManager's Listener list
        TimeManager.Instance.RegisterTracker(this);
        SceneTransitionManager.Instance.onLocationLoad.AddListener(RenderPersistentObjects); 
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        UpdateShippingState(timestamp); 
        UpdateFarmState(timestamp);
        IncubationManager.UpdateEggs();
        blackboard.SetValue(TIMESTAMP, timestamp);

        if (timestamp.hour == 0 && timestamp.minute == 0)
        {
            OnDayReset(); 
        }

        //6:01 am, do rain on land logic
        if(timestamp.hour == 6 && timestamp.minute == 1) { 
            RainOnLand();
        
        }

        if(minutesElapsed >= 15)
        {
            minutesElapsed = 0;
            onIntervalUpdate?.Invoke();
            

        } 
        else
        {
            minutesElapsed++; 
        }

        // the lights will turn on at 6pm
        nightLights = GameObject.FindGameObjectsWithTag("Lights");
        foreach (GameObject i in nightLights)
        {
            if (timestamp.hour >= 18)
            {
                i.GetComponent<Light>().enabled = true;
            }
            else
            {
                i.GetComponent<Light>().enabled = false;
            }
        }

    }

    //Called when the day has been reset
    void OnDayReset()
    {
        Debug.Log("Day has been reset"); 
        foreach(NPCRelationshipState npc in RelationshipStats.relationships)
        {
            npc.hasTalkedToday = false;
            npc.giftGivenToday = false; 
        }
        AnimalFeedManager.ResetFeedboxes();
        AnimalStats.OnDayReset();

        //
    }

    void UpdateShippingState(GameTimestamp timestamp)
    {
        //Check if the hour is here (Exactly 1800 hours)
        if(timestamp.hour == ShippingBin.hourToShip && timestamp.minute == 0)
        {
            ShippingBin.ShipItems();
        }
    }

    void RainOnLand()
    {
        //Check if raining
        if (WeatherManager.Instance.WeatherToday != WeatherData.WeatherType.Rain) {
            return;
        }

        //Retrieve the Land and Farm data from the static variable
        List<LandSaveState> landData = LandManager.farmData.Item1;
        

        for(int i=0; i< landData.Count; i++)
        {
            //Set the last watered to now
            if (landData[i].landStatus != Land.LandStatus.Soil)
            {
                landData[i] = new LandSaveState(Land.LandStatus.Watered, TimeManager.Instance.GetGameTimestamp(), landData[i].obstacleStatus);

            }
        }
        
    }

    void UpdateFarmState(GameTimestamp timestamp)
    {
        //Update the Land and Crop Save states as long as the player is outside of the Farm scene
        if (SceneTransitionManager.Instance.currentLocation != SceneTransitionManager.Location.Farm)
        {
            //If there is nothing to update to begin with, stop 
            if (LandManager.farmData == null) return;

            //Retrieve the Land and Farm data from the static variable
            List<LandSaveState> landData = LandManager.farmData.Item1;
            List<CropSaveState> cropData = LandManager.farmData.Item2;

            //If there are no crops planted, we don't need to worry about updating anything
            if (cropData.Count == 0) return;

            for (int i = 0; i < cropData.Count; i++)
            {
                //Get the crop and corresponding land data
                CropSaveState crop = cropData[i];
                LandSaveState land = landData[crop.landID];

                //Check if the crop is already wilted
                if (crop.cropState == CropBehaviour.CropState.Wilted) continue;

                //Update the Land's state
                land.ClockUpdate(timestamp);
                //Update the crop's state based on the land state
                if (land.landStatus == Land.LandStatus.Watered)
                {
                    crop.Grow();
                }
                else if (crop.cropState != CropBehaviour.CropState.Seed)
                {
                    crop.Wither();
                }

                //Update the element in the array
                cropData[i] = crop;
                landData[crop.landID] = land;

            }

            /*LandManager.farmData.Item2.ForEach((CropSaveState crop) => {
                Debug.Log(crop.seedToGrow + "\n Health: " + crop.health + "\n Growth: " + crop.growth + "\n State: " + crop.cropState.ToString());
            });*/
        }
    }

    //Instantiate the GameObject such that it stays in the scene even after the player leaves 
    public GameObject PersistentInstantiate(GameObject gameObject, Vector3 position, Quaternion rotation)
    {
        GameObject item = Instantiate(gameObject, position, rotation);
        //Save it to the blackboard
        (GameObject, Vector3) itemInformation = (gameObject, position);
        List<(GameObject, Vector3)> items = sceneItemsBoard.GetOrInitList<(GameObject, Vector3)>(SceneTransitionManager.Instance.currentLocation.ToString());
        items.Add(itemInformation);
        sceneItemsBoard.Debug();
        return item;
    }

    public void PersistentDestroy(GameObject item)
    {
        if (sceneItemsBoard.TryGetValue(SceneTransitionManager.Instance.currentLocation.ToString(), out List<(GameObject, Vector3)> items))
        {
            int index = items.FindIndex(i => i.Item2 == item.transform.position);
            items.RemoveAt(index); 
        }
        Destroy(item); 
    }

    void RenderPersistentObjects()
    {
        Debug.Log("Rendering Persistent Objects");
        if (sceneItemsBoard.TryGetValue(SceneTransitionManager.Instance.currentLocation.ToString(), out List<(GameObject, Vector3)> items)) { 
            foreach((GameObject, Vector3) item in items)
            {
                Instantiate(item.Item1, item.Item2, Quaternion.identity); 
            }
        }
    }

    public void Sleep()
    {
        //Call a fadeout
        UIManager.Instance.FadeOutScreen();
        screenFadedOut = false;
        StartCoroutine(TransitionTime());
        //Restore Stamina fully when sleeping
        PlayerStats.RestoreStamina();
    }

    IEnumerator TransitionTime()
    {
        //Calculate how many ticks we need to advance the time to 6am

        //Get the time stamp of 6am the next day
        GameTimestamp timestampOfNextDay = TimeManager.Instance.GetGameTimestamp();
        timestampOfNextDay.day += 1;
        timestampOfNextDay.hour = 6;
        timestampOfNextDay.minute = 0;
        Debug.Log(timestampOfNextDay.day + " " + timestampOfNextDay.hour + ":" + timestampOfNextDay.minute);
        
        
        //Wait for the scene to finish fading out before loading the next scene
        while (!screenFadedOut)
        {
            yield return new WaitForSeconds(1f);
        }
        TimeManager.Instance.SkipTime(timestampOfNextDay);
        //Save
        SaveManager.Save(ExportSaveState());
        blackboard.Deserialise();
        //Reset the boolean
        screenFadedOut = false;
        UIManager.Instance.ResetFadeDefaults();

    }

    //Called when the screen has faded out
    public void OnFadeOutComplete()
    {
        screenFadedOut = true;

    }

    public GameSaveState ExportSaveState()
    {
        //Prepare blackboard for saving
        blackboard.Serialise(); 

        //Retrieve Farm Data 
        FarmSaveState farmSaveState = FarmSaveState.Export();

        //Retrieve inventory data 
        InventorySaveState inventorySaveState = InventorySaveState.Export();

        PlayerSaveState playerSaveState = PlayerSaveState.Export();

        //Time
        GameTimestamp timestamp = TimeManager.Instance.GetGameTimestamp();
        //Relationships
        //RelationshipSaveState relationshipSaveState = RelationshipSaveState.Export();

        WeatherSaveState weather = WeatherSaveState.Export();

        return new GameSaveState(blackboard, farmSaveState, inventorySaveState, timestamp, playerSaveState, weather);
    }

    public void LoadSave()
    {
        //Retrieve the loaded save
        GameSaveState save = SaveManager.Load();
        //Load up the parts
        
        blackboard = save.blackboard;
        blackboard.Deserialise(); 
        blackboard.Debug();

        //Time
        TimeManager.Instance.LoadTime(save.timestamp);

        //Inventory
        save.inventorySaveState.LoadData();

        //Farming data 
        save.farmSaveState.LoadData();

        //Player Stats
        save.playerSaveState.LoadData();

        //Weather
        save.weatherSaveState.LoadData();

        //Relationship stats
        AnimalStats.LoadStats();
        RelationshipStats.LoadStats(); 
        
        //save.relationshipSaveState.LoadData();

    }
}

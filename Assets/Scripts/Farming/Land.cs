using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Land : MonoBehaviour, ITimeTracker
{
    public int id; 
    public enum LandStatus
    {
        Soil, Farmland, Watered
    }

    public LandStatus landStatus;

    public Material soilMat, farmlandMat, wateredMat;
    new Renderer renderer;

    //The selection gameobject to enable when the player is selecting the land
    public GameObject select;

    //Cache the time the land was watered 
    GameTimestamp timeWatered;

    [Header("Crops")]
    //The crop prefab to instantiate
    public GameObject cropPrefab;

    //The crop currently planted on the land
    CropBehaviour cropPlanted = null;

    //Obstacles
    public enum FarmObstacleStatus { None, Rock, Wood, Weeds}
    [Header("Obstacles")]
    public FarmObstacleStatus obstacleStatus;
    public GameObject rockPrefab, woodPrefab, weedsPrefab;

    //Store the instantiated obstacle as a variable so we can access it 
    GameObject obstacleObject; 

    // Start is called before the first frame update
    void Start()
    {
        //Get the renderer component
        renderer = GetComponent<Renderer>();

        //Set the land to soil by default
        SwitchLandStatus(LandStatus.Soil);

        //Deselect the land by default
        Select(false);

        //Add this to TimeManager's Listener list
        TimeManager.Instance.RegisterTracker(this);


    }

    public void LoadLandData(LandStatus landStatusToSwitch, GameTimestamp lastWatered, FarmObstacleStatus obstacleStatusToSwitch)
    {
        //Set land status accordingly
        landStatus = landStatusToSwitch;
        timeWatered = lastWatered;

        Material materialToSwitch = soilMat;

        //Decide what material to switch to
        switch (landStatusToSwitch)
        {
            case LandStatus.Soil:
                //Switch to the soil material
                materialToSwitch = soilMat;
                break;
            case LandStatus.Farmland:
                //Switch to farmland material 
                materialToSwitch = farmlandMat;
                break;

            case LandStatus.Watered:
                //Switch to watered material
                materialToSwitch = wateredMat;
                break;

        }

        //Get the renderer to apply the changes
        renderer.material = materialToSwitch;

        switch (obstacleStatusToSwitch)
        {
            case FarmObstacleStatus.None:
                //Destroy the Obstacle object, if any
                if (obstacleObject != null) Destroy(obstacleObject);
                break;
            case FarmObstacleStatus.Rock:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(rockPrefab, transform);
                break;
            case FarmObstacleStatus.Wood:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(woodPrefab, transform);
                break;
            case FarmObstacleStatus.Weeds:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(weedsPrefab, transform);
                break;
        }

        //Move the obstacle object to the top of the land gameobject
        if (obstacleObject != null) obstacleObject.transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);

        //Set the status accordingly
        obstacleStatus = obstacleStatusToSwitch;

    }

    public void SwitchLandStatus(LandStatus statusToSwitch)
    {
        //Set land status accordingly
        landStatus = statusToSwitch;

        Material materialToSwitch = soilMat; 

        //Decide what material to switch to
        switch (statusToSwitch)
        {
            case LandStatus.Soil:
                //Switch to the soil material
                materialToSwitch = soilMat;
                break;
            case LandStatus.Farmland:
                //Switch to farmland material 
                materialToSwitch = farmlandMat;

                break;

            case LandStatus.Watered:
                //Switch to watered material
                materialToSwitch = wateredMat;

                //Cache the time it was watered
                timeWatered = TimeManager.Instance.GetGameTimestamp(); 
                break; 

        }

        //Get the renderer to apply the changes
        renderer.material = materialToSwitch;

        LandManager.Instance.OnLandStateChange(id, landStatus, timeWatered, obstacleStatus);
    }

    public void SetObstacleStatus(FarmObstacleStatus statusToSwitch)
    {
        switch (statusToSwitch)
        {
            case FarmObstacleStatus.None:
                //Destroy the Obstacle object, if any
                if (obstacleObject != null) Destroy(obstacleObject); 
                break;
            case FarmObstacleStatus.Rock:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(rockPrefab, transform); 
                break;
            case FarmObstacleStatus.Wood:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(woodPrefab, transform);
                break;
            case FarmObstacleStatus.Weeds:
                //Instantiate the obstacle prefab on the land and assign it to obstacleObject
                obstacleObject = Instantiate(weedsPrefab, transform);
                break; 
        }

        //Move the obstacle object to the top of the land gameobject
        if(obstacleObject != null) obstacleObject.transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);

        //Set the status accordingly
        obstacleStatus = statusToSwitch;

        LandManager.Instance.OnLandStateChange(id, landStatus, timeWatered, obstacleStatus);
    }

    public void Select(bool toggle)
    {
        select.SetActive(toggle);
    }

    //When the player presses the interact button while selecting this land
    public void Interact()
    {
        //Check the player's tool slot
        ItemData toolSlot = InventoryManager.Instance.GetEquippedSlotItem(InventorySlot.InventoryType.Tool);

        //If there's nothing equipped, return
        if (!InventoryManager.Instance.SlotEquipped(InventorySlot.InventoryType.Tool))
        {
            return; 
        }

        //Try casting the itemdata in the toolslot as EquipmentData
        EquipmentData equipmentTool = toolSlot as EquipmentData; 

        //Check if it is of type EquipmentData 
        if(equipmentTool != null)
        {
            //Get the tool type
            EquipmentData.ToolType toolType = equipmentTool.toolType;

            switch (toolType)
            {
                case EquipmentData.ToolType.Hoe:
                    SwitchLandStatus(LandStatus.Farmland);
                    break;
                case EquipmentData.ToolType.WateringCan:
                    //The land must be tilled first
                    if (landStatus != LandStatus.Soil)
                    {
                        SwitchLandStatus(LandStatus.Watered);
                    }
                    
                    break;

                case EquipmentData.ToolType.Shovel:

                    //Remove the crop from the land
                    if(cropPlanted != null)
                    {
                        cropPlanted.RemoveCrop();
                    }

                    //Remove weed obstacle
                    if (obstacleStatus == FarmObstacleStatus.Weeds) SetObstacleStatus(FarmObstacleStatus.None); 

                    break;

                case EquipmentData.ToolType.Axe:
                    //Remove wood obstacle
                    if (obstacleStatus == FarmObstacleStatus.Wood) SetObstacleStatus(FarmObstacleStatus.None);
                    break;

                case EquipmentData.ToolType.Pickaxe:
                    //Remove rock obstacle
                    if (obstacleStatus == FarmObstacleStatus.Rock) SetObstacleStatus(FarmObstacleStatus.None);
                    break;

            }

            //We don't need to check for seeds if we have already confirmed the tool to be an equipment
            return; 
        }

        //Try casting the itemdata in the toolslot as SeedData
        SeedData seedTool = toolSlot as SeedData; 

        ///Conditions for the player to be able to plant a seed
        ///1: He is holding a tool of type SeedData
        ///2: The Land State must be either watered or farmland
        ///3. There isn't already a crop that has been planted
        ///4. There are no obstacles
        if(seedTool != null && landStatus != LandStatus.Soil && cropPlanted == null && obstacleStatus == FarmObstacleStatus.None)
        {
            SpawnCrop();
            //Plant it with the seed's information
            cropPlanted.Plant(id, seedTool);

            //Consume the item
            InventoryManager.Instance.ConsumeItem(InventoryManager.Instance.GetEquippedSlot(InventorySlot.InventoryType.Tool));

        }
    }

    public CropBehaviour SpawnCrop()
    {
        //Instantiate the crop object parented to the land
        GameObject cropObject = Instantiate(cropPrefab, transform);
        //Move the crop object to the top of the land gameobject
        cropObject.transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);

        //Access the CropBehaviour of the crop we're going to plant
        cropPlanted = cropObject.GetComponent<CropBehaviour>();
        return cropPlanted; 
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {

        //When raining, have it watered if it is farmland
        if (WeatherManager.Instance.WeatherToday == WeatherData.WeatherType.Rain && landStatus == LandStatus.Farmland)
        {
            SwitchLandStatus(LandStatus.Watered);

        }

        //Checked if 24 hours has passed since last watered
        if (landStatus == LandStatus.Watered)
        {
            //Hours since the land was watered
            int hoursElapsed = GameTimestamp.CompareTimestamps(timeWatered, timestamp);
            Debug.Log(hoursElapsed + " hours since this was watered");

            //Grow the planted crop, if any
            if(cropPlanted != null)
            {
                cropPlanted.Grow();
            }

            if(hoursElapsed > 24)
            {
                //Dry up (Switch back to farmland)
                SwitchLandStatus(LandStatus.Farmland);
            }
        }

        //Handle the wilting of the plant when the land is not watered
        if(landStatus != LandStatus.Watered && cropPlanted != null)
        {
            //If the crop has already germinated, start the withering
            if (cropPlanted.cropState != CropBehaviour.CropState.Seed)
            {
                cropPlanted.Wither();
            }
        }
    }

    private void OnDestroy()
    {
        //Unsubscribe from the list on destroy
        TimeManager.Instance.UnregisterTracker(this);
    }
}

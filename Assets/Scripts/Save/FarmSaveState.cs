using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
public class FarmSaveState 
{
    public List<LandSaveState> landData;
    public List<CropSaveState> cropData;
    public List<EggIncubationSaveState> eggsIncubating;

    public FarmSaveState(
        List<LandSaveState> landData,
        List<CropSaveState> cropData,
        List<EggIncubationSaveState> eggsIncubating
        )
    {
        this.landData = landData;
        this.cropData = cropData;
        this.eggsIncubating = eggsIncubating;
    }

    //Prepare the data for exporting 
    public static FarmSaveState Export()
    {

        List<LandSaveState> landData = new List<LandSaveState>();
        List<CropSaveState> cropData = new List<CropSaveState>(); 
        if (LandManager.farmData != null)
        {
            landData = LandManager.farmData.Item1;
            cropData = LandManager.farmData.Item2;
        }
        List<EggIncubationSaveState> eggsIncubating = new List<EggIncubationSaveState>(); 
        if(IncubationManager.eggsIncubating != null)
        {
            eggsIncubating = IncubationManager.eggsIncubating;
        }
        return new FarmSaveState(landData, cropData, eggsIncubating); 

    }

    //Load in the data into LandManager
    public void LoadData()
    {
        LandManager.farmData = new System.Tuple<List<LandSaveState>, List<CropSaveState>>(landData, cropData);
        IncubationManager.eggsIncubating = eggsIncubating;
    }
}

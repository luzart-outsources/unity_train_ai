using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LandSaveState 
{
    public Land.LandStatus landStatus;
    public GameTimestamp lastWatered;
    public Land.FarmObstacleStatus obstacleStatus; 

    public LandSaveState(Land.LandStatus landStatus, GameTimestamp lastWatered, Land.FarmObstacleStatus obstacleStatus)
    {
        this.landStatus = landStatus;
        this.lastWatered = lastWatered;
        this.obstacleStatus = obstacleStatus; 
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        //Checked if 24 hours has passed since last watered
        if (landStatus == Land.LandStatus.Watered)
        {
            //Hours since the land was watered
            int hoursElapsed = GameTimestamp.CompareTimestamps(lastWatered, timestamp);
            Debug.Log(hoursElapsed + " hours since this was watered");

            if (hoursElapsed > 24)
            {
                //Dry up (Switch back to farmland)
                landStatus = Land.LandStatus.Farmland; 
            }
        }

        
    }
}

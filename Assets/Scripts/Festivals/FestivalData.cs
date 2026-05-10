using SoCollection;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FestivalData", menuName = "Festival/FestivalData")]
public class FestivalData : ScriptableObject
{
    public string displayName;
    public GameTimestamp date;
    //To be displayed on the Calendar 
    public Sprite displayIcon;

    public SoCollection<FestivalLocation> locationConfiguration;

    public FestivalLocationLock[] locks;
    //Lock the player out of indoor locations in the town that do not belong to the player
    public bool lockIndoorLocations = true;

    public bool activateTownSquareDecor = true;

    public List<FestivalLocation> LocationConfiguration()
    {
        //Convert to list
        List<FestivalLocation> locations = new List<FestivalLocation>(locationConfiguration);
        return locations; 

    }
}

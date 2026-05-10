using UnityEngine;

[System.Serializable]
public class FestivalLocationLock
{
    public SceneTransitionManager.Location locationToLock; 

    //The time interval of the lock
    public int startHour;
    public int endHour; 
    public int startMinute;
    public int endMinute;

   

    

}

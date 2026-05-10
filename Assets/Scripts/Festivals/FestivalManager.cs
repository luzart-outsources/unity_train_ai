using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FestivalManager : MonoBehaviour, ITimeTracker
{
    public static FestivalManager Instance;

    List<FestivalData> festivals;

    FestivalNPCHandler npcHandler; 

    //Whether a festival is underway
    bool festivalInProgress = false; 
    //Current festival in progress
    FestivalData festival = null;

   
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

        LoadFestivals();
    }

    private void Start()
    {
        TimeManager.Instance.RegisterTracker(this);
        SceneTransitionManager.Instance.onLocationLoad.AddListener(ManageFestivalScene);
        npcHandler = new FestivalNPCHandler();

    }

    void LoadFestivals()
    {
        festivals = Resources.LoadAll<FestivalData>("Festivals").ToList();
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        //Check for festivals at 6:01am every day (To ensure it works even after loading save)
        if (timestamp.hour == 6 && timestamp.minute == 1) {
            //Reset
            festivalInProgress = false;
            festival = GetFestivalByDay(timestamp);
            if(festival != null)
            {
                festivalInProgress = true; 
            }

            EngageManagerService();
            Debug.Log(festivalInProgress? "Festival is " +  festival: "No festival today");

        }

        //Minute by minute update locks
        if (festivalInProgress)
        {
            SetLocks();
        }

    }

    /// <summary>
    /// Determine if certain services should pause or resume their default behaviour
    /// Affected: NPCManager and CutsceneManager
    /// </summary>
    void EngageManagerService()
    {
        if (festivalInProgress)
        {
            NPCManager.Instance.Pause();
            CutsceneManager.Instance.Pause(true);

        } else
        {
            NPCManager.Instance.Continue();
            CutsceneManager.Instance.Pause(false);
        }
    }

    /// <summary>
    /// Configure locks based on festival config
    /// </summary>
    void SetLocks()
    {
        //Unlock all 
        LocationManager.Instance.UnlockAllLocations();
        if (!festivalInProgress)
        {
            return;
        }

        //Lock indoors
        if (festival.lockIndoorLocations)
        {
            LocationManager.Instance.LockIndoorLocations();
        }

        if (festival.locks.Length > 0)
        {
            GameTimestamp time = TimeManager.Instance.GetGameTimestamp();
            int nowVal = GameTimestamp.TimestampInMinutes(time);
            foreach (FestivalLocationLock l in festival.locks)
            {

                //Create the hypothetical timestamps
                GameTimestamp start = TimeManager.Instance.GetGameTimestamp();
                start.hour = l.startHour;
                start.minute = l.startMinute;
                int startVal = GameTimestamp.TimestampInMinutes(start);

                GameTimestamp end = TimeManager.Instance.GetGameTimestamp();
                end.hour = l.endHour;
                end.minute = l.endMinute;
                int endVal = GameTimestamp.TimestampInMinutes(end);


                //Perform the checks
                if (nowVal >= startVal && nowVal <= endVal)
                {
                    //Lock it
                    LocationManager.Instance.LockLocation(l.locationToLock, true);
                }



            }

        }



    }

    public FestivalData GetFestivalByDay(GameTimestamp timestamp)
    {
        //Run the festival checking logic
        return festivals.Find(x => x.date.day == timestamp.day && x.date.season == timestamp.season);
    }

    
    
    /// <summary>
    /// Festival logic for when the scene loads
    /// </summary>
    public void ManageFestivalScene()
    {
        //If there is no festival going on there is no need. 
        if (!festivalInProgress) return;
        EngageManagerService(); 
        SetLocks();
        
        npcHandler.LoadFestivalData(festival); 

    }







}

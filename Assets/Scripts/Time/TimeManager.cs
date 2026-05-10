using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Internal Clock")]
    [SerializeField]
    GameTimestamp timestamp;
    public float timeScale = 1.0f;

    public enum DayPhase
    {
        Morning,    // 05:00 - 11:30
        Noon,       // 11:30 - 14:00
        Afternoon,  // 14:00 - 18:00
        Evening,    // 18:00 - 21:00
        Night       // 21:00 - 05:00 (Sleep)
    }
    public DayPhase currentPhase = DayPhase.Morning;

    [Header ("Day and Night cycle")]
    //The transform of the directional light (sun)
    public Transform sunTransform;
    private float indoorAngle = 40; 

    //List of Objects to inform of changes to the time
    List<ITimeTracker> listeners = new List<ITimeTracker>();

    public bool TimeTicking { get; set; }

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
        //Initialise the time stamp
        // Bắt đầu game vào lúc 05:00 sáng
        timestamp = new GameTimestamp(0, GameTimestamp.Season.Spring, 1, 5, 0);
        TimeTicking = true;
        StartCoroutine(TimeUpdate());

    }

    //Load the time from a save 
    public void LoadTime(GameTimestamp timestamp)
    {
        this.timestamp = new GameTimestamp(timestamp);
    }

    IEnumerator TimeUpdate()
    {
        while (true)
        {
            if (TimeTicking)
            {
                Tick();
            }
            
            yield return new WaitForSeconds(1 / timeScale);
        }
       
    }
    

    //A tick of the in-game time
    public void Tick()
    {
        timestamp.UpdateClock();

        UpdateDayPhase();

        // Enforce 21:00 Bedtime
        if (timestamp.hour >= 21)
        {
            SleepToNextDay();
            return;
        }

        //Inform each of the listeners of the new time state 
        foreach(ITimeTracker listener in listeners)
        {
            listener.ClockUpdate(timestamp);
        }

        UpdateSunMovement(); 
    }

    void UpdateDayPhase()
    {
        float timeInHours = timestamp.hour + (timestamp.minute / 60f);

        if (timeInHours >= 5f && timeInHours < 11.5f) currentPhase = DayPhase.Morning;
        else if (timeInHours >= 11.5f && timeInHours < 14f) currentPhase = DayPhase.Noon;
        else if (timeInHours >= 14f && timeInHours < 18f) currentPhase = DayPhase.Afternoon;
        else if (timeInHours >= 18f && timeInHours < 21f) currentPhase = DayPhase.Evening;
        else currentPhase = DayPhase.Night;
    }

    public void SleepToNextDay()
    {
        // Add Auto-save here later
        Debug.Log("21:00 - Tắt đèn đi ngủ! Auto-saving...");
        
        // Skip to 05:00 next day
        timestamp.day += 1;
        timestamp.hour = 5;
        timestamp.minute = 0;
        
        // Force update listeners
        foreach(ITimeTracker listener in listeners)
        {
            listener.ClockUpdate(timestamp);
        }
        UpdateSunMovement();
    }

    public void SkipTime(GameTimestamp timeToSkipTo)
    {
        //Convert to minutes
        int timeToSkipInMinutes = GameTimestamp.TimestampInMinutes(timeToSkipTo);
        Debug.Log("Time to skip to:" + timeToSkipInMinutes);
        int timeNowInMinutes = GameTimestamp.TimestampInMinutes(timestamp);
        Debug.Log("Time now: " + timeNowInMinutes);

        int differenceInMinutes = timeToSkipInMinutes - timeNowInMinutes;
        Debug.Log(differenceInMinutes + " minutes will be advanced");

        //Check if the timestamp to skip to has already been reached
        if (differenceInMinutes <= 0) return; 

        for(int i = 0; i<differenceInMinutes; i++)
        {
            Tick(); 
        }
    }

    //Day and night cycle
    void UpdateSunMovement()
    {

        //Disable Day and Night cycle if indoors
        if (SceneTransitionManager.Instance.CurrentlyIndoor())
        {
            sunTransform.eulerAngles = new Vector3(indoorAngle, 0, 0);
            return; 
        }

        //Convert the current time to minutes
        int timeInMinutes = GameTimestamp.HoursToMinutes(timestamp.hour) + timestamp.minute;

        //Sun moves 15 degrees in an hour
        //.25 degrees in a minute
        //At midnight (0:00), the angle of the sun should be -90
        float sunAngle = .25f * timeInMinutes - 90;

        //Apply the angle to the directional light
        sunTransform.eulerAngles = new Vector3(sunAngle, 0, 0);
    }

    //Get the timestamp
    public GameTimestamp GetGameTimestamp()
    {
        //Return a cloned instance
        return new GameTimestamp(timestamp);
    }


    //Handling Listeners

    //Add the object to the list of listeners
    public void RegisterTracker(ITimeTracker listener)
    {
        listeners.Add(listener);
    }

    //Remove the object from the list of listeners
    public void UnregisterTracker(ITimeTracker listener)
    {
        listeners.Remove(listener);
    }


}

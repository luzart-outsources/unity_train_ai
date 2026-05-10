using UnityEngine;
[System.Serializable]
public struct ScheduleEvent 
{
    public string name;
    [Header("Conditions")]
    public GameTimestamp time;
    public GameTimestamp.DayOfTheWeek dayOfTheWeek; 
    //So we can override schedules for special occasions, etc. 
    public int priority;

    //By default, only the Season, Hour, Minute, DayOfWeek are considered
    //Check this if the exact date is a condition (Example usecase: New Year festivals)
    public bool factorDate;
    //For conditions that do not take into account the day of the week
    public bool ignoreDayOfTheWeek; 

    [Header("Position")]
    public SceneTransitionManager.Location location;
    public Vector3 coord;
    public Vector3 facing; 
}

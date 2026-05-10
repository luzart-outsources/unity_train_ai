using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; 

public class CalendarUIListing : MonoBehaviour
{
    [SerializeField]
    List<CalendarEntry> entries;
    GameTimestamp timestamp;
    [SerializeField]
    TextMeshProUGUI calendarHeader;  
    // Start is called before the first frame update
    void Start()
    {
        //RenderCalendar(TimeManager.Instance.GetGameTimestamp());
    }

    public void RenderCalendar(GameTimestamp timestamp)
    {
        this.timestamp = timestamp;
        calendarHeader.text = "Year "+ timestamp.year +" " + timestamp.season.ToString();

        //Get the day of the first day of the season
        GameTimestamp seasonsTime = new GameTimestamp(timestamp.year, timestamp.season, 1, 0, 0);
        int dayOfWeek = (int) seasonsTime.GetDayOfTheWeek();
        //Adjust the day value so that Sunday will be of index 0
        dayOfWeek = (dayOfWeek + 6) % 7;
        Debug.Log("Day value: " + dayOfWeek);

        int entryIndex = 0;
        if(dayOfWeek != 0)
        {
            //Clear the empty entries before the first day of the week
            for (entryIndex = 0; entryIndex < dayOfWeek; entryIndex++)
            {
                entries[entryIndex].EmptyEntry();
            }
        }

        int lastDay = entryIndex + 30; 
        //Loop through all the dates of the season
        for(; entryIndex < lastDay; entryIndex++)
        {
            entries[entryIndex].season = timestamp.season;

            //Event checking
            //Check for birthday
            CharacterData charactersBirthday = RelationshipStats.WhoseBirthday(seasonsTime);
            //Handle the birthday
            if(charactersBirthday != null)
            {
                entries[entryIndex].Display(seasonsTime.day, seasonsTime.GetDayOfTheWeek(), charactersBirthday.portrait, charactersBirthday.name +"'s birthday");
            } else
            {
                entries[entryIndex].Display(seasonsTime.day, seasonsTime.GetDayOfTheWeek());
            }

            
            //Move to the next day
            seasonsTime.hour = 23;
            seasonsTime.minute = 59;
            seasonsTime.UpdateClock(); 
        }

        //Clear our the remaining
        for(; entryIndex < entries.Count; entryIndex++)
        {
            entries[entryIndex].EmptyEntry();
        }

        
    }

    public void NextSeason()
    {
        GameTimestamp nextSeason = timestamp;
        timestamp.day = 30;
        timestamp.hour = 23;
        timestamp.minute = 59; 
        timestamp.UpdateClock();
        RenderCalendar(nextSeason);
    }

    public void PrevSeason()
    {
        GameTimestamp prevSeason = timestamp;
        if(timestamp.season == GameTimestamp.Season.Spring)
        {
            timestamp.year--;
            timestamp.season = GameTimestamp.Season.Winter;
        } else
        {
            timestamp.season--;
        }
        RenderCalendar(prevSeason);
    }
}

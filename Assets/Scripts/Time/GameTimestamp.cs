using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameTimestamp 
{
    public int year; 
    public enum Season
    {
        Spring,
        Summer,
        Fall,
        Winter
    }
    public Season season;

    public enum DayOfTheWeek
    {
        Saturday,
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday
        
    }

    public int day; 
    public int hour;
    public int minute; 

    //Constructor to set up the class
    public GameTimestamp(int year, Season season, int day, int hour, int minute)
    {
        this.year = year;
        this.season = season;
        this.day = day;
        this.hour = hour;
        this.minute = minute;
    }

    //Creating a new instance of a GameTimestamp from another pre-existing one
    public GameTimestamp(GameTimestamp timestamp)
    {
        this.year = timestamp.year;
        this.season = timestamp.season;
        this.day = timestamp.day;
        this.hour = timestamp.hour;
        this.minute = timestamp.minute;
    }

    //Increments the time by 1 minute
    public void UpdateClock()
    {
        minute++; 

        //60 minutes in 1 hour
        if(minute >= 60)
        {
            //reset minutes
            minute = 0;
            hour++;
        }

        //24 hours in 1 day
        if(hour >= 24)
        {
            //Reset hours 
            hour = 0;

            day++; 
        }

        //30 days in a season
        if(day > 30)
        {
            //Reset days
            day = 1;

            //If at the final season, reset and change to spring
            if(season == Season.Winter)
            {
                season = Season.Spring;
                //Start of a new year
                year++; 
            } else
            {

                season++;
            }

            
            
        }
    }
    public DayOfTheWeek DayOfWeek
    {
        get { return GetDayOfTheWeek(); }
    }
    public DayOfTheWeek GetDayOfTheWeek()
    {
        //Convert the total time passed into days
        int daysPassed = YearsToDays(year) + SeasonsToDays(season) + day;

        //Remainder after dividing daysPassed by 7
        int dayIndex =  daysPassed % 7;

        //Cast into Day of the Week
        return (DayOfTheWeek)dayIndex;
    }

    //Convert hours to minutes
    public static int HoursToMinutes(int hour)
    {
        //60 minutes = 1 hour
        return hour * 60; 
    }

    //Convert Days to Hours
    public static int DaysToHours(int days)
    {
        //24 Hours in a day
        return days * 24; 
    }

    //Convert Seasons to days
    public static int SeasonsToDays(Season season)
    {
        int seasonIndex = (int)season;
        return seasonIndex * 30; 
    }

    //Years to Days
    public static int YearsToDays(int years)
    {
        return years * 4 * 30; 
    }

    //The entire timestamp in minutes
    public static int TimestampInMinutes(GameTimestamp timestamp)
    {
        return HoursToMinutes(DaysToHours(YearsToDays(timestamp.year)) + DaysToHours(SeasonsToDays(timestamp.season)) + DaysToHours(timestamp.day) + timestamp.hour) + timestamp.minute; 
    }

    // Calculate the difference between 2 timestamps in hours
    public static int CompareTimestamps(GameTimestamp timestamp1, GameTimestamp timestamp2)
    {
        //Convert timestamps to hours
        int timestamp1Hours = DaysToHours(YearsToDays(timestamp1.year)) + DaysToHours(SeasonsToDays(timestamp1.season)) + DaysToHours(timestamp1.day) + timestamp1.hour;
        int timestamp2Hours = DaysToHours(YearsToDays(timestamp2.year)) + DaysToHours(SeasonsToDays(timestamp2.season)) + DaysToHours(timestamp2.day) + timestamp2.hour;
        int difference = timestamp2Hours - timestamp1Hours;
        return Mathf.Abs(difference);
    }

   
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class CalendarEntry : MonoBehaviour
{
    //The date
    [SerializeField]
    TextMeshProUGUI dateText;
    [SerializeField]
    Image icon;
    //The colour of the entry
    Image entry;
    //The colours of the day
    [SerializeField]
    Color weekday, sat, sun, today;
    public GameTimestamp.Season season; 
    string eventDescription;
    
    // Start is called before the first frame update
    void OnEnable()
    {
        icon.gameObject.SetActive(false);
        entry = GetComponent<Image>();
    }

    //For days with special events
    public void Display(int date, GameTimestamp.DayOfTheWeek day, Sprite eventSprite, string eventDescription)
    {
        dateText.text = date.ToString();
        Color colorToSet = weekday; 
        switch (day)
        {
            case GameTimestamp.DayOfTheWeek.Saturday:
                colorToSet = sat;
                break;
            case GameTimestamp.DayOfTheWeek.Sunday:
                colorToSet = sun;
                break; 
            default:
                colorToSet = weekday;
                break; 
        }
        //Check if it's today
        GameTimestamp today = TimeManager.Instance.GetGameTimestamp(); 
        if(date == today.day && today.season == season)
        {
            colorToSet = this.today; 
        }


        entry.color = colorToSet;

        if(eventSprite != null)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = eventSprite; 
        } else
        {
            icon.gameObject.SetActive(false);
        }
        this.eventDescription = eventDescription;

    }

    //For normal days
    public void Display(int date, GameTimestamp.DayOfTheWeek day)
    {
        
        Display(date, day, null, "Just an ordinary day");
        
    }

    //For null entries
    public void EmptyEntry()
    {
        entry.color = Color.clear; 
        dateText.text = "";
        icon.gameObject.SetActive(false);
    }
}

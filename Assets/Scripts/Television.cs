using UnityEngine;

public class Television : InteractableObject
{

    public override void Pickup()
    {
        //For now, just do a weather forecast
        WeatherData.WeatherType weather = WeatherManager.Instance.WeatherTomorrow;
        DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage(
            "The weather tomorrow is " + weather.ToString()
            ));;
    }
}

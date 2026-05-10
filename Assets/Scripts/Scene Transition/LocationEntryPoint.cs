using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationEntryPoint : MonoBehaviour
{
    [SerializeField]
    SceneTransitionManager.Location locationToSwitch;

    [SerializeField]
    //For locking doors
    bool locked = false; 

    private void OnTriggerEnter(Collider other)
    {
        //Update lock value 
        locked = LocationManager.Instance.LocationLocked(locationToSwitch); 

        //Check if the collider belongs to the player
        if(other.tag == "Player")
        {
            if(WeatherManager.Instance.WeatherToday == WeatherData.WeatherType.Typhoon && SceneTransitionManager.Instance.currentLocation == SceneTransitionManager.Location.PlayerHome)
            {
                //You cant go out in a typhoon
                DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage("It's not safe to go out"));
                return;
            }

            if (locked)
            {
                DialogueManager.Instance.StartDialogue(DialogueManager.CreateSimpleMessage("You can't enter this place at this time."));
                return; 
            }

            //Switch scenes to the location of the entry point
            SceneTransitionManager.Instance.SwitchLocation(locationToSwitch);
        }

        //Characters walking through here and items thrown will be despawned
        if(other.tag == "Item")
        {
            Destroy(other.gameObject);
        }
        
    }



}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events; 

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    //The blackboard key for location
    const string LOCATION_KEY = "Location";
    const string INDOORS = "CurrentlyIndoor"; 

    //The scenes the player can enter
    public enum Location { Farm, PlayerHome, Town, YodelRanch, ChickenCoop, Forest, Inn, TownSquare }
    public Location currentLocation;

    //List of all the places that are to be considered indoor
    public static readonly Location[] indoor = { Location.PlayerHome, Location.ChickenCoop, Location.YodelRanch, Location.Inn };
    //List of all indoor places that belong to the player
    public static readonly Location[] playerProperties = { Location.PlayerHome, Location.ChickenCoop }; 
    //The player's transform
    Transform playerPoint;
    
    //Check if the screen has finished fading out
    bool screenFadedOut;

    public UnityEvent onLocationLoad; 

    private void Awake()
    {
        //If there is more than 1 instance, destroy GameObject
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            //Set the static instance to this instance
            Instance = this;
        }

        //Make the gameobject persistent across scenes
        DontDestroyOnLoad(gameObject);

        //OnLocationLoad will be called when the scene is loaded
        SceneManager.sceneLoaded += OnLocationLoad;

        //Find the player's transform
        playerPoint = FindObjectOfType<PlayerController>().transform;
    }

    //Checks if the current location is indoors
    public bool CurrentlyIndoor()
    {
        return indoor.Contains(currentLocation); 
    }

    //Switch the player to another scene
    public void SwitchLocation(Location locationToSwitch)
    {
        //Call a fadeout
        UIManager.Instance.FadeOutScreen();
        screenFadedOut = false;
        StartCoroutine(ChangeScene(locationToSwitch)); 
    }

    IEnumerator ChangeScene(Location locationToSwitch)
    {
        //Disable the player's CharacterController component
        CharacterController playerCharacter = playerPoint.GetComponent<CharacterController>();
        playerCharacter.enabled = false;
        //Wait for the scene to finish fading out before loading the next scene
        while (!screenFadedOut)
        {
            yield return new WaitForSeconds(0.1f); 
        }

        //Reset the boolean
        screenFadedOut = false;
        UIManager.Instance.ResetFadeDefaults();
        SceneManager.LoadScene(locationToSwitch.ToString());
        
    }

    //Called when the screen has faded out
    public void OnFadeOutComplete()
    {
        screenFadedOut = true;
        
    }

    
    //Called when a scene is loaded
    public void OnLocationLoad(Scene scene, LoadSceneMode mode)
    {
        //The location the player is coming from when the scene loads
        Location oldLocation = currentLocation;

        //Get the new location by converting the string of our current scene into a Location enum value
        Location newLocation = (Location) Enum.Parse(typeof(Location), scene.name);

        //If the player is not coming from any new place, stop executing the function
        if (currentLocation == newLocation) return; 

        //Find the start point
        Transform startPoint = LocationManager.Instance.GetPlayerStartingPosition(oldLocation);

        //If the player object is destroyed, stop execution
        if (playerPoint == null) return; 
        
        //Disable the player's CharacterController component
        CharacterController playerCharacter = playerPoint.GetComponent<CharacterController>();
        playerCharacter.enabled = false; 

        //Change the player's position to the start point
        playerPoint.position = startPoint.position;
        playerPoint.rotation = startPoint.rotation;

        //Re-enable player character controller so he can move
        playerCharacter.enabled = true;

        //Save the current location that we just switched to
        currentLocation = newLocation;

        GameBlackboard blackboard = GameStateManager.Instance.GetBlackboard();
        blackboard.SetValue(LOCATION_KEY, currentLocation);
        blackboard.SetValue(INDOORS, CurrentlyIndoor()); 



        onLocationLoad?.Invoke();

        CutsceneManager.Instance.OnLocationLoad(); 
    }
}

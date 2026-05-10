using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;

public class MainMenu : MonoBehaviour
{
    public Button loadGameButton; 

    public void NewGame()
    {
        StartCoroutine(LoadGameAsync(SceneTransitionManager.Location.PlayerHome, null));
    }

    public void ContinueGame()
    {
        StartCoroutine(LoadGameAsync(SceneTransitionManager.Location.PlayerHome, LoadGame));
    }

    //To be called after the scene is loaded
    void LoadGame()
    {
        //Confirm if the GameStateManager is there (It should be if the scene is loaded)
        if(GameStateManager.Instance == null)
        {
            Debug.LogError("Cannot find Game State Manager!");
            return;
        }
        GameStateManager.Instance.LoadSave();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    IEnumerator LoadGameAsync(SceneTransitionManager.Location scene, Action onFirstFrameLoad)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(scene.ToString());
        //Make this GameObject persistent so it can continue to run after the scene is loaded
        DontDestroyOnLoad(gameObject);
        //Wait for the scene to load
        while (!asyncLoad.isDone)
        {
            yield return null;
            Debug.Log("Loading"); 
        }

        //Scene Loaded
        Debug.Log("Loaded!");

        yield return new WaitForEndOfFrame();
        Debug.Log("First frame is loaded");
        //If there is an Action assigned, call it
        onFirstFrameLoad?.Invoke(); 

        //Done
        Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        //Disable or enable the Load Game button based on whether there is a save file
        loadGameButton.interactable = SaveManager.HasSave(); 
    }


}

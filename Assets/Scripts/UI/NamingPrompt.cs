using System; 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class NamingPrompt : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI promptText;
    [SerializeField]
    TMP_InputField inputField;

    Action<string> onConfirm;
    Action onPromptComplete; 

    public void CreatePrompt(string message, Action<string> onConfirm)
    {
        //Set the action 
        this.onConfirm = onConfirm;
        //Display the prompt
        promptText.text = message;
    }

    //Queue an action to be executed when the prompt is complete
    public void QueuePromptAction(Action action)
    {
        onPromptComplete += action; 
    }

    //When player selects the confirm button
    public void Confirm()
    {
        //Invoke the callback and pass in the input field string
        onConfirm?.Invoke(inputField.text);

        //Reset the action
        onConfirm = null;
        //Reset the input text
        inputField.text = ""; 

        //Close the panel
        gameObject.SetActive(false);

        //Invoke the prompt complete callback
        onPromptComplete?.Invoke();
        onPromptComplete = null;
    }
}

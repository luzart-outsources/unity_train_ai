using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class YesNoPrompt : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI promptText;
    Action onYesSelected = null; 

    public void CreatePrompt(string message, Action onYesSelected)
    {
        //Set the action 
        this.onYesSelected = onYesSelected;
        //Display the prompt
        promptText.text = message; 
    }

    public void Answer(bool yes)
    {
        //Execute the action if yes is selected
        if (yes && onYesSelected != null)
        {
            onYesSelected();
        }

        //Reset the action 
        onYesSelected = null;

        gameObject.SetActive(false); 
    }
}

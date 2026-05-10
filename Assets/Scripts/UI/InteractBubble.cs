using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class InteractBubble : WorldUI
{
    [SerializeField]
    TextMeshProUGUI messageText; 
    public void Display(string message)
    {
        messageText.text = message; 
    }
}

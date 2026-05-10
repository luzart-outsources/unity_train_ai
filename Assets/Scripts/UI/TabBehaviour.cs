using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabBehaviour : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Sprite defaultSprite, selected, hover;
    Image tabImage;
    public UIManager.Tab windowToOpen;

    public static UnityEvent onTabStateChange = new UnityEvent(); 

    private void Awake()
    {
        tabImage = GetComponent<Image>();

        //Add this instance's function to be called on every tab state change
        onTabStateChange.AddListener(RenderTabState); 
    }

    //When the player clicks and selects the tab
    public void OnPointerClick(PointerEventData eventData)
    {
        onTabStateChange?.Invoke();
        tabImage.sprite = selected;
        UIManager.Instance.OpenWindow(windowToOpen);
    }

    //Hover
    public void OnPointerEnter(PointerEventData eventData)
    {
        onTabStateChange?.Invoke();
        tabImage.sprite = hover; 
    }

    //When the player exits 
    public void OnPointerExit(PointerEventData eventData)
    {
        onTabStateChange?.Invoke();
    }

    //Reset the tab to its basic render state if it is not selected
    void RenderTabState()
    {
        //Check if the selected tab is ours
        if(UIManager.Instance.selectedTab == windowToOpen)
        {
            tabImage.sprite = selected;
            return; 
        }
        tabImage.sprite = defaultSprite;
    }


}

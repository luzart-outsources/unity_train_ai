using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro; 

public class ShopListing : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Image itemThumbnail;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;

    ItemData itemData; 

    public void Display(ItemData itemData)
    {
        this.itemData = itemData;
        itemThumbnail.sprite = itemData.thumbnail;
        nameText.text = itemData.name;
        costText.text = itemData.cost + PlayerStats.CURRENCY; 
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        UIManager.Instance.shopListingManager.OpenConfirmationScreen(itemData);
    }

    //Display the item info on the item info box when the player mouses over
    public void OnPointerEnter(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(itemData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UIManager.Instance.DisplayItemInfo(null);
    }
}

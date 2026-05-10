using UnityEngine;
using UnityEngine.UI;

public class SelectedItemDisplay : MonoBehaviour
{
    [SerializeField] Image itemImage;
    [SerializeField] TMPro.TextMeshProUGUI quantityText;
    [SerializeField] Vector2 offset;

    private Canvas parentCanvas; 
    
    bool hasSelection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Get the parent canvas
        parentCanvas = GetComponentInParent<Canvas>();

        //Disable the item
        Enable(false); 
    }

    void Enable(bool enabled)
    {
        
        itemImage.gameObject.SetActive(enabled);
        quantityText.gameObject.SetActive(false);
    }

    public void Render(ItemSlotData itemSlot)
    {
        if (itemSlot.IsEmpty())
        {
            Enable(false);
            hasSelection = false; 
        } else
        {
            hasSelection = true;
            Enable(true);

            itemImage.sprite = itemSlot.itemData.thumbnail; 
            //Check if the item slot is more than 1
            if(itemSlot.quantity > 1)
            {
                quantityText.gameObject.SetActive(true); 
                quantityText.text = itemSlot.quantity.ToString();
            }


        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasSelection) return;
        Vector2 localPoint;

        //Convert mouse position to canvas space 
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            Input.mousePosition,
            parentCanvas.worldCamera,
            out localPoint
            );
        //Apply offset
        localPoint += offset;
        //Set position
        transform.localPosition = localPoint;
    }

    //Cancel Selection the moment the panel closes
    private void OnDisable()
    {
        InventoryManager.Instance.CancelSelection();
    }
}

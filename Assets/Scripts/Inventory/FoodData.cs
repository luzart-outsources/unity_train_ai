using UnityEngine;

[CreateAssetMenu(fileName = "FoodData", menuName = "Items/Food")]
public class FoodData : ItemData
{
    [Header("Consumption Effects")]
    [SerializeField] int staminaRestore;


    //For now, give the player stamina
    public void OnConsume()
    {
        PlayerStats.IncreaseStamina(staminaRestore);
    }
}

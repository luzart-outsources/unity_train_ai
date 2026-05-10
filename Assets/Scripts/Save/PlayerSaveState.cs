using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerSaveState
{
    //In future we will collect information like achievements, shipping history, building assets/upgrades and the like here. 
    public int money;
    public int stamina;

    public PlayerSaveState(int money, int stamina)
    {
        this.money = money;
        this.stamina = stamina;
        this.stamina = stamina;
    }

    public void LoadData()
    {
        PlayerStats.LoadStats(money, stamina);
    }

    public static PlayerSaveState Export()
    {
        return new PlayerSaveState(PlayerStats.Money, PlayerStats.Stamina); 
    }
}

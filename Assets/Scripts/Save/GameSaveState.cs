using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameSaveState
{
    //Blackboard 
    public GameBlackboard blackboard; 
    //Farm Data
    public FarmSaveState farmSaveState;
    //Inventory
    public InventorySaveState inventorySaveState;
    //Time
    public GameTimestamp timestamp;

    //PlayerStats
    public PlayerSaveState playerSaveState;

    //Weather State
    public WeatherSaveState weatherSaveState;

    //Relationships
    //public RelationshipSaveState relationshipSaveState;

    public GameSaveState(
        GameBlackboard blackboard,
        FarmSaveState farmSaveState,
        InventorySaveState inventorySaveState,
        GameTimestamp timestamp,
        PlayerSaveState playerSaveState,
        WeatherSaveState weatherSaveState
        //RelationshipSaveState relationshipSaveState
        )
        
    {
        this.blackboard = blackboard;
        this.farmSaveState = farmSaveState;
        this.inventorySaveState = inventorySaveState;
        this.timestamp = timestamp;
        this.playerSaveState = playerSaveState;
        this.weatherSaveState = weatherSaveState;
        //this.relationshipSaveState = relationshipSaveState;
    }
}

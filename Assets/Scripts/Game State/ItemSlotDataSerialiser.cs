using System.Collections.Generic;
using System.Linq;
using UnityEditor.Overlays;
using UnityEngine;

/// <summary>
/// Automatically serialise and deserialise lists of ItemSlotData
/// </summary>
public class ItemSlotDataSerialiser : IBlackboardSerialiser
{
    //Condition: must be an ItemSlotData list
    public bool ConditionMet(string key, object value)
    {
        return value.GetType().Equals(typeof(List<ItemSlotData>));
    }

    public void Deserialise(string key, object value, GameBlackboard blackboard)
    {
        ItemSlotSaveData[] saveData = value as ItemSlotSaveData[];

        ItemSlotData[] itemSlotData = ItemSlotData.DeserializeArray(saveData); 

        blackboard.SetValue(key, itemSlotData.ToList());

    }

    public bool DeserialiseConditionMet(string key, object value)
    {
        return value.GetType().Equals(typeof(ItemSlotSaveData[]));
    }

    public void Serialise(string key, object value, GameBlackboard blackboard)
    {
        List<ItemSlotData> saveData = value as List<ItemSlotData>;
        ItemSlotSaveData[] data = ItemSlotData.SerializeArray(saveData.ToArray());

        //Set the new value
        blackboard.SetValue(key, data);

    }
}

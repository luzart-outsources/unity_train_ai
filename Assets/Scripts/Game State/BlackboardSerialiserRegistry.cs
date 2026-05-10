using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// Handles the serialisation of data and all serialisers 
/// </summary>
public static class BlackboardSerialiserRegistry
{
    static List<IBlackboardSerialiser> serialisers = new List<IBlackboardSerialiser>(); 

    public static void RegisterSerialiser(IBlackboardSerialiser serialiser)
    {
        serialisers.Add(serialiser);
    }

    public static void UnregisterSerialiser(IBlackboardSerialiser serialiser)
    {
        serialisers.Remove(serialiser);
    }

    /// <summary>
    /// Function to register all serialisers
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void InitialiseSerialisers()
    {
        RegisterSerialiser(new ItemSlotDataSerialiser());
    }

    /// <summary>
    /// Serialise all data by registered serialisers
    /// </summary>
    public static void Serialise(GameBlackboard blackboard)
    {

        //Loop through the entire blackboard (copy)
        foreach (var entry in blackboard.entries.ToList())
        {
            //Check each serialiser 
            foreach(IBlackboardSerialiser serialiser in serialisers)
            {
                if (!serialiser.ConditionMet(entry.Key, entry.Value)) continue ; 

                serialiser.Serialise(entry.Key, entry.Value, blackboard);
            }
        }
        
    }

    /// <summary>
    /// Deserialise all data with registered serialisers
    /// </summary>
    public static void Deserialise(GameBlackboard blackboard)
    {
        //Loop through the entire blackboard
        foreach (var entry in blackboard.entries.ToList())
        {
            //Check each serialiser 
            foreach (IBlackboardSerialiser serialiser in serialisers)
            {
                if (!serialiser.DeserialiseConditionMet(entry.Key, entry.Value)) continue;

                serialiser.Deserialise(entry.Key, entry.Value, blackboard);
            }
        }
    }

}

using UnityEngine;

public interface IBlackboardSerialiser
{

    /// <summary>
    /// Condition for the serialiser to work
    /// </summary>
    /// <param name="key">The key from the blackboard</param>
    /// <param name="value">The value from the blackboard</param>
    /// <returns></returns>
    public bool ConditionMet(string key, object value);

    /// <summary>
    /// Deserialisation Condition
    /// </summary>
    /// <returns></returns>
    public bool DeserialiseConditionMet(string key, object value);

    /// <summary>
    /// Serialise a blackboard entry such that it can be saved
    /// </summary>
    /// <param name="key">The key from the blackboard</param>
    /// <param name="value">The value from the blackboard</param>
    /// <param name="blackboard">The blackboard itself</param>
    public void Serialise(string key, object value, GameBlackboard blackboard);

    /// <summary>
    /// Deserialise a blackboard entry such that it can be loaded in the game
    /// </summary>
    /// <param name="key">The key from the blackboard</param>
    /// <param name="value">The value from the blackboard</param>
    /// <param name="blackboard">The blackboard itself</param>
    public void Deserialise(string key, object value, GameBlackboard blackboard);
}

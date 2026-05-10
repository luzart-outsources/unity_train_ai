using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using static BlackboardCondition;

[Serializable]
public class GameBlackboard 
{

    public Dictionary<string, object> entries;
    public GameBlackboard()
    {
        entries = new Dictionary<string, object>();
    }

    public void Serialise()
    {
        BlackboardSerialiserRegistry.Serialise(this); 
    }

    public void Deserialise()
    {
        BlackboardSerialiserRegistry.Deserialise(this); 
    }

    public void Debug()
    {
        //For each entry print out its corresponding value
        foreach(var entry in entries)
        {
            string key = entry.Key.ToString(); 
            UnityEngine.Debug.Log($"key: {key} value: {entry.Value}");

        }
    }

    //Get a list type value or initialise one
    public List<T> GetOrInitList<T>(string key)
    {
        List<T> value = new List<T>();
        if (entries.TryGetValue(key, out object v))
        {
            value = (List<T>) v;
            
        }
        SetValue(key, value); 
        return value;

    }

    public bool TryGetValueAsString(string key, out string value)
    {
        value = default;
        if(TryGetValue<string>(key, out string strValue))
        {
            value = strValue;
            return true;
        }

        if(TryGetValue<bool>(key, out bool boolValue)) { 
            value = boolValue.ToString();
            return true;
        }


        if (TryGetValue(key, out int intValue))
        {
            value = intValue.ToString();
            return true;
        }
        if (TryGetValue(key, out float floatValue))
        {
            value = floatValue.ToString();
            return true; 
        }
 
        if (TryGetValue(key, out Vector3 vectorValue))
        {
            value = vectorValue.ToString();
            return true;
        }

        if (TryGetValue(key, out Vector2 vector2Value))
        {
            value = vector2Value.ToString();
            return true;
        }

        return false; 
    }

    //Try to get the value according to the type
    public bool TryGetValue<T>(string key, out T value)
    {
        //Parse the key such that 
        //(e.g.) NPCRelationship_Ben.friendshipPoints
        //Will result in retrieving the object "NPCRelationship_Ben"
        //And then the field "friendship" will be retrieved from the object

        //Get the index of where the '.' is 
        int dotIndex = key.IndexOf('.');
        string nestedObjectKey = string.Empty;
        bool nested = false; 
        //If there is a nested object
        if(dotIndex != -1)
        {
            nestedObjectKey = key.Substring(dotIndex + 1);
            nested = true;
            //Set it up such that the key is whatever preceding the '.' 
            key = key.Substring(0, dotIndex);
        }

        if (entries.TryGetValue(key, out object v))
        {
            //Handle Nested Property here 
            if (nested)
            {
                //Check if v is null
                if (v == null)
                {
                    UnityEngine.Debug.LogError(key + " not found");
                    value = default;
                    return false; 
                }

                value = (T) GetNestedObject(nestedObjectKey, v);
                if (value is not null)
                {
                    return true;
                }
                return false; 
            }

            try
            {
                value = (T)v;
                if (value is not null)
                {
                    return true;
                }
            }
            catch (InvalidCastException e)
            {
                UnityEngine.Debug.LogError($"{e.Message} Failed to cast object of type '{v.GetType().Name}' to type '{typeof(T).Name}'.");
            }
            
            
        }
        value = default(T);
        return false; 
    }

    object GetNestedObject(string keys, object parent)
    {
        if(keys == string.Empty) return null;
        // Check if parent is null
        if (parent == null)
        {
            UnityEngine.Debug.LogError($"Could not access nested object '{keys}' because parent object is null.");
            return null;
        }
        // Split keys into an array for potential nested access
        string[] keyParts = keys.Split('.');

        object currentObject = parent;
        foreach (string keyPart in keyParts)
        {
            // Check if current object is null during traversal
            if (currentObject == null)
            {
                UnityEngine.Debug.LogError($"Could not access nested object '{keys}' because a parent object in the path was null.");
                return null;
            }

            // Try to get the property using reflection
            var property = currentObject.GetType().GetProperty(keyPart);

            if (property != null)
            {
                // Access using property if available
                currentObject = property.GetValue(currentObject);
                continue; // Move to the next key part
            }
            // Fallback: Try to access as a field if property not found
            var field = currentObject.GetType().GetField(keyPart, BindingFlags.Public | BindingFlags.Instance);
            if (field != null)
            {
                currentObject = field.GetValue(currentObject);
                continue; 
            }

            UnityEngine.Debug.LogError($"Property and Field '{keyPart}' not found in object of type '{currentObject.GetType().Name}'.");
            LogProperties(currentObject);
            LogFields(currentObject); 
            return null;


        }

        return currentObject;

    }

    //Debug function to print out all the public fields of the object
    public static void LogFields(object obj)
    {
        if (obj == null)
        {
            UnityEngine.Debug.LogError("Object is null");
            return;
        }

        Type type = obj.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

        UnityEngine.Debug.Log($"Fields of type '{type.Name}':");
        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(obj);
            UnityEngine.Debug.Log($"{field.Name}: {value}");
        }
    }
    //Debug function to print out all the public properties of the object
    public static void LogProperties(object obj)
    {
        if (obj == null)
        {
            UnityEngine.Debug.LogError("Object is null");
            return;
        }

        Type type = obj.GetType();
        PropertyInfo[] properties = type.GetProperties();

        UnityEngine.Debug.Log($"Properties of type '{type.Name}' ({properties.Length}):");
        foreach (PropertyInfo property in properties)
        {
            UnityEngine.Debug.Log($"{property.Name}: {property.PropertyType}");
        }
    }

    //Change the value in the blackboard entry, otherwise create a new entry
    public void SetValue<T>(string key, T value)
    {
        //UnityEngine.Debug.Log($"Setting {key} to {value}");
        entries[key] = value;
        //UnityEngine.Debug.Log($"The value is {entries[key]}");
        Debug();
    }

    //Increase value of an entry
    public void IncreaseValue(string key, int valueToIncrease){
        //Get the value
        if(TryGetValue(key, out int previousValue))
        {
            SetValue(key, previousValue+ valueToIncrease);
        } else
        {
            //Create a new entry
            SetValue(key, valueToIncrease);
        }
    }

    public bool ContainsKey(string key) => entries.ContainsKey(key);

    public void RemoveKey(string key) => entries.Remove(key);

    //Compare with the blackboard
    public bool CompareValue(BlackboardCondition condition)
    {
        UnityEngine.Debug.Log("Now comparing " + condition.blackboardEntryData.keyName + "=" + condition.blackboardEntryData.GetValue().ToString());
        
        //Operator defaults to == for non numeric types
        switch (condition.blackboardEntryData.valueType)
        {
            case BlackboardEntryData.ValueType.Bool:
                if(TryGetValue(condition.blackboardEntryData.keyName, out bool boolValue)){
                    UnityEngine.Debug.Log($"  - Retrieved value: {boolValue}");
                    return (boolValue == condition.blackboardEntryData.boolValue);
                    
                }
                UnityEngine.Debug.LogError($"No such value {condition.blackboardEntryData.keyName} found. ");
                break; 
            case BlackboardEntryData.ValueType.Int:
                if(TryGetValue(condition.blackboardEntryData.keyName, out int intValue))
                {
                    UnityEngine.Debug.Log($"  - Retrieved value: {intValue}");
                    //return (intValue == condition.intValue);
                    return CompareValuesLogic(intValue, condition.blackboardEntryData.intValue, condition.comparison);

                }
                UnityEngine.Debug.LogError($"No such value {condition.blackboardEntryData.keyName} found. ");
                break;
            case BlackboardEntryData.ValueType.Float:
                if (TryGetValue(condition.blackboardEntryData.keyName, out float floatValue))
                {
                    UnityEngine.Debug.Log($"  - Retrieved value: {floatValue}");
                    //return (floatValue == condition.floatValue);
                    return CompareValuesLogic(floatValue, condition.blackboardEntryData.floatValue, condition.comparison);
                }
                UnityEngine.Debug.LogError($"No such value {condition.blackboardEntryData.keyName} found. ");
                break;
            case BlackboardEntryData.ValueType.String:
                if (TryGetValue(condition.blackboardEntryData.keyName, out string stringValue))
                {
                    UnityEngine.Debug.Log($"  - Retrieved value: {stringValue}");
                    return (stringValue == condition.blackboardEntryData.stringValue);

                }
                UnityEngine.Debug.LogError($"No such value {condition.blackboardEntryData.keyName} found. ");
                break;
            case BlackboardEntryData.ValueType.Vector3:
                if (TryGetValue(condition.blackboardEntryData.keyName, out Vector3 vector3Value))
                {
                    UnityEngine.Debug.Log($"  - Retrieved value: {vector3Value}");
                    return (vector3Value == condition.blackboardEntryData.vector3Value);

                }
                UnityEngine.Debug.LogError($"No such value {condition.blackboardEntryData.keyName} found. ");
                break;

        }
        return false; 
    }

    //Helper function for comparison logic
    bool CompareValuesLogic<T>(T value1, T value2, BlackboardCondition.ComparisonType comparisonType)
    {
        //If we are NOT comparing numbers and somehow aren't comparing equal, it is an invalid comparison
        if( (typeof(T) != typeof(int) && typeof(T) != typeof(float))  && comparisonType != ComparisonType.Equal)
        {
            UnityEngine.Debug.LogError($"Invalid comparison for type: {typeof(T).Name} with comparison type: LessThan");
            return false;
        }

        switch (comparisonType)
        {
            case ComparisonType.Equal:
                return value1.Equals(value2);
            case ComparisonType.GreaterThanOrEqualTo:
                if (typeof(T) == typeof(int))
                {
                    return (int)(object)value1 >= (int)(object)value2;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (float)(object)value1 >= (float)(object)value2;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Invalid comparison for type: {typeof(T).Name} with comparison type: GreaterThanOrEqualTo");
                    return false;
                }
            case ComparisonType.LessThanOrEqualTo:
                if (typeof(T) == typeof(int))
                {
                    return (int)(object)value1 <= (int)(object)value2;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (float)(object)value1 <= (float)(object)value2;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Invalid comparison for type: {typeof(T).Name} with comparison type: LessThanOrEqualTo");
                    return false;
                }
            case ComparisonType.GreaterThan:
                if (typeof(T) == typeof(int))
                {
                    return (int)(object)value1 > (int)(object)value2;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (float)(object)value1 > (float)(object)value2;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Invalid comparison for type: {typeof(T).Name} with comparison type: GreaterThan");
                    return false;
                }
            case ComparisonType.LessThan:
                if (typeof(T) == typeof(int))
                {
                    return (int)(object)value1 < (int)(object)value2;
                }
                else if (typeof(T) == typeof(float))
                {
                    return (float)(object)value1 < (float)(object)value2;
                }
                else
                {
                    UnityEngine.Debug.LogError($"Invalid comparison for type: {typeof(T).Name} with comparison type: LessThan");
                    return false;
                }
            default:
                UnityEngine.Debug.LogError("Invalid comparison type: " + comparisonType);
                return false;
        }
    }

}

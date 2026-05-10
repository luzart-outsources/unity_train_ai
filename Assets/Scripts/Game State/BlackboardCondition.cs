using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BlackboardCondition 
{
    public BlackboardEntryData blackboardEntryData;
    public enum ComparisonType
    {
        Equal,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo,
        GreaterThan,
        LessThan
    }
    public ComparisonType comparison; 
}

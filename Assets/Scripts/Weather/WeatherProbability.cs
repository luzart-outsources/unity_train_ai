using UnityEngine;
[System.Serializable]
public struct WeatherProbability
{
    public WeatherData.WeatherType weatherType;

    [Range(0f, 1f)]
    public float probability;
    
}

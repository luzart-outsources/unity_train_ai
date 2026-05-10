using UnityEngine;

[System.Serializable]
public struct WeatherSaveState
{
    public WeatherData.WeatherType weather; 

    public WeatherSaveState(WeatherData.WeatherType weather)
    {
        this.weather = weather;
    }

    public void LoadData()
    {
        WeatherManager.Instance.LoadWeather(this);
    }

    public static WeatherSaveState Export()
    {
        return new WeatherSaveState(WeatherManager.Instance.WeatherTomorrow); 
    }
    
}

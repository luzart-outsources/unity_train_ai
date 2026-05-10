using UnityEngine;

public class WeatherManager : MonoBehaviour, ITimeTracker
{
    public static WeatherManager Instance { get; private set; }
    private void Awake()
    {
        //If there is more than one instance, destroy the extra
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            //Set the static instance to this instance
            Instance = this;
        }
    }

    public WeatherData.WeatherType WeatherToday { get; private set; }

    public WeatherData.WeatherType WeatherTomorrow { get; private set; }

    //Check if the weather has been set before
    bool weatherSet = false; 

    //The weather data
    [SerializeField] WeatherData weatherData;


    void Start()
    {
        //Add this to TimeManager's Listener list
        TimeManager.Instance.RegisterTracker(this);
    }




        public WeatherData.WeatherType ComputeWeather(GameTimestamp.Season season)
    {
        if(weatherData == null)
        {
            throw new System.Exception("No weather data loaded"); 
        }

        //What are the possible weathers to compute
        WeatherProbability[] weatherSet = null;

        switch (season)
        {
            case GameTimestamp.Season.Spring:
                weatherSet = weatherData.springWeather;
                break; 
            case GameTimestamp.Season.Summer:
                weatherSet = weatherData.summerWeather;
                break;

            case GameTimestamp.Season.Fall:
                weatherSet = weatherData.fallWeather;
                break;

            case GameTimestamp.Season.Winter:
                weatherSet = weatherData.winterWeather;
                break; 
        }

        //Roll a random value 
        float randomValue = Random.Range(0, 1f);

        //Initialise probability
        float culmProbability = 0; 
        foreach(WeatherProbability weatherProbability in weatherSet)
        {
            culmProbability += weatherProbability.probability;
            //If in the probability
            if (randomValue <= culmProbability) {
                return weatherProbability.weatherType;
            }

        }

        //Sunny by default
        return WeatherData.WeatherType.Sunny; 
    }

    public void LoadWeather(WeatherSaveState saveState)
    {
        weatherSet = true;
        WeatherToday = saveState.weather;

        //Set the forecast 
        WeatherTomorrow = ComputeWeather(TimeManager.Instance.GetGameTimestamp().season);
    }

    public void ClockUpdate(GameTimestamp timestamp)
    {
        //Check if it is 6am 
        if(timestamp.hour == 6 && timestamp.minute == 0)
        {
            //Set the current weather
            if (!weatherSet)
            {
                WeatherToday = ComputeWeather(timestamp.season);
               
            }
            else
            {
                WeatherToday = WeatherTomorrow;
                
            }
            UIManager.Instance.ChangeWeatherUI();

            //Set the forecast 
            WeatherTomorrow = ComputeWeather(timestamp.season);

            weatherSet = true;
            Debug.Log("The weather is " + WeatherToday.ToString());


        }
    }
}

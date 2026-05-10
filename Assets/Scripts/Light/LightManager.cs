using UnityEngine;

public class LightManager : MonoBehaviour , ITimeTracker
{
    public GameObject[] nightLights;

    private void Update()
    {
        nightLights = GameObject.FindGameObjectsWithTag("Lights");
    }
    public void ClockUpdate(GameTimestamp timestamp)
    {

        if (timestamp.hour >= 6)
        {
            foreach (GameObject i in nightLights)   
            {
                
                i.GetComponent<Light>().enabled = true;
            }
        }
    }

}

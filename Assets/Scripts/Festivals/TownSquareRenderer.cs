using UnityEngine;

public class TownSquareRenderer : MonoBehaviour
{
    //The props if a festival were to be ongoing
    [SerializeField] GameObject eventProps; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FestivalData festival = FestivalManager.Instance.GetFestivalByDay(TimeManager.Instance.GetGameTimestamp()); 

        if(festival != null)
        {
            eventProps.gameObject.SetActive(festival.activateTownSquareDecor); 
        } else
        {
            eventProps.gameObject.SetActive(false);
        }
    }

}

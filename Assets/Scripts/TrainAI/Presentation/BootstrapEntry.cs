using TrainAI.Services;
using UnityEngine;

namespace TrainAI.Presentation
{
    [DefaultExecutionOrder(-1000)]
    public class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] bool dontDestroyOnLoad = true;
        [SerializeField] int startDay = 1;
        [SerializeField] bool autoStartDay = true;

        void Awake()
        {
            if (services == null)
            {
                Debug.LogError("[BootstrapEntry] ServiceLocator not assigned.");
                return;
            }
            services.Bootstrap();
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (autoStartDay) services.Quests?.StartDay(startDay);
        }

        void OnDestroy()
        {
            if (services != null) services.Shutdown();
        }
    }
}

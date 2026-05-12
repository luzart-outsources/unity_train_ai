using Cysharp.Threading.Tasks;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Presentation
{
    [DefaultExecutionOrder(-1000)]
    public class BootstrapEntry : MonoBehaviour
    {
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] bool dontDestroyOnLoad = true;
        [SerializeField] int startDay = 1;
        [SerializeField] bool autoStartDay = true;
        [SerializeField] SceneRefSO worldScene;

        async void Awake()
        {
            if (services == null)
            {
                Debug.LogError("[BootstrapEntry] ServiceLocator not assigned.");
                return;
            }
            services.Bootstrap();
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (worldScene != null && services.Scenes != null)
            {
                if (!SceneManager.GetSceneByName(worldScene.sceneName).isLoaded)
                    await services.Scenes.LoadAdditive(worldScene);
            }

            if (autoStartDay) services.Quests?.StartDay(startDay);
        }

        void OnDestroy()
        {
            if (services != null) services.Shutdown();
        }
    }
}

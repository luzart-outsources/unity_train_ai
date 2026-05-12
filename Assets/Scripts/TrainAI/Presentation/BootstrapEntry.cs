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
        [SerializeField] SceneRefSO firstScene; // 01_MainMenu

        async void Awake()
        {
            if (services == null) { Debug.LogError("[BootstrapEntry] ServiceLocator not assigned."); return; }
            services.Bootstrap();
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

            if (firstScene != null && services.Scenes != null
                && !SceneManager.GetSceneByName(firstScene.sceneName).isLoaded)
            {
                await services.Scenes.LoadSingle(firstScene);
            }
        }

        void OnDestroy()
        {
            if (services != null) services.Shutdown();
        }
    }
}

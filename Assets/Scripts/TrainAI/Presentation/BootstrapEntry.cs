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
            // Force a moderate quality preset at game start. Project default
            // was 'Ultra' (level 5) — too heavy for GTX 1060 Max-Q class GPUs:
            // shadowDistance=150, AA=2x, full real-time shadows. Bumping down
            // to 'Medium' (level 2) gives ~30-40% GPU frame-time headroom
            // without visually losing much in a stylized campus scene.
            // Also pin targetFrameRate so vSync stays at 60Hz on monitors
            // that default to higher refresh.
            UnityEngine.QualitySettings.SetQualityLevel(2, true);
            UnityEngine.Application.targetFrameRate = 60;

            // async void Awake — wrap so a Sentis model load failure or a
            // first-scene-missing throw becomes a logged error instead of
            // Unity's unhandled-exception crash (which leaves the user
            // staring at a black editor).
            try
            {
                services.Bootstrap();
                if (dontDestroyOnLoad)
                {
                    DontDestroyOnLoad(gameObject);
                    // Bridge needs to survive scene swaps — attach to Bootstrap
                    // GameObject so DDOL carries it. EventSystem is intentionally
                    // NOT DDOL'd: each scene ships with its own and we rely on
                    // Unity to keep one active per scene. Trying to DDOL one
                    // here previously fed the "Multiple EventSystems" warning
                    // every frame during 2-scene overlap windows, flooding the
                    // log and eventually TDR-killing the GPU.
                    EnsureBridge();
                }

                if (firstScene != null && services.Scenes != null
                    && !SceneManager.GetSceneByName(firstScene.sceneName).isLoaded)
                {
                    await services.Scenes.LoadSingle(firstScene);
                }
            }
            catch (System.Exception e) { Debug.LogError($"[BootstrapEntry] startup failed: {e}"); }
        }

        void EnsureBridge()
        {
            // Attach the bridge to the Bootstrap GameObject if it isn't here
            // already (e.g. on legacy scenes where it lived in 10_World). The
            // DDOL we already did above carries the bridge across all scene
            // swaps so PlayerInteractor's InteractPressedMsg always has a
            // listener — without this, pressing E in a sub-scene was silent.
            var bridge = GetComponent<InteractionRouterBridge>();
            if (bridge == null) bridge = gameObject.AddComponent<InteractionRouterBridge>();
            if (bridge.services == null) bridge.Wire(services);
        }

        void OnDestroy()
        {
            if (services != null) services.Shutdown();
        }
    }
}

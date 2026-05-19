using Cysharp.Threading.Tasks;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIMainMenuController : UIScreenBase
    {
        [SerializeField] Button newGameButton;
        [SerializeField] Button continueButton;
        [SerializeField] Button exitButton;
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] SceneRefSO createCharScene;
        [SerializeField] SceneRefSO worldScene;

        protected override void Awake()
        {
            base.Awake();
            if (newGameButton != null) newGameButton.onClick.AddListener(OnNewGame);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
            if (exitButton != null) exitButton.onClick.AddListener(OnExit);
            Show();
        }

        async void OnNewGame()
        {
            if (services == null || services.Scenes == null || createCharScene == null) return;
            try { await services.Scenes.LoadSingle(createCharScene); }
            catch (System.Exception e) { Debug.LogError($"[MainMenu] OnNewGame failed: {e}"); }
        }

        async void OnContinue()
        {
            if (services == null || services.Save == null) return;
            try
            {
                if (!services.Save.TryLoad(slot: 1, out _))
                {
                    if (services.UI != null) await services.UI.ShowConfirm("Khong co save game.");
                    return;
                }
                // Saves usually happen at end-of-day (Sleep at 18:30). Reset to 05:00 so
                // the player resumes at the start of the saved day, not after all quests
                // have already missed their windows.
                services.Clock?.SkipTo(5, 0);
                if (worldScene != null) await services.Scenes.LoadSingle(worldScene);
                services.Quests?.StartDay(services.clock != null ? services.clock.day : 1);
            }
            catch (System.Exception e) { Debug.LogError($"[MainMenu] OnContinue failed: {e}"); }
        }

        void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

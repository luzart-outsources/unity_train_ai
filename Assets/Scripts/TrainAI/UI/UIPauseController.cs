using Cysharp.Threading.Tasks;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TrainAI.UI
{
    // Pause overlay that the player can summon at any time during gameplay
    // (HUD button or ESC key). Three actions:
    //
    //   Resume        — un-freeze clock, hide panel, restore Time.timeScale
    //   Save & Quit   — services.Save.Save(slot 1), then LoadSingle(01_MainMenu)
    //   Quit (no save)— LoadSingle(01_MainMenu) without saving
    //
    // Why a top-level Time.timeScale=0 instead of just clock.Freeze():
    //   - clock.Freeze stops the in-game minute counter but Update loops
    //     (NPC director, movement service, camera follow) keep running. The
    //     player wants the world frozen during pause — UI tween, music duck,
    //     animator drivers, all halted. Time.timeScale=0 is the cleanest
    //     blanket stop.
    //   - Sub-scene transitions during pause would be chaos; SceneRouter
    //     LoadSingle handles its own timeScale restore inside the loading
    //     screen.
    //
    // Pause panel sits on ModalCanvas (sortingOrder 200) so it overlays HUD
    // but loses out to dialogue/quiz/loading when those are open. Save & Quit
    // is gated behind those sibling modals via UIScreenBase.canvasGroup
    // (interactable=false when alpha=0).
    public class UIPauseController : UIScreenBase
    {
        [SerializeField] Button resumeButton;
        [SerializeField] Button saveQuitButton;
        [SerializeField] Button quitNoSaveButton;
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] SceneRefSO mainMenuScene;

        bool _busy;

        protected override void Awake()
        {
            base.Awake();
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (saveQuitButton != null) saveQuitButton.onClick.AddListener(OnSaveQuit);
            if (quitNoSaveButton != null) quitNoSaveButton.onClick.AddListener(OnQuitNoSave);
        }

        // Override the base Hide so the GameObject STAYS ACTIVE — otherwise
        // Update() stops firing and ESC can never re-open the pause panel.
        // Visually identical to base.Hide (alpha=0, blocksRaycasts=false), just
        // without the gameObject.SetActive(false) tail.
        public override void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }

        void Update()
        {
            // Project uses Unity new Input System exclusively — legacy
            // Input.GetKeyDown throws InvalidOperationException at runtime.
            // Read directly off the InputSystem keyboard device; no need for
            // a full InputAction asset entry for a single hotkey.
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.escapeKey.wasPressedThisFrame && !_busy)
            {
                if (canvasGroup != null && canvasGroup.alpha > 0.5f) OnResume();
                else Open();
            }
        }

        public void Open()
        {
            if (_busy) return;
            // Freeze world before any UI fade — if Show() ever becomes async
            // we don't want the world to keep ticking between request and
            // visible pause.
            Time.timeScale = 0f;
            services?.Clock?.Freeze();
            Show();
        }

        void OnResume()
        {
            if (_busy) return;
            Hide();
            services?.Clock?.Resume();
            Time.timeScale = 1f;
        }

        async void OnSaveQuit()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                // Restore timescale BEFORE LoadSingle — scene loads under
                // timeScale=0 can stall coroutines that some scene init
                // depends on (UILoading uses WaitForSeconds which respects
                // timeScale).
                Time.timeScale = 1f;
                services?.Clock?.Resume();
                services?.Save?.Save(slot: 1);
                if (services != null && services.Scenes != null && mainMenuScene != null)
                    await services.Scenes.LoadSingle(mainMenuScene);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Pause] SaveQuit failed: {e}");
            }
            finally
            {
                _busy = false;
                Hide();
            }
        }

        async void OnQuitNoSave()
        {
            if (_busy) return;
            _busy = true;
            try
            {
                Time.timeScale = 1f;
                services?.Clock?.Resume();
                if (services != null && services.Scenes != null && mainMenuScene != null)
                    await services.Scenes.LoadSingle(mainMenuScene);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Pause] QuitNoSave failed: {e}");
            }
            finally
            {
                _busy = false;
                Hide();
            }
        }
    }
}

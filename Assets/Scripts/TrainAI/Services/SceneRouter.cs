using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Services
{
    public class SceneRouter : ISceneRouter
    {
        readonly IUIRouter _ui;

        public SceneRouter(IUIRouter ui) { _ui = ui; }

        // Set true while a scene transition is mid-flight so other systems
        // (MovementService, NPCDirector, etc.) can pause their per-frame
        // work. Without this the still-alive DDOL services kept dispatching
        // Sentis GPU compute on NPC transforms that were being destroyed in
        // the unload phase — the GPU then dereferenced freed memory and the
        // NVIDIA driver killed the editor (D3D12 crash in DispatchComputeProgram).
        public static bool IsTransitioning { get; private set; }

        // Load a target scene and unload all previously loaded scenes (except
        // DontDestroyOnLoad objects, which persist automatically). This is the
        // "transition" pattern: enter a building, leave the campus behind.
        //
        // Why this exists separately from LoadSingle:
        //   - LoadSingle uses LoadSceneMode.Single which auto-unloads everything.
        //     Problem: during the brief window between unload-old and load-new,
        //     the engine has ZERO scenes loaded. Any code touching SceneManager
        //     during that window throws.
        //   - LoadReplacing loads ADDITIVELY first (so engine always has ≥1 scene),
        //     sets the new one active, then unloads the olds. Safer transition.
        //
        // Player persistence: Player must have `PlayerPersistence` (or any
        // DontDestroyOnLoad) component, otherwise it gets destroyed when the
        // old scene unloads. PlayerPersistence also teleports the player to a
        // `PlayerSpawn` GameObject if one exists in the newly loaded scene.
        public async UniTask LoadReplacing(SceneRefSO target, string transitionText = null)
        {
            if (target == null || string.IsNullOrEmpty(target.sceneName)) return;
            IsTransitioning = true;
            try
            {
                if (!string.IsNullOrEmpty(transitionText) && _ui != null)
                    await _ui.ShowLoading(transitionText, 0.5f);

                // Snapshot scenes to unload BEFORE loading target — by name to
                // avoid the post-load enumeration mutating the list.
                var toUnload = new List<string>();
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var s = SceneManager.GetSceneAt(i);
                    if (s.isLoaded && s.name != target.sceneName) toUnload.Add(s.name);
                }

                // Load target additively first so we never have 0 scenes loaded.
                await SceneManager.LoadSceneAsync(target.sceneName, LoadSceneMode.Additive).ToUniTask();

                // Set new as active scene so further ops (instantiation, lighting)
                // target the right scene.
                var newScene = SceneManager.GetSceneByName(target.sceneName);
                if (newScene.IsValid()) SceneManager.SetActiveScene(newScene);

                // Unload the olds. Sequential awaits — Unity will throw if we
                // start a 2nd unload before the 1st completes its frame.
                foreach (var name in toUnload)
                {
                    if (!SceneManager.GetSceneByName(name).isLoaded) continue;
                    await SceneManager.UnloadSceneAsync(name).ToUniTask();
                }

                // After the swap, kill duplicate EventSystems from the target
                // scene — the DDOL one BootstrapEntry promoted is the canonical
                // input handler. Without this, the target scene's own
                // EventSystem and the DDOL one fight (Unity disables one) and
                // sometimes the wrong one survives → UI clicks die.
                DeduplicateEventSystems();
                DeduplicateCameras();
            }
            catch (Exception e) { Debug.LogError($"[SceneRouter] LoadReplacing '{target.sceneName}' failed: {e.Message}"); }
            finally { IsTransitioning = false; }
        }

        // Keeps exactly one EventSystem alive, preferring DDOL → active-scene →
        // first. Called only from LoadReplacing because that's the one path
        // where two scenes are momentarily loaded together — the load mode
        // Single auto-handles its own cleanup, and Additive is intentionally
        // multi-scene.
        static void DeduplicateEventSystems()
        {
            var systems = UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            if (systems.Length <= 1) return;
            UnityEngine.EventSystems.EventSystem keep = null;
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i].gameObject.scene.name == "DontDestroyOnLoad") { keep = systems[i]; break; }
            }
            if (keep == null)
            {
                // No DDOL one (the recommended state) — keep the one in the
                // active scene so input continues working after the unload.
                var active = SceneManager.GetActiveScene();
                for (int i = 0; i < systems.Length; i++)
                {
                    if (systems[i].gameObject.scene == active) { keep = systems[i]; break; }
                }
            }
            if (keep == null) keep = systems[0];
            for (int i = 0; i < systems.Length; i++)
            {
                if (systems[i] != keep) UnityEngine.Object.Destroy(systems[i].gameObject);
            }
        }

        // Conservative camera cleanup, called only from LoadReplacing. Only
        // touches cameras when there's a DDOL one (i.e., ThirdPersonCameraRig
        // already survived from an earlier 10_World load). Otherwise leaves
        // every camera alone — destroying the only camera in a scene that
        // hasn't yet wired up a DDOL one (eg first LoadSingle into 10_World
        // before CameraRig.Awake) blacks out the screen.
        static void DeduplicateCameras()
        {
            var cams = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            bool hasDDOL = false;
            for (int i = 0; i < cams.Length; i++)
            {
                if (cams[i] != null && cams[i].targetTexture == null && cams[i].gameObject.scene.name == "DontDestroyOnLoad")
                { hasDDOL = true; break; }
            }
            if (!hasDDOL) return; // safer to live with the duplicate than to blank the screen

            for (int i = 0; i < cams.Length; i++)
            {
                var c = cams[i];
                if (c == null) continue;
                if (c.targetTexture != null) continue;       // skip RT (minimap)
                if (c.gameObject.scene.name == "DontDestroyOnLoad") continue;
                UnityEngine.Object.Destroy(c.gameObject);
            }

            // AudioListener dedup — but only when we actually have multiples,
            // and prefer the DDOL one if present.
            var listeners = UnityEngine.Object.FindObjectsByType<UnityEngine.AudioListener>(
                UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
            if (listeners.Length <= 1) return;
            UnityEngine.AudioListener keep = null;
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].gameObject.scene.name == "DontDestroyOnLoad") { keep = listeners[i]; break; }
            }
            if (keep == null) keep = listeners[0];
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i] != keep) UnityEngine.Object.Destroy(listeners[i]);
            }
        }

        // All three Load/Unload methods catch exceptions. SceneManager throws
        // ArgumentException when the scene isn't in build settings, which in
        // earlier playtests propagated up through the InteractionRouter await
        // chain into Unity's unhandled-exception path and crashed the in-game
        // flow when a door pointed to a not-yet-baked sub-scene. Logging +
        // returning lets the player keep playing on bad data instead of
        // bricking the session.
        public async UniTask LoadAdditive(SceneRefSO scene, string transitionText = null)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            try
            {
                if (!string.IsNullOrEmpty(transitionText) && _ui != null)
                    await _ui.ShowLoading(transitionText, 0.5f);
                await SceneManager.LoadSceneAsync(scene.sceneName, LoadSceneMode.Additive).ToUniTask();
                // No dedup here — LoadAdditive is an OVERLAY load (the prior
                // scene stays loaded intentionally), so multiple EventSystems
                // / cameras is the expected state. Dedup would destroy the
                // overlay scene's input, breaking it.
            }
            catch (Exception e) { Debug.LogError($"[SceneRouter] LoadAdditive '{scene.sceneName}' failed: {e.Message}"); }
        }

        public async UniTask UnloadAdditive(SceneRefSO scene)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            try { await SceneManager.UnloadSceneAsync(scene.sceneName).ToUniTask(); }
            catch (Exception e) { Debug.LogError($"[SceneRouter] UnloadAdditive '{scene.sceneName}' failed: {e.Message}"); }
        }

        public async UniTask LoadSingle(SceneRefSO scene)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            try
            {
                await SceneManager.LoadSceneAsync(scene.sceneName, LoadSceneMode.Single).ToUniTask();
                // No dedup here — LoadSceneMode.Single unloads everything
                // non-DDOL on its own, so by the time the await returns there's
                // already exactly one EventSystem / MainCamera (the new scene's).
                // Earlier this method called dedup, which then destroyed the
                // ONLY remaining camera in main-menu/create-char scenes that
                // had no DDOL counterpart yet → black screen during boot flow.
            }
            catch (Exception e) { Debug.LogError($"[SceneRouter] LoadSingle '{scene.sceneName}' failed: {e.Message}"); }
        }
    }
}

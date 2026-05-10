using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Systems.Audio;
using TrainAI.UI.Components;
using UnityEngine.SceneManagement;

namespace TrainAI.Systems.Scene
{
    // Async scene change voi Loading screen + freeze time + audio swap.
    public class SceneFlowService
    {
        public string CurrentSceneId { get; private set; } = "";

        public async UniTask TravelAsync(SceneRouteSO route, CancellationToken ct = default)
        {
            if (route == null) return;

            var loadingData = new LoadingData { Text = route.loadingText };
            var loadingHandle = await UIManager.Instance.ShowAsync(UIId.Loading, new UIContext(loadingData), default, ct);

            // Freeze time tu khi bat dau load (GDD: scene quest -> dong bang).
            if (route.freezeTimeWhileLoaded && GameServices.Time != null)
                GameServices.Time.Freeze();

            // Stop music truoc.
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.StopMusic(0.3f);
                am.StopAmbient(0.3f);
            }

            try
            {
                var minWait = UniTask.Delay(TimeSpan.FromSeconds(route.minLoadingScreenSeconds), cancellationToken: ct);
                var sceneLoad = SceneManager.LoadSceneAsync(route.sceneName).ToUniTask(cancellationToken: ct);
                await UniTask.WhenAll(minWait, sceneLoad);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"[SceneFlow] Travel failed to {route.sceneName}: {e}");
            }

            CurrentSceneId = route.id;

            // Play music + ambient cho scene moi.
            if (am != null)
            {
                if (route.backgroundMusic != AudioCueId.None) am.Play(route.backgroundMusic);
                if (route.ambient != AudioCueId.None) am.Play(route.ambient);
            }

            // Resume time chi khi scene la World (khong freeze).
            if (!route.freezeTimeWhileLoaded && GameServices.Time != null)
                GameServices.Time.Resume();

            await UIManager.Instance.HideAsync(loadingHandle, default, ct);
        }

        public void ResumeTimeIfFrozen()
        {
            if (GameServices.Time != null && GameServices.Time.IsFrozen)
                GameServices.Time.Resume();
        }
    }
}

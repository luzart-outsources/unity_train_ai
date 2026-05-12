using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
using UnityEngine.SceneManagement;

namespace TrainAI.Services
{
    public class SceneRouter : ISceneRouter
    {
        readonly IUIRouter _ui;

        public SceneRouter(IUIRouter ui) { _ui = ui; }

        public async UniTask LoadAdditive(SceneRefSO scene, string transitionText = null)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            if (!string.IsNullOrEmpty(transitionText) && _ui != null)
                await _ui.ShowLoading(transitionText, 0.5f);
            await SceneManager.LoadSceneAsync(scene.sceneName, LoadSceneMode.Additive).ToUniTask();
        }

        public async UniTask UnloadAdditive(SceneRefSO scene)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            await SceneManager.UnloadSceneAsync(scene.sceneName).ToUniTask();
        }

        public async UniTask LoadSingle(SceneRefSO scene)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) return;
            await SceneManager.LoadSceneAsync(scene.sceneName, LoadSceneMode.Single).ToUniTask();
        }
    }
}

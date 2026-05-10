using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Systems.Audio;
using TrainAI.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    public class MainMenuScreen : UIBase
    {
        [Header("Buttons")]
        [SerializeField] private Button btnNewGame;
        [SerializeField] private Button btnContinue;
        [SerializeField] private Button btnQuit;

        [Header("Background")]
        [SerializeField] private TextMeshProUGUI txtTitle;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnNewGame != null) btnNewGame.onClick.AddListener(OnNewGame);
            if (btnContinue != null) btnContinue.onClick.AddListener(OnContinue);
            if (btnQuit != null) btnQuit.onClick.AddListener(OnQuit);
            return UniTask.CompletedTask;
        }

        public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
        {
            if (txtTitle != null) txtTitle.text = "Hoc ky quan doi";
            if (btnContinue != null && GameServices.Save != null)
                btnContinue.interactable = GameServices.Save.HasSave;
            AudioManager.Instance?.Play(AudioCueId.Music_MainMenu);
            return UniTask.CompletedTask;
        }

        private void OnNewGame()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            FlowNewGameAsync().Forget();
        }

        private void OnContinue()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            FlowContinueAsync().Forget();
        }

        private void OnQuit()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private async UniTaskVoid FlowNewGameAsync()
        {
            // Show CharacterCreate -> wait name -> travel to World.
            var data = new CharacterCreateData { DefaultName = GameServices.Database != null ? GameServices.Database.playerNameDefault : "Hoc vien" };
            await UIManager.Instance.ShowAsync(UIId.CreateCharacter, new UIContext(data));
            string name = await data.ResultTcs.Task;
            if (!string.IsNullOrEmpty(name))
            {
                GameServices.Player = new PlayerData(name);
                Core.Events.GameEvents.RaiseTimeTick(GameServices.Time.Now); // refresh HUD
                await UIManager.Instance.HideAsync(UIId.MainMenu);
                await TravelToWorldAsync();
            }
        }

        private async UniTaskVoid FlowContinueAsync()
        {
            if (GameServices.Save == null || !GameServices.Save.HasSave) return;
            GameServices.Save.TryLoad(out _);
            await UIManager.Instance.HideAsync(UIId.MainMenu);
            await TravelToWorldAsync();
        }

        private async UniTask TravelToWorldAsync()
        {
            var route = FindWorldRoute();
            if (route == null || GameServices.SceneFlow == null) return;
            await GameServices.SceneFlow.TravelAsync(route);

            // Show GameplayHUD.
            await UIManager.Instance.ShowAsync(UIId.GameplayHud);
        }

        private static SceneRouteSO FindWorldRoute()
        {
            var db = GameServices.Database;
            if (db == null || db.scenes == null) return null;
            for (int i = 0; i < db.scenes.Count; i++)
            {
                var r = db.scenes[i];
                if (r != null && (r.id == "R_World" || r.sceneName == "World")) return r;
            }
            return null;
        }
    }
}

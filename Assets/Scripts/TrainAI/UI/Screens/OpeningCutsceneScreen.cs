using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.Systems.Audio;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace TrainAI.UI.Screens
{
    public class OpeningCutsceneScreen : UIBase
    {
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Button btnSkip;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnSkip != null) btnSkip.onClick.AddListener(OnSkip);
            return UniTask.CompletedTask;
        }

        public override async UniTask OnShownAsync(UIContext ctx, CancellationToken ct)
        {
            AudioManager.Instance?.Play(AudioCueId.Music_Cutscene);
            if (videoPlayer != null && videoPlayer.clip != null)
            {
                videoPlayer.Play();
                while (videoPlayer.isPlaying && !ct.IsCancellationRequested)
                    await UniTask.Yield(ct);
            }
            Luzart.UIManager.Instance.HideAsync(this.Id).Forget();
        }

        private void OnSkip()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            if (videoPlayer != null) videoPlayer.Stop();
            Luzart.UIManager.Instance.HideAsync(this.Id).Forget();
        }
    }
}

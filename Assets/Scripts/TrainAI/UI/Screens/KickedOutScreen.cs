using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using TrainAI.Configs;
using TrainAI.Systems.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    public class KickedOutScreen : UIBase
    {
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Button btnConfirm;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirm);
            if (txtMessage != null) txtMessage.text = "Ban bi duoi hoc!";
            return UniTask.CompletedTask;
        }

        public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
        {
            AudioManager.Instance?.Play(AudioCueId.Score_KickedOut);
            return UniTask.CompletedTask;
        }

        private void OnConfirm()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            Luzart.UIManager.Instance.HideAllExceptSystemAsync().Forget();
            Luzart.UIManager.Instance.HideAsync(this.Id).Forget();
            Luzart.UIManager.Instance.ShowAsync(UIId.MainMenu).Forget();
        }
    }
}

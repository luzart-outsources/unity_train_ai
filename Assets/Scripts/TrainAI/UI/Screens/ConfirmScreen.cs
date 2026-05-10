using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using TrainAI.Configs;
using TrainAI.Systems.Audio;
using TrainAI.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    public class ConfirmScreen : UIBase<ConfirmData>
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Button btnOk;
        [SerializeField] private Button btnCancel;
        [SerializeField] private TextMeshProUGUI txtOkLabel;
        [SerializeField] private TextMeshProUGUI txtCancelLabel;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnOk != null) btnOk.onClick.AddListener(OnOk);
            if (btnCancel != null) btnCancel.onClick.AddListener(OnCancel);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(ConfirmData data, CancellationToken ct)
        {
            if (data == null) return UniTask.CompletedTask;
            if (txtTitle != null) txtTitle.text = data.Title ?? "";
            if (txtMessage != null) txtMessage.text = data.Message ?? "";
            if (txtOkLabel != null) txtOkLabel.text = data.OkLabel ?? "OK";
            if (btnCancel != null) btnCancel.gameObject.SetActive(!string.IsNullOrEmpty(data.CancelLabel));
            if (txtCancelLabel != null) txtCancelLabel.text = data.CancelLabel ?? "";
            return UniTask.CompletedTask;
        }

        private void OnOk()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            Data?.ResultTcs.TrySetResult(true);
            UIManager.Instance.HideAsync(this.Id).Forget();
        }

        private void OnCancel()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            Data?.ResultTcs.TrySetResult(false);
            UIManager.Instance.HideAsync(this.Id).Forget();
        }
    }
}

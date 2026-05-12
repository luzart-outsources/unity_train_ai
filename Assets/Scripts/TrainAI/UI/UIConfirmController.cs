using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIConfirmController : UIScreenBase
    {
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button okButton;
        [SerializeField] Button cancelButton;

        UniTaskCompletionSource<bool> _tcs;

        protected override void Awake()
        {
            base.Awake();
            if (okButton != null) okButton.onClick.AddListener(OnOk);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancel);
        }

        public UniTask<bool> ShowAsync(string text)
        {
            _tcs = new UniTaskCompletionSource<bool>();
            if (bodyText != null) bodyText.text = text ?? "";
            Show();
            return _tcs.Task;
        }

        void OnOk() { _tcs?.TrySetResult(true); Hide(); }
        void OnCancel() { _tcs?.TrySetResult(false); Hide(); }
    }
}

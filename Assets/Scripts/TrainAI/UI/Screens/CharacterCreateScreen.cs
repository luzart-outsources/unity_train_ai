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
    public class CharacterCreateScreen : UIBase<CharacterCreateData>
    {
        [SerializeField] private TMP_InputField inputName;
        [SerializeField] private Button btnConfirm;
        [SerializeField] private TextMeshProUGUI txtPrompt;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirm);
            if (txtPrompt != null) txtPrompt.text = "Ban can dien ten truoc khi vao game";
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(CharacterCreateData data, CancellationToken ct)
        {
            if (data == null) return UniTask.CompletedTask;
            if (inputName != null) inputName.text = data.DefaultName ?? "";
            return UniTask.CompletedTask;
        }

        private void OnConfirm()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            string name = inputName != null ? inputName.text : "Hoc vien";
            if (string.IsNullOrWhiteSpace(name)) name = "Hoc vien";
            Data?.ResultTcs.TrySetResult(name);
            Luzart.UIManager.Instance.HideAsync(this.Id).Forget();
        }
    }
}

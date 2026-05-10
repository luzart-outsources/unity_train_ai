using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using TrainAI.UI.Components;

namespace TrainAI.UI.Screens
{
    public class LoadingScreen : UIBase<LoadingData>
    {
        [UnityEngine.SerializeField] private TextMeshProUGUI txtMessage;

        protected override UniTask OnBeforeShowAsync(LoadingData data, CancellationToken ct)
        {
            if (txtMessage != null && data != null)
                txtMessage.text = string.IsNullOrEmpty(data.Text) ? "Dang chuyen canh..." : data.Text;
            return UniTask.CompletedTask;
        }
    }
}

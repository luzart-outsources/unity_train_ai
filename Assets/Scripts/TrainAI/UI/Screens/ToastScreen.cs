using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    // Toast: lane Toast, allow multi-instance, auto-fade.
    public class ToastScreen : UIBase<ToastData>
    {
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image imgBg;

        protected override async UniTask OnShownAsync(ToastData data, CancellationToken ct)
        {
            if (data == null) return;
            if (txtMessage != null) txtMessage.text = data.Message;
            if (imgBg != null) imgBg.color = StyleColor(data.Style);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                await UniTask.Delay((int)(data.DurationSeconds * 1000), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
                // Fade out 0.3s.
                float t = 0f;
                while (t < 0.3f && canvasGroup != null && !ct.IsCancellationRequested)
                {
                    t += UnityEngine.Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.3f);
                    await UniTask.Yield(ct);
                }
            }
            else
            {
                await UniTask.Delay((int)(data.DurationSeconds * 1000), cancellationToken: ct);
            }
            UIManager.Instance.HideAsync(this.Id).Forget();
        }

        private static Color StyleColor(ToastStyle s)
        {
            switch (s)
            {
                case ToastStyle.Success: return new Color(0.2f, 0.7f, 0.2f, 0.9f);
                case ToastStyle.Warning: return new Color(0.9f, 0.6f, 0.2f, 0.9f);
                case ToastStyle.Error:   return new Color(0.8f, 0.2f, 0.2f, 0.9f);
                default:                 return new Color(0.2f, 0.4f, 0.7f, 0.9f);
            }
        }
    }
}

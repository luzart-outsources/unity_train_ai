using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace TrainAI.UI
{
    public class UILoadingController : UIScreenBase
    {
        [SerializeField] TMP_Text label;
        [SerializeField] RectTransform spinner;
        [SerializeField] float spinRpm = 60f;

        bool _spinning;

        public async UniTask ShowAsync(string text, float seconds)
        {
            if (label != null) label.text = text ?? "";
            Show();
            _spinning = true;
            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < seconds)
            {
                if (spinner != null)
                    spinner.Rotate(0, 0, -spinRpm * 6f * Time.unscaledDeltaTime);
                await UniTask.Yield();
            }
            _spinning = false;
            Hide();
        }
    }
}

using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIExpelController : UIScreenBase
    {
        [SerializeField] TMP_Text bodyText;

        protected override void Awake()
        {
            base.Awake();
            BroadcastService.Subscribe<ExpelTriggeredMsg>(OnExpel);
        }

        void OnDestroy() => BroadcastService.Unsubscribe<ExpelTriggeredMsg>(OnExpel);

        void OnExpel(ExpelTriggeredMsg _)
        {
            if (bodyText != null)
                bodyText.text = "Dong chi da bi duoi hoc do khong giu duoc ki luat.";
            Show();
        }
    }
}

using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIEndingController : UIScreenBase
    {
        [SerializeField] TMP_Text headlineText;
        [SerializeField] TMP_Text bodyText;

        protected override void Awake()
        {
            base.Awake();
            BroadcastService.Subscribe<GameEndedMsg>(OnGameEnded);
        }

        void OnDestroy() => BroadcastService.Unsubscribe<GameEndedMsg>(OnGameEnded);

        void OnGameEnded(GameEndedMsg m) => ShowGrade((EndingGrade)m.gradeIndex);

        public void ShowGrade(EndingGrade grade)
        {
            string head = grade switch
            {
                EndingGrade.XuatSac => "XUAT SAC",
                EndingGrade.Tot => "TOT",
                _ => "TRUNG BINH"
            };
            string body = grade switch
            {
                EndingGrade.XuatSac => "Dong chi da hoan thanh xuat sac hoc ky quan su.",
                EndingGrade.Tot => "Dong chi da hoan thanh tot hoc ky quan su.",
                _ => "Dong chi da hoan thanh hoc ky quan su o muc trung binh."
            };
            if (headlineText != null) headlineText.text = head;
            if (bodyText != null) bodyText.text = body;
            Show();
        }
    }
}

using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.UI
{
    public class UIScoreHUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text hocTapText;
        [SerializeField] TMP_Text renLuyenText;
        [SerializeField] PlayerStateRSO playerState;

        void OnEnable()
        {
            BroadcastService.Subscribe<ScoreChangedMsg>(OnScore);
            Refresh(playerState != null ? playerState.hocTap : 0,
                    playerState != null ? playerState.renLuyen : 0);
        }

        void OnDisable() => BroadcastService.Unsubscribe<ScoreChangedMsg>(OnScore);

        void OnScore(ScoreChangedMsg m) => Refresh(m.hocTap, m.renLuyen);

        void Refresh(int hocTap, int renLuyen)
        {
            if (hocTapText != null) hocTapText.text = $"Hoc tap: {hocTap}/480";
            if (renLuyenText != null) renLuyenText.text = $"Ren luyen: {renLuyen}/100";
        }
    }
}

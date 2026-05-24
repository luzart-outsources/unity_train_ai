using TMPro;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIScoreHUDController : MonoBehaviour
    {
        [SerializeField] TMP_Text hocTapText;
        [SerializeField] TMP_Text renLuyenText;
        [SerializeField] PlayerStateRSO playerState;

        // Avatar = circle frame with the player's first-name initial. Avoids
        // shipping portrait art for every possible character — initial-style
        // avatars are widely understood from Gmail/Slack/Discord defaults.
        // Name text sits to the right of the avatar so the whole "who you are"
        // block reads as one chip.
        [SerializeField] TMP_Text avatarLetterText;
        [SerializeField] TMP_Text playerNameText;

        void OnEnable()
        {
            BroadcastService.Subscribe<ScoreChangedMsg>(OnScore);
            Refresh(playerState != null ? playerState.hocTap : 0,
                    playerState != null ? playerState.renLuyen : 0);
            RefreshPlayerInfo();
        }

        void OnDisable() => BroadcastService.Unsubscribe<ScoreChangedMsg>(OnScore);

        void OnScore(ScoreChangedMsg m) => Refresh(m.hocTap, m.renLuyen);

        void Refresh(int hocTap, int renLuyen)
        {
            if (hocTapText != null) hocTapText.text = $"Hoc tap: {hocTap}/480";
            if (renLuyenText != null) renLuyenText.text = $"Ren luyen: {renLuyen}/100";
        }

        void RefreshPlayerInfo()
        {
            string name = playerState != null && !string.IsNullOrEmpty(playerState.playerName)
                          ? playerState.playerName
                          : "Hoc vien";
            if (playerNameText != null) playerNameText.text = name;
            if (avatarLetterText != null)
            {
                // First non-whitespace letter, uppercased. Fallback "H" so
                // the avatar never renders blank.
                char c = 'H';
                foreach (var ch in name)
                {
                    if (!char.IsWhiteSpace(ch)) { c = char.ToUpperInvariant(ch); break; }
                }
                avatarLetterText.text = c.ToString();
            }
        }
    }
}

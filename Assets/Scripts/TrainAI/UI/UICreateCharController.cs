using TMPro;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UICreateCharController : UIScreenBase
    {
        [SerializeField] TMP_Text headerText;
        [SerializeField] TMP_InputField nameInput;
        [SerializeField] Button confirmButton;
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] PlayerStateRSO playerState;
        [SerializeField] GameClockRSO clock;
        [SerializeField] SceneRefSO worldScene;
        [SerializeField] int startDay = 1;

        protected override void Awake()
        {
            base.Awake();
            if (headerText != null) headerText.text = "Ban can dien ten truoc khi vao game";
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirm);
            Show();
        }

        async void OnConfirm()
        {
            if (nameInput == null) return;
            string name = (nameInput.text ?? "").Trim();
            if (string.IsNullOrEmpty(name))
            {
                if (services != null && services.UI != null) await services.UI.ShowConfirm("Vui long dien ten.");
                return;
            }
            if (playerState != null) { playerState.Reset(); playerState.playerName = name; }
            if (clock != null) clock.Reset();
            if (services != null && services.Scenes != null && worldScene != null)
                await services.Scenes.LoadSingle(worldScene);
            services?.Quests?.StartDay(startDay);
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.UI.HUD;
using UnityEngine;

namespace TrainAI.UI.Screens
{
    public class GameplayHud : UIBase
    {
        [Header("HUD widgets")]
        [SerializeField] private ClockHUD clockHud;
        [SerializeField] private QuestHUD questHud;
        [SerializeField] private ScoreHUD scoreHud;
        [SerializeField] private InteractButton interactButton;
        [SerializeField] private JoystickWidget joystick;
        [SerializeField] private MiniMapHUD miniMap;
        [SerializeField] private AvatarHUD avatar;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            // Widget tu subscribe events trong Awake/OnEnable.
            return UniTask.CompletedTask;
        }

        public override UniTask OnPauseAsync(CancellationToken ct)
        {
            // Disable child widget update khi popup khac de len.
            if (clockHud != null) clockHud.enabled = false;
            if (questHud != null) questHud.enabled = false;
            if (scoreHud != null) scoreHud.enabled = false;
            return UniTask.CompletedTask;
        }

        public override UniTask OnResumeAsync(CancellationToken ct)
        {
            if (clockHud != null) clockHud.enabled = true;
            if (questHud != null) questHud.enabled = true;
            if (scoreHud != null) scoreHud.enabled = true;
            return UniTask.CompletedTask;
        }
    }
}

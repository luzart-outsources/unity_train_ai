using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using TrainAI.Core.Time;
using UnityEngine;

namespace TrainAI.World
{
    // NPC stub - chua wire Phase B PPO. NPC dung yen va co InteractableSO de chat duoc.
    // Khi co schedule, target Anchor cua location khop voi locationKey trong scene -
    // Quyen co the implement movement sau bang cach extend file nay.
    public class NPCController : MonoBehaviour
    {
        [SerializeField] private NPCProfileSO profile;
        [SerializeField] private float minHourMoveDistance = 0f;

        public NPCProfileSO Profile => profile;
        public void SetProfile(NPCProfileSO p) => profile = p;

        private void OnEnable()
        {
            GameEvents.TimeTick += OnTick;
        }

        private void OnDisable()
        {
            GameEvents.TimeTick -= OnTick;
        }

        private void OnTick(GameTime t)
        {
            if (profile == null) return;
            if (profile.aiMode != NPCAIMode.Movement && profile.aiMode != NPCAIMode.ChatAndMovement) return;
            // Phase B not wired: log moi gio de Quyen biet schedule dang chay.
            // Implementation thuc: navigate to anchor cua locationKey at hour.
            if (profile.schedule != null && t.minute == 0)
            {
                string loc = profile.schedule.GetLocationAtHour(t.hour);
                if (!string.IsNullOrEmpty(loc))
                    Debug.Log($"[NPC {profile.displayName}] hour {t.hour} -> location {loc} (movement TBD).");
            }
        }
    }
}

using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.Services;
using UnityEngine;

namespace TrainAI.Presentation
{
    [DefaultExecutionOrder(-500)]
    public class GameLoopDriver : MonoBehaviour
    {
        [SerializeField] ServiceLocatorSO services;
        [SerializeField] int autoSaveSlot = 1;

        void OnEnable()
        {
            BroadcastService.Subscribe<DayStartedMsg>(OnDayStarted);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<DayStartedMsg>(OnDayStarted);
        }

        void Update()
        {
            if (services == null || !services.IsBootstrapped) return;
            float dt = Time.deltaTime;
            services.Clock?.Tick(dt);
            services.Quests?.Tick(dt);
            services.Movement?.Tick(dt);
            services.NPCs?.Tick(dt);
        }

        void OnDayStarted(DayStartedMsg _)
        {
            // Auto-save at the start of each new day so Continue resumes at "next morning".
            // DayStartedMsg fires after the day counter increments in GameClockService.
            services?.Save?.Save(autoSaveSlot);
        }

        void OnApplicationQuit()
        {
            services?.Save?.Save(autoSaveSlot);
        }
    }
}

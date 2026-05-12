using TrainAI.Services;
using UnityEngine;

namespace TrainAI.Presentation
{
    [DefaultExecutionOrder(-500)]
    public class GameLoopDriver : MonoBehaviour
    {
        [SerializeField] ServiceLocatorSO services;

        void Update()
        {
            if (services == null || !services.IsBootstrapped) return;
            float dt = Time.deltaTime;
            services.Clock?.Tick(dt);
            services.Quests?.Tick(dt);
            services.Movement?.Tick(dt);
            services.NPCs?.Tick(dt);
        }
    }
}

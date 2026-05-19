using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    public class InteractionRouterBridge : MonoBehaviour
    {
        // Public so BootstrapEntry can wire it at runtime when adding the
        // component to the DDOL Bootstrap GameObject (Unity still serializes
        // public fields the same as [SerializeField] private ones).
        public ServiceLocatorSO services;

        public void Wire(ServiceLocatorSO svc) => services = svc;

        // Singleton guard: the canonical bridge lives on the DDOL Bootstrap
        // GameObject. Legacy scenes (10_World was the original home) used to
        // ship with their own bridge GameObject; when those scenes load,
        // duplicate bridges would each subscribe to InteractPressedMsg and
        // every E-press would fire HandlePressed twice. Self-destruct any
        // late-spawning copies so the DDOL one stays canonical.
        static InteractionRouterBridge _instance;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        void OnEnable()
        {
            if (_instance != this) return;
            BroadcastService.Subscribe<InteractPressedMsg>(OnPressed);
        }
        void OnDisable()
        {
            if (_instance != this) return;
            BroadcastService.Unsubscribe<InteractPressedMsg>(OnPressed);
        }
        void OnDestroy() { if (_instance == this) _instance = null; }

        async void OnPressed(InteractPressedMsg msg)
        {
            if (services == null || !services.IsBootstrapped) return;
            var inter = msg.target as InteractableSO;
            if (inter == null || services.Interactions == null) return;
            // async void: any uncaught exception inside this await chain
            // becomes a Unity unhandled-exception, which kills the editor's
            // input pump on Windows and looks like a hard crash to the
            // player. Wrap the boundary so a single bad interaction (missing
            // scene, null UI strategy) logs instead of bricks.
            try { await services.Interactions.HandlePressed(inter); }
            catch (System.Exception e) { Debug.LogError($"[InteractionBridge] '{inter.name}' failed: {e}"); }
        }
    }
}

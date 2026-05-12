using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    public class InteractionRouterBridge : MonoBehaviour
    {
        [SerializeField] ServiceLocatorSO services;

        void OnEnable() => BroadcastService.Subscribe<InteractPressedMsg>(OnPressed);
        void OnDisable() => BroadcastService.Unsubscribe<InteractPressedMsg>(OnPressed);

        async void OnPressed(InteractPressedMsg msg)
        {
            if (services == null || !services.IsBootstrapped) return;
            var inter = msg.target as InteractableSO;
            if (inter == null || services.Interactions == null) return;
            await services.Interactions.HandlePressed(inter);
        }
    }
}

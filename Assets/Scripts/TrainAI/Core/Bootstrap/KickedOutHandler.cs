using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Core.Events;
using TrainAI.UI.Components;
using UnityEngine;

namespace TrainAI.Core.Bootstrap
{
    // Subscribe KickedOut event va show KickedOut popup.
    public class KickedOutHandler : MonoBehaviour
    {
        private void OnEnable() => GameEvents.KickedOut += OnKickedOut;
        private void OnDisable() => GameEvents.KickedOut -= OnKickedOut;

        private void OnKickedOut()
        {
            if (Luzart.UIManager.Instance != null)
                Luzart.UIManager.Instance.ShowAsync(UIIdGame.KickedOut, default, default).Forget();
        }
    }
}

using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Presentation
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float facingDot = 0.3f;
        [SerializeField] Key interactKey = Key.E;

        readonly HashSet<InteractableMarker> _inRange = new();
        InteractableMarker _current;

        void OnTriggerEnter(Collider other)
        {
            var m = other.GetComponentInParent<InteractableMarker>();
            if (m == null || m.interactable == null) return;
            _inRange.Add(m);
            BroadcastService.Send(new InteractZoneEnteredMsg(m.interactable));
        }

        void OnTriggerExit(Collider other)
        {
            var m = other.GetComponentInParent<InteractableMarker>();
            if (m == null || m.interactable == null) return;
            _inRange.Remove(m);
            BroadcastService.Send(new InteractZoneExitedMsg(m.interactable));
            if (_current == m) _current = null;
        }

        void Update()
        {
            _current = null;
            float best = facingDot;
            foreach (var m in _inRange)
            {
                if (m == null) continue;
                Vector3 toIt = (m.transform.position - transform.position);
                toIt.y = 0;
                if (toIt.sqrMagnitude < 0.0001f) { _current = m; break; }
                float d = Vector3.Dot(transform.forward, toIt.normalized);
                if (d > best) { best = d; _current = m; }
            }

            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame && _current != null && _current.interactable != null)
                BroadcastService.Send(new InteractPressedMsg(_current.interactable));
        }
    }
}

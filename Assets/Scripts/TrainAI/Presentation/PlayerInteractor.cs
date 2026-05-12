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
        [SerializeField] float facingDot = 0.5f;
        [SerializeField] InputActionReference interactAction;

        readonly HashSet<InteractableMarker> _inRange = new();
        InteractableMarker _current;

        void OnEnable()
        {
            interactAction?.action?.Enable();
            if (interactAction != null && interactAction.action != null)
                interactAction.action.performed += OnInteract;
        }

        void OnDisable()
        {
            if (interactAction != null && interactAction.action != null)
                interactAction.action.performed -= OnInteract;
            interactAction?.action?.Disable();
        }

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
        }

        void OnInteract(InputAction.CallbackContext _)
        {
            if (_current == null || _current.interactable == null) return;
            BroadcastService.Send(new InteractPressedMsg(_current.interactable));
        }
    }
}

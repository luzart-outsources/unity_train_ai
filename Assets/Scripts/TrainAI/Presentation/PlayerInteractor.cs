using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Presentation
{
    /// Polls Physics.OverlapSphere each Update to find nearby InteractableMarkers.
    /// Polling is more reliable than OnTriggerEnter when the player's collider chain
    /// mixes a CharacterController on the root with a separate trigger SphereCollider
    /// on a child — Unity's dispatch rules in that setup are subtle. OverlapSphere
    /// always works regardless of Rigidbody/CharacterController placement.
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] float interactRadius = 2f;
        [SerializeField] float facingDot = 0.3f;
        [SerializeField] Key interactKey = Key.E;
        [SerializeField] LayerMask probeMask = ~0; // all layers

        // Both sets are reused across frames — earlier versions allocated a
        // fresh HashSet<> + a closure-capturing RemoveWhere lambda EVERY frame,
        // which under sustained gameplay caused per-frame GC pressure and
        // pause spikes that looked like crashes/freezes on weaker hardware.
        readonly HashSet<InteractableMarker> _inRange = new();
        readonly HashSet<InteractableMarker> _seenThisFrame = new();
        readonly List<InteractableMarker> _toRemove = new();
        InteractableMarker _current;

        void Update()
        {
            PollNearby();
            UpdateCurrent();

            var kb = Keyboard.current;
            if (kb != null && kb[interactKey].wasPressedThisFrame
                && _current != null && _current.interactable != null)
            {
                BroadcastService.Send(new InteractPressedMsg(_current.interactable));
            }
        }

        static readonly Collider[] _hits = new Collider[32];

        void PollNearby()
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position + Vector3.up * 0.9f,
                interactRadius, _hits, probeMask, QueryTriggerInteraction.Collide);

            _seenThisFrame.Clear();
            for (int i = 0; i < count; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                var m = col.GetComponentInParent<InteractableMarker>();
                if (m == null || m.interactable == null) continue;
                _seenThisFrame.Add(m);
                if (_inRange.Add(m))
                    BroadcastService.Send(new InteractZoneEnteredMsg(m.interactable));
            }

            // Two-pass remove: collect into _toRemove first to avoid the
            // RemoveWhere(lambda) allocation. Capture-free, alloc-free.
            _toRemove.Clear();
            foreach (var m in _inRange)
            {
                if (m == null) { _toRemove.Add(m); continue; }
                if (!_seenThisFrame.Contains(m)) _toRemove.Add(m);
            }
            for (int i = 0; i < _toRemove.Count; i++)
            {
                var m = _toRemove[i];
                _inRange.Remove(m);
                if (m != null && m.interactable != null)
                    BroadcastService.Send(new InteractZoneExitedMsg(m.interactable));
                if (_current == m) _current = null;
            }
        }

        void UpdateCurrent()
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
    }
}

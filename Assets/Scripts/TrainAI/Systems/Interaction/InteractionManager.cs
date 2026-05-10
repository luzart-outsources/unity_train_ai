using System;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Systems.Audio;
using UnityEngine;

namespace TrainAI.Systems.Interaction
{
    // Trang thai tuong tac global: player co interactable trong tam khong.
    // KHONG MonoBehaviour - subscribed by InteractableTrigger.
    public class InteractionManager
    {
        private InteractableSO _current;
        private Transform _currentTransform;

        public InteractableSO Current => _current;
        public Transform CurrentTransform => _currentTransform;

        public event Action<InteractableSO> OnEnter;
        public event Action OnExit;
        public event Action<InteractableSO> OnActivated;

        public void Enter(InteractableSO data, Transform t)
        {
            _current = data;
            _currentTransform = t;
            OnEnter?.Invoke(data);
        }

        public void Exit(InteractableSO data)
        {
            if (_current == data)
            {
                _current = null;
                _currentTransform = null;
                OnExit?.Invoke();
            }
        }

        public void Clear()
        {
            _current = null;
            _currentTransform = null;
            OnExit?.Invoke();
        }

        // Goi tu nut UI Interact hoac InputReader.
        public void Activate()
        {
            if (_current == null) return;
            var qm = GameServices.Quest;
            var current = _current;

            if (current.kind == InteractableKind.SceneDoor)
            {
                AudioManager.Instance?.Play(AudioCueId.Player_Interact);
                if (current.doorTarget != null && GameServices.SceneFlow != null)
                    GameServices.SceneFlow.TravelAsync(current.doorTarget).Forget();
                OnActivated?.Invoke(current);
                return;
            }

            if (current.kind == InteractableKind.NPC)
            {
                AudioManager.Instance?.Play(AudioCueId.Player_Interact);
                if (current.npcProfile != null && GameServices.Dialogue != null)
                    GameServices.Dialogue.OpenDialogueAsync(current.npcProfile).Forget();
                OnActivated?.Invoke(current);
                return;
            }

            // QuestPoint: chi cho phep tuong tac neu khop voi quest hien tai.
            var q = qm != null ? qm.CurrentQuest : null;
            if (q == null) return;
            if (q.interactableLocationKey != current.key)
            {
                AudioManager.Instance?.Play(AudioCueId.UI_ToastWarning);
                Debug.Log($"[Interaction] '{current.key}' khong phai quest hien tai ({q.interactableLocationKey}).");
                return;
            }

            AudioManager.Instance?.Play(AudioCueId.Player_Interact);
            qm.StartCurrentQuest();
            // QuestRunner se chay async tu QuestRunnerHost.
            OnActivated?.Invoke(current);
        }
    }
}

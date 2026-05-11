using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.World
{
    // Mui ten o duoi chan player tro toi vi tri nhiem vu hien tai.
    // Tu tim Interactable theo QuestDef.interactableLocationKey moi khi quest doi.
    // An khi khong co quest hoac target khong co trong scene hien tai.
    public class QuestArrow : MonoBehaviour
    {
        [SerializeField] private Renderer[] visualRenderers;

        [Header("Bobbing")]
        [SerializeField] private float bobAmount = 0.05f;
        [SerializeField] private float bobSpeed = 3f;

        [Header("Refresh")]
        [SerializeField] private float retryInterval = 0.5f;

        private Transform _target;
        private string _cachedKey;
        private float _retryTimer;
        private Vector3 _basePos;

        private void Awake()
        {
            // Auto-discover Renderers neu chua wire.
            if (visualRenderers == null || visualRenderers.Length == 0)
                visualRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _basePos = transform.localPosition;
        }

        private void OnEnable()
        {
            GameEvents.QuestChanged += OnQuestChanged;
            RefreshTarget();
        }

        private void OnDisable()
        {
            GameEvents.QuestChanged -= OnQuestChanged;
        }

        private void OnQuestChanged(QuestDefSO q) => RefreshTarget();

        private void RefreshTarget()
        {
            _target = null;
            var q = GameServices.Quest != null ? GameServices.Quest.CurrentQuest : null;
            if (q == null || string.IsNullOrEmpty(q.interactableLocationKey))
            {
                _cachedKey = null;
                return;
            }
            _cachedKey = q.interactableLocationKey;
            _target = FindByKey(_cachedKey);
        }

        private static Transform FindByKey(string key)
        {
            var triggers = FindObjectsByType<InteractableTrigger>(FindObjectsSortMode.None);
            for (int i = 0; i < triggers.Length; i++)
            {
                var t = triggers[i];
                if (t == null || t.Data == null) continue;
                if (t.Data.key == key) return t.transform;
            }
            return null;
        }

        private void Update()
        {
            // Retry tim target khi target null (scene moi vua load, anchor xuat hien sau, ...).
            _retryTimer -= UnityEngine.Time.deltaTime;
            if (_retryTimer <= 0f)
            {
                _retryTimer = retryInterval;
                if (_target == null) RefreshTarget();
            }

            bool visible = _target != null;
            SetVisible(visible);
            if (!visible) return;

            // Orient world rotation toward target.
            Vector3 dir = _target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir);

            // Bob up-down (local Y).
            var lp = _basePos;
            lp.y += Mathf.Sin(UnityEngine.Time.time * bobSpeed) * bobAmount;
            transform.localPosition = lp;
        }

        private void SetVisible(bool v)
        {
            if (visualRenderers == null) return;
            for (int i = 0; i < visualRenderers.Length; i++)
                if (visualRenderers[i] != null) visualRenderers[i].enabled = v;
        }
    }
}

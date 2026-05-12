using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    public class QuestArrowHUD : MonoBehaviour
    {
        [SerializeField] Transform arrow;
        [SerializeField] float headOffset = 2.5f;
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobFrequency = 1.5f;
        [SerializeField] float pulseMin = 0.9f;
        [SerializeField] float pulseMax = 1.1f;

        Vector3 _baseScale;
        Vector3 _baseLocalPos;
        QuestSO _activeQuest;

        void Awake()
        {
            if (arrow == null && transform.childCount > 0)
                arrow = transform.GetChild(0);

            if (arrow != null)
            {
                // Force arrow to sit above the player's head so it's always visible.
                var lp = arrow.localPosition;
                if (lp.y < headOffset - 0.01f) lp.y = headOffset;
                arrow.localPosition = lp;

                _baseScale = arrow.localScale;
                _baseLocalPos = arrow.localPosition;
                arrow.gameObject.SetActive(false);
            }
        }

        void OnEnable()
        {
            BroadcastService.Subscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Subscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Subscribe<QuestMissedMsg>(OnEnded);
        }

        void OnDisable()
        {
            BroadcastService.Unsubscribe<QuestActivatedMsg>(OnActivated);
            BroadcastService.Unsubscribe<QuestCompletedMsg>(OnEnded);
            BroadcastService.Unsubscribe<QuestMissedMsg>(OnEnded);
        }

        void OnActivated(QuestActivatedMsg m)
        {
            _activeQuest = m.quest as QuestSO;
            if (arrow != null) arrow.gameObject.SetActive(_activeQuest != null);
        }

        void OnEnded<T>(T _)
        {
            _activeQuest = null;
            if (arrow != null) arrow.gameObject.SetActive(false);
        }

        void Update()
        {
            if (arrow == null || _activeQuest == null || _activeQuest.area == null) return;

            Vector3 targetPos = _activeQuest.area.worldPos;
            // Arrow points horizontally toward the area, from its anchored position above head.
            Vector3 worldArrow = arrow.position;
            Vector3 look = new Vector3(targetPos.x, worldArrow.y, targetPos.z);
            if ((look - worldArrow).sqrMagnitude > 0.001f)
                arrow.LookAt(look);

            float t = Time.time;
            float bob = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            arrow.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
            float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(t * 3f) + 1f) * 0.5f);
            arrow.localScale = _baseScale * pulse;
        }
    }
}

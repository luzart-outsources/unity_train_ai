using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    public class QuestArrowHUD : MonoBehaviour
    {
        [SerializeField] Transform arrow;
        [SerializeField] float bobAmplitude = 0.05f;
        [SerializeField] float bobFrequency = 2f;
        [SerializeField] float pulseMin = 0.95f;
        [SerializeField] float pulseMax = 1.05f;

        Vector3 _baseScale;
        Vector3 _baseLocalPos;
        QuestSO _activeQuest;

        void Awake()
        {
            if (arrow != null)
            {
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
            Vector3 look = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            arrow.LookAt(look);

            float t = Time.time;
            float bob = Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            arrow.localPosition = _baseLocalPos + new Vector3(0f, bob, 0f);
            float pulse = Mathf.Lerp(pulseMin, pulseMax, (Mathf.Sin(t * 3f) + 1f) * 0.5f);
            arrow.localScale = _baseScale * pulse;
        }
    }
}

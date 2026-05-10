using TMPro;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.UI.HUD
{
    public class QuestHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtQuestTitle;
        [SerializeField] private TextMeshProUGUI txtQuestTime;

        private void OnEnable()
        {
            GameEvents.QuestChanged += OnQuestChanged;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.QuestChanged -= OnQuestChanged;
        }

        private void OnQuestChanged(QuestDefSO q)
        {
            Render(q);
        }

        private void Refresh()
        {
            var q = GameServices.Quest != null ? GameServices.Quest.CurrentQuest : null;
            Render(q);
        }

        private void Render(QuestDefSO q)
        {
            if (q == null)
            {
                if (txtQuestTitle != null) txtQuestTitle.text = "Khong co nhiem vu";
                if (txtQuestTime != null) txtQuestTime.text = "";
                return;
            }
            if (txtQuestTitle != null) txtQuestTitle.text = q.title;
            if (txtQuestTime != null)
                txtQuestTime.text = $"({q.deadlineHour:D2}:{q.deadlineMinute:D2})";
        }
    }
}

using TMPro;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.HUD
{
    public class ScoreHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI txtDiscipline;
        [SerializeField] private TextMeshProUGUI txtAcademic;
        [SerializeField] private Image disciplineBar;
        [SerializeField] private Image academicBar;

        private void OnEnable()
        {
            GameEvents.DisciplineChanged += OnDiscipline;
            GameEvents.AcademicChanged += OnAcademic;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.DisciplineChanged -= OnDiscipline;
            GameEvents.AcademicChanged -= OnAcademic;
        }

        private void Refresh()
        {
            var s = GameServices.Score;
            if (s == null) return;
            UpdateDiscipline(s.Discipline);
            UpdateAcademic(s.Academic);
        }

        private void OnDiscipline(int v, int d) => UpdateDiscipline(v);
        private void OnAcademic(int v, int d) => UpdateAcademic(v);

        private void UpdateDiscipline(int v)
        {
            int max = GameServices.Score != null && GameServices.Score.Config != null
                ? GameServices.Score.Config.maxDiscipline : 100;
            if (txtDiscipline != null) txtDiscipline.text = $"{v}/{max}";
            if (disciplineBar != null) disciplineBar.fillAmount = max > 0 ? (float)v / max : 0;
        }

        private void UpdateAcademic(int v)
        {
            int max = GameServices.Score != null && GameServices.Score.Config != null
                ? GameServices.Score.Config.maxAcademic : 480;
            if (txtAcademic != null) txtAcademic.text = $"{v}/{max}";
            if (academicBar != null) academicBar.fillAmount = max > 0 ? (float)v / max : 0;
        }
    }
}

using Luzart.NewBase;
using TMPro;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.HUD
{
    // GDD: 'Goc tay trai o tren se co hinh mat nguoi (co dinh), diem nguoi choi dang co'.
    // Avatar mood thay doi theo discipline qua SelectSwitchImage (3 sprite: vui/binh thuong/buon).
    public class AvatarHUD : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private TextMeshProUGUI txtName;
        [SerializeField] private TextMeshProUGUI txtPoints;
        [SerializeField] private SelectSwitchImage moodSelector;

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

        private void OnDiscipline(int v, int d) { Refresh(); }
        private void OnAcademic(int v, int d) { Refresh(); }

        private void Refresh()
        {
            string name = GameServices.Player != null ? GameServices.Player.Name : "Hoc vien";
            int disc = GameServices.Score != null ? GameServices.Score.Discipline : 100;
            int acad = GameServices.Score != null ? GameServices.Score.Academic : 0;
            if (txtName != null) txtName.text = name;
            if (txtPoints != null) txtPoints.text = $"RL:{disc}  HT:{acad}";
            if (moodSelector != null)
            {
                int mood = disc >= 80 ? 0 : disc >= 50 ? 1 : 2;
                moodSelector.Select(mood);
            }
        }
    }
}

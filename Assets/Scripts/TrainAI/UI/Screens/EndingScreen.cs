using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using Luzart.NewBase;
using TMPro;
using TrainAI.Configs;
using TrainAI.Systems.Audio;
using TrainAI.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.Screens
{
    public class EndingScreen : UIBase<EndingData>
    {
        [SerializeField] private TextMeshProUGUI txtTitle;
        [SerializeField] private TextMeshProUGUI txtAcademic;
        [SerializeField] private TextMeshProUGUI txtDiscipline;
        [SerializeField] private TextMeshProUGUI txtRank;
        [SerializeField] private SelectSwitchTMP_Text rankSelector;
        [SerializeField] private Button btnBackToMenu;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            if (btnBackToMenu != null) btnBackToMenu.onClick.AddListener(OnBack);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(EndingData data, CancellationToken ct)
        {
            if (data == null) return UniTask.CompletedTask;
            if (txtTitle != null) txtTitle.text = "Ban da hoan thanh khoa hoc quan su!";
            if (txtAcademic != null) txtAcademic.text = $"Diem hoc tap: {data.Academic}/480 ({data.AcademicGrade})";
            if (txtDiscipline != null) txtDiscipline.text = $"Diem ren luyen: {data.Discipline}/100 ({data.DisciplineGrade})";

            // Take min grade as overall rank (worst case).
            string overall = WorstGrade(data.AcademicGrade, data.DisciplineGrade);
            if (txtRank != null) txtRank.text = $"Tot nghiep loai: {overall}";

            // Select rankSelector based on grade index: XS=0, Tot=1, TB=2.
            if (rankSelector != null) rankSelector.Select(GradeIndex(overall));

            AudioManager.Instance?.Play(AudioCueId.Music_Ending);
            return UniTask.CompletedTask;
        }

        private void OnBack()
        {
            AudioManager.Instance?.Play(AudioCueId.UI_Click);
            Luzart.UIManager.Instance.HideAsync(this.Id).Forget();
            Luzart.UIManager.Instance.ShowAsync(UIId.MainMenu).Forget();
        }

        private static string WorstGrade(string a, string b)
        {
            // Order: TB > Tot > XS (TB la te nhat, ranking nguoc).
            int ai = GradeIndex(a);
            int bi = GradeIndex(b);
            int worst = Mathf.Max(ai, bi);
            switch (worst)
            {
                case 0: return "Xuat sac";
                case 1: return "Tot";
                default: return "Trung binh";
            }
        }

        private static int GradeIndex(string g)
        {
            if (string.IsNullOrEmpty(g)) return 2;
            if (g.StartsWith("Xuat")) return 0;
            if (g.StartsWith("Tot")) return 1;
            return 2;
        }
    }
}

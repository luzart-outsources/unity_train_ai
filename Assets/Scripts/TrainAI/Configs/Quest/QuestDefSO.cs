using Luzart;
using UnityEngine;

namespace TrainAI.Configs
{
    // Dinh nghia 1 nhiem vu trong ngay - quan trong nhat trong he config.
    [CreateAssetMenu(menuName = "TrainAI/Quest/Quest Def", fileName = "Q_Quest")]
    public class QuestDefSO : ScriptableObject
    {
        [Foldout("Identity")]
        [InfoBox("ID UNIQUE trong toan project. Convention: Q_<Type>_<HourMinute>.")]
        public string id = "Q_Exercise_0500";

        [Foldout("Identity")]
        public string title = "Tap the duc";

        [Foldout("Identity")]
        [TextArea(1, 3)]
        public string descriptionTemplate = "{title} ({deadline})";

        [Foldout("Type & Time")]
        public QuestType type = QuestType.Exercise;

        [Foldout("Type & Time")]
        [Slider(0, 23)] public int startHour = 5;
        [Foldout("Type & Time")]
        [Slider(0, 59)] public int startMinute = 0;

        [Foldout("Type & Time")]
        [InfoBox("GDD: late = chua start sau 15p tinh tu deadline -> tru -5d ren luyen.", InfoBoxType.Warning)]
        [Slider(0, 23)] public int deadlineHour = 5;
        [Foldout("Type & Time")]
        [Slider(0, 59)] public int deadlineMinute = 15;

        [Foldout("Type & Time")]
        [InfoBox("Sau khi xong, jump thoi gian toi gio X (GDD: 'Thoi gian chuyen toi Xh').")]
        [Slider(0, 23)] public int skipToHour = 6;
        [Foldout("Type & Time")]
        [Slider(0, 59)] public int skipToMinute = 0;

        [Foldout("Location")]
        [Tooltip("Neu null, lam quest tai chinh scene World.")]
        public SceneRouteSO targetScene;

        [Foldout("Location")]
        [InfoBox("Khop voi InteractableSO.key. Player tuong tac dung diem nay -> start quest.")]
        public string interactableLocationKey = "SanVanDong";

        [Foldout("UI")]
        [InfoBox("GDD: 'Ban dang tap the duc', 'Ban dang an', 'Di ngu'. " +
                 "Voi quest Study*, field nay khong dung - de trong.")]
        public string confirmText = "Ban dang tap the duc";

        [Foldout("UI")]
        public string okButtonText = "OK";

        [Foldout("Quiz (chi cho Study)")]
        [ShowIfAny("type", QuestType.StudyMorning, "type", QuestType.StudyAfternoon)]
        public SubjectSO subject;

        [Foldout("Quiz (chi cho Study)")]
        [ShowIfAny("type", QuestType.StudyMorning, "type", QuestType.StudyAfternoon)]
        public QuizSetSO quizSet;

        [Foldout("Score impact")]
        [InfoBox("Theo GDD: late hoac miss deu tru 5d ren luyen.")]
        [Slider(0, 20)] public int penaltyOnLate = 5;
        [Foldout("Score impact")]
        [Slider(0, 20)] public int penaltyOnMissed = 5;

        [Foldout("Audio (optional)")]
        public AudioCueId startSound = AudioCueId.Quest_Started;
        [Foldout("Audio (optional)")]
        public AudioCueId completeSound = AudioCueId.Quest_Completed;

        [Button("Validate Time Window")]
        private void ValidateTimes()
        {
            int startTotal = startHour * 60 + startMinute;
            int deadlineTotal = deadlineHour * 60 + deadlineMinute;
            int skipTotal = skipToHour * 60 + skipToMinute;

            if (startTotal > deadlineTotal)
                Debug.LogError($"[{name}] startTime ({startHour:D2}:{startMinute:D2}) > deadline ({deadlineHour:D2}:{deadlineMinute:D2})");

            if (deadlineTotal > skipTotal)
                Debug.LogWarning($"[{name}] skipTo ({skipToHour:D2}:{skipToMinute:D2}) < deadline. Thuong skipTo nen >= deadline.");

            if ((type == QuestType.StudyMorning || type == QuestType.StudyAfternoon) && quizSet == null)
                Debug.LogError($"[{name}] Study quest cua quizSet null - phai assign 1 QuizSetSO.");

            Debug.Log($"[{name}] Validate OK.");
        }
    }
}

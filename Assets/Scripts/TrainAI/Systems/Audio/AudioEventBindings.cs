using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.Systems.Audio
{
    // Bridge GameEvents -> AudioManager.Play. Tach loose coupling: AudioManager khong
    // biet gi ve quest/score; component nay hop nhat.
    // Gan vao GameBootstrap GO.
    public class AudioEventBindings : MonoBehaviour
    {
        private void OnEnable()
        {
            GameEvents.QuestStarted += OnQuestStarted;
            GameEvents.QuestCompleted += OnQuestCompleted;
            GameEvents.QuestLate += OnQuestLate;
            GameEvents.QuestMissed += OnQuestMissed;
            GameEvents.DisciplineChanged += OnDiscipline;
            GameEvents.AcademicChanged += OnAcademic;
            GameEvents.KickedOut += OnKickedOut;
            GameEvents.NewDay += OnNewDay;
            GameEvents.Saved += OnSaved;
        }

        private void OnDisable()
        {
            GameEvents.QuestStarted -= OnQuestStarted;
            GameEvents.QuestCompleted -= OnQuestCompleted;
            GameEvents.QuestLate -= OnQuestLate;
            GameEvents.QuestMissed -= OnQuestMissed;
            GameEvents.DisciplineChanged -= OnDiscipline;
            GameEvents.AcademicChanged -= OnAcademic;
            GameEvents.KickedOut -= OnKickedOut;
            GameEvents.NewDay -= OnNewDay;
            GameEvents.Saved -= OnSaved;
        }

        private static void Play(AudioCueId id)
        {
            var am = AudioManager.Instance;
            if (am != null) am.Play(id);
        }

        private void OnQuestStarted(QuestDefSO q)
        {
            if (q == null) return;
            Play(q.startSound != AudioCueId.None ? q.startSound : AudioCueId.Quest_Started);
        }

        private void OnQuestCompleted(QuestDefSO q, int score)
        {
            if (q == null) return;
            Play(q.completeSound != AudioCueId.None ? q.completeSound : AudioCueId.Quest_Completed);
        }

        private void OnQuestLate(QuestDefSO q) => Play(AudioCueId.Quest_Late);
        private void OnQuestMissed(QuestDefSO q) => Play(AudioCueId.Quest_Missed);

        private void OnDiscipline(int newVal, int delta)
        {
            if (delta < 0) Play(AudioCueId.Score_DisciplinePenalty);
        }

        private void OnAcademic(int newVal, int delta)
        {
            if (delta > 0) Play(AudioCueId.Score_AcademicGain);
        }

        private void OnKickedOut() => Play(AudioCueId.Score_KickedOut);
        private void OnNewDay(int day) => Play(AudioCueId.Day_Start);
        private void OnSaved() => Play(AudioCueId.UI_ToastSuccess);
    }
}

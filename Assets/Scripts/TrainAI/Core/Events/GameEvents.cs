using System;
using TrainAI.Configs;
using TrainAI.Core.Time;

namespace TrainAI.Core.Events
{
    // Static event hub cho cross-system. Tach loose coupling.
    // Subscribe trong OnEnable, unsubscribe trong OnDisable de tranh leak.
    public static class GameEvents
    {
        // --- Quest ---
        public static event Action<QuestDefSO> QuestStarted;
        public static event Action<QuestDefSO, int /*scoreEarned*/> QuestCompleted;
        public static event Action<QuestDefSO> QuestLate;
        public static event Action<QuestDefSO> QuestMissed;
        public static event Action<QuestDefSO /*newCurrent*/> QuestChanged;

        // --- Score ---
        public static event Action<int /*newDiscipline*/, int /*delta*/> DisciplineChanged;
        public static event Action<int /*newAcademic*/, int /*delta*/> AcademicChanged;
        public static event Action KickedOut;

        // --- Time ---
        public static event Action<GameTime> TimeTick;
        public static event Action<int /*newDay*/> NewDay;

        // --- Save ---
        public static event Action Saved;
        public static event Action Loaded;

        // --- Player ---
        public static event Action<string /*name*/> PlayerCreated;

        // Raise helpers - manager goi.
        internal static void RaiseQuestStarted(QuestDefSO q) => QuestStarted?.Invoke(q);
        internal static void RaiseQuestCompleted(QuestDefSO q, int s) => QuestCompleted?.Invoke(q, s);
        internal static void RaiseQuestLate(QuestDefSO q) => QuestLate?.Invoke(q);
        internal static void RaiseQuestMissed(QuestDefSO q) => QuestMissed?.Invoke(q);
        internal static void RaiseQuestChanged(QuestDefSO q) => QuestChanged?.Invoke(q);
        internal static void RaiseDisciplineChanged(int newVal, int delta) => DisciplineChanged?.Invoke(newVal, delta);
        internal static void RaiseAcademicChanged(int newVal, int delta) => AcademicChanged?.Invoke(newVal, delta);
        internal static void RaiseKickedOut() => KickedOut?.Invoke();
        internal static void RaiseTimeTick(GameTime t) => TimeTick?.Invoke(t);
        internal static void RaiseNewDay(int d) => NewDay?.Invoke(d);
        internal static void RaiseSaved() => Saved?.Invoke();
        internal static void RaiseLoaded() => Loaded?.Invoke();
        internal static void RaisePlayerCreated(string name) => PlayerCreated?.Invoke(name);

        public static void ClearAll()
        {
            QuestStarted = null; QuestCompleted = null; QuestLate = null; QuestMissed = null; QuestChanged = null;
            DisciplineChanged = null; AcademicChanged = null; KickedOut = null;
            TimeTick = null; NewDay = null;
            Saved = null; Loaded = null;
            PlayerCreated = null;
        }
    }
}

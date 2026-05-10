using TrainAI.Configs;
using TrainAI.Systems.Quest.Runners;

namespace TrainAI.Systems.Quest
{
    // Default registry factory. GameBootstrap goi InitDefault() de wire 7 runner.
    public static class QuestRunnerProvider
    {
        public static QuestRunnerRegistry Registry { get; private set; }

        public static QuestRunnerRegistry InitDefault()
        {
            var r = new QuestRunnerRegistry();
            r.Register(QuestType.Exercise, new ExerciseRunner());
            r.Register(QuestType.Cleaning, new CleaningRunner());
            r.Register(QuestType.Eat, new EatRunner());
            r.Register(QuestType.FreeRoam, new FreeRoamRunner());
            r.Register(QuestType.StudyMorning, new StudyRunner());
            r.Register(QuestType.StudyAfternoon, new StudyRunner());
            r.Register(QuestType.Sleep, new SleepRunner());
            // Cutscene + Custom: leave unregistered, host se log warning.
            Registry = r;
            return r;
        }
    }
}

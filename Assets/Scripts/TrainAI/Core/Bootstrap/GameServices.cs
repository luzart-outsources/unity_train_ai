using TrainAI.Configs;
using TrainAI.Core.Time;
using TrainAI.Systems.Audio;
using TrainAI.Systems.Dialogue;
using TrainAI.Systems.Interaction;
using TrainAI.Systems.Quest;
using TrainAI.Systems.Quiz;
using TrainAI.Systems.Save;
using TrainAI.Systems.Scene;
using TrainAI.Systems.Score;

namespace TrainAI.Core.Bootstrap
{
    // Service Locator nhe - thay vi Zenject hoac singleton ricaril.
    // Init bang GameBootstrap, access tu bat ki dau qua property.
    // De testable: tests co the assign mock vao property.
    public static class GameServices
    {
        public static GameDatabaseSO Database { get; set; }
        public static TimeManager Time { get; set; }
        public static ScoreManager Score { get; set; }
        public static QuestManager Quest { get; set; }
        public static QuizManager Quiz { get; set; }
        public static SceneFlowService SceneFlow { get; set; }
        public static InteractionManager Interaction { get; set; }
        public static DialogueManager Dialogue { get; set; }
        public static AudioManager Audio { get; set; }
        public static SaveManager Save { get; set; }
        public static PlayerData Player { get; set; }

        public static void Reset()
        {
            Database = null;
            Time = null;
            Score = null;
            Quest = null;
            Quiz = null;
            SceneFlow = null;
            Interaction = null;
            Dialogue = null;
            Audio = null;
            Save = null;
            Player = null;
        }
    }

    // Player data lightweight - khong lien quan PlayerStats SO cua DATN cu.
    public class PlayerData
    {
        public string Name;
        public PlayerData(string name) { Name = name; }
    }
}

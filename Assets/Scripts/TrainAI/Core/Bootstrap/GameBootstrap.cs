using Cysharp.Threading.Tasks;
using Luzart;
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
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Core.Bootstrap
{
    // Init order:
    //  Awake: register tat ca service vao GameServices, init AudioManager + UIManager.
    //  Start: load Title scene neu chua.
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] private GameDatabaseSO database;

        [Header("First scene")]
        [SerializeField] private string firstSceneName = "Title";
        [SerializeField] private bool loadFirstScene = true;

        [Header("Optional - assign neu da co trong scene _Boot")]
        [SerializeField] private AudioManager audioManagerPrefabRef;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitServices();
        }

        private async void Start()
        {
            if (loadFirstScene && SceneManager.GetActiveScene().name != firstSceneName)
            {
                await SceneManager.LoadSceneAsync(firstSceneName).ToUniTask();
            }
        }

        private void InitServices()
        {
            if (database == null)
            {
                Debug.LogError("[GameBootstrap] database not assigned. Aborting init.");
                return;
            }
            if (!database.IsValid(out var err))
            {
                Debug.LogError($"[GameBootstrap] Database invalid: {err}");
                return;
            }

            GameServices.Reset();

            GameServices.Database = database;
            if (database.audioBank != null) database.audioBank.RebuildLookup();
            if (database.uiText != null) database.uiText.RebuildLookup();

            GameServices.Time = new GameClock(database.timeConfig);
            GameServices.Score = new ScoreManager(database.scoreConfig);
            GameServices.Quest = new QuestManager(database.dayCycle, GameServices.Time);
            GameServices.Quiz = new QuizManager();
            GameServices.SceneFlow = new SceneFlowService();
            GameServices.Interaction = new InteractionManager();
            GameServices.Dialogue = new ChatDirector();
            GameServices.Save = new SaveStore();
            GameServices.Player = new PlayerData(database.playerNameDefault);

            // AudioManager singleton.
            var am = AudioManager.Instance != null ? AudioManager.Instance : AudioManager.EnsureInstance();
            am.Bank = database.audioBank;
            GameServices.Audio = am;

            // Quest runner registry.
            QuestRunnerProvider.InitDefault();

            // Wire GameClock events to GameEvents (re-broadcast).
            GameServices.Time.OnTick += t => Core.Events.GameEvents.RaiseTimeTick(t);
            GameServices.Time.OnNewDay += d => Core.Events.GameEvents.RaiseNewDay(d);

            // Start day 1.
            GameServices.Quest.StartDay(database.timeConfig.firstDay);

            // Add helper components vao chinh GO bootstrap.
            if (gameObject.GetComponent<TimeTickDriver>() == null)
                gameObject.AddComponent<TimeTickDriver>();
            if (gameObject.GetComponent<QuestRunnerHost>() == null)
                gameObject.AddComponent<QuestRunnerHost>();
            if (gameObject.GetComponent<AudioEventBindings>() == null)
                gameObject.AddComponent<AudioEventBindings>();
            if (gameObject.GetComponent<KickedOutHandler>() == null)
                gameObject.AddComponent<KickedOutHandler>();
        }
    }
}

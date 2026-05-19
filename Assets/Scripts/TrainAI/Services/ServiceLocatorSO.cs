using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    [CreateAssetMenu(fileName = "ServiceLocator", menuName = "TrainAI/Service Locator")]
    public class ServiceLocatorSO : ScriptableObject
    {
        [Header("Config")]
        public GameConfigSO gameConfig;
        public TimeConfigSO timeConfig;

        [Header("Runtime State")]
        public PlayerStateRSO playerState;
        public GameClockRSO clock;
        public ActiveQuestRSO activeQuest;
        public DayProgressRSO dayProgress;

        [Header("Databases")]
        public QuestDB questDB;
        public NPCDB npcDB;
        public InteractableDB interactDB;
        public DayDB dayDB;
        public QuizDB quizDB;
        public AreaDB areaDB;
        public SubjectDB subjectDB;

        [Header("Models + Responses")]
        public Unity.InferenceEngine.ModelAsset intentModel;
        public Unity.InferenceEngine.ModelAsset soldierModel;
        public TextAsset intentMetaJson;
        public TextAsset responsesJson;
        public ResponseTemplatesSO responseTemplates;

        [Header("Rules")]
        public List<ScoreRuleSO> scoreRules = new();
        public EndingRuleSO endingRule;

        public IGameClock Clock { get; private set; }
        public IQuestRouter Quests { get; private set; }
        public ISceneRouter Scenes { get; private set; }
        public IScoreSystem Score { get; private set; }
        public IInteractionRouter Interactions { get; private set; }
        public INPCDirector NPCs { get; private set; }
        public IDialogueService Dialogue { get; private set; }
        public IMovementService Movement { get; private set; }
        public IUIRouter UI { get; private set; }
        public ISaveService Save { get; private set; }
        public ISentisRuntime Sentis { get; private set; }

        public bool IsBootstrapped { get; private set; }
        UIRouterFacade _uiFacade;

        public void Bootstrap()
        {
            if (IsBootstrapped) return;

            BackendKind backend = gameConfig != null ? gameConfig.preferredBackend : BackendKind.GPUCompute;
            if (intentModel != null || soldierModel != null)
                Sentis = new TrainAI.Sentis.SentisRuntime(intentModel, soldierModel, intentMetaJson, responsesJson, backend);
            else
                Sentis = new SentisRuntimeStub();

            _uiFacade = new UIRouterFacade { Inner = new UIRouter() };
            UI = _uiFacade;
            Scenes = new SceneRouter(UI);

            Score = new ScoreSystem(playerState, gameConfig, scoreRules);
            Clock = new GameClockService(timeConfig, clock, gameConfig);
            // QuestRouter takes IGameClock so it can SkipTo the next quest's
            // start after one completes — closes the dead time gap that made
            // quest chains feel broken in real gameplay (the simulator was
            // hiding it by manually SkipTo-ing per quest).
            Quests = new QuestRouter(dayDB, activeQuest, clock, dayProgress, Score, Clock);

            Movement = new MovementService(Sentis);
            Dialogue = new DialogueService(Quests, playerState, responseTemplates, Sentis, areaDB);
            NPCs = new NPCDirector(npcDB, Movement, clock);

            Interactions = new InteractionRouter(Quests, UI, Scenes, Clock, Score);
            Save = new SaveService(playerState, clock, dayProgress);

            IsBootstrapped = true;
            Debug.Log("[ServiceLocator] Bootstrap done. 11 services online.");
        }

        public void Shutdown()
        {
            if (!IsBootstrapped) return;
            // Dispose IDisposable services so they unhook BroadcastService
            // subscriptions. Otherwise the static BroadcastService keeps the
            // old handlers alive across play-mode restarts, doubling event
            // dispatch each session — a memory + correctness leak.
            (Quests as System.IDisposable)?.Dispose();
            (NPCs as System.IDisposable)?.Dispose();
            (Movement as System.IDisposable)?.Dispose();
            Sentis?.Dispose();
            // Also clear the broadcaster outright as a belt-and-suspenders
            // measure for stray subscribers in UI / presentation layers.
            BroadcastService.Clear();
            IsBootstrapped = false;
        }

        public void OverrideUI(IUIRouter ui)
        {
            if (ui == null) return;
            if (_uiFacade != null) _uiFacade.Inner = ui;
            else UI = ui;
        }
    }
}

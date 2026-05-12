---
title: TrainAI — GDD & Technical Design (Full 30-day SO-First)
category: spec
created: 2026-05-12
updated: 2026-05-12
status: approved-pending-user-review
sources:
  - .wiki/raw/gdd/gdd_inGame.txt
  - .wiki/raw/gdd/context_v2.md
  - AI_Training/deliverables/MERGED_REPORT.md
  - AI_Training/deliverables/intent_classifier_meta.json
  - AI_Training/deliverables/soldier.meta.json
  - AI_Training/deliverables/responses.json
---

# TrainAI — Master Design Spec

Game mô phỏng học kỳ quân sự (đề tài ĐATN Nguyễn Mạnh Quyền). Full 30 ngày, **Unity 6**, SO-First Modular, AI ONNX qua Unity AI Inference Engine (`com.unity.ai.inference` 2.6.1, namespace `Unity.InferenceEngine`, formerly Sentis) cho NPC chat (intent classifier LSTM) và NPC pathfinding (PPO soldier policy). Logic config-by-SO triệt để: thêm quest/NPC/môn học/ending = tạo asset, không sửa core.

---

## 0. Architectural decisions (locked)

| # | Quyết định | Lý do |
|---|---|---|
| D-01 | SO-First Modular + ServiceLocator-via-SO | User chốt mức "config cực kỳ sâu, SOLID, mở rộng nhanh nhất". |
| D-02 | BroadcastService (typed static pub/sub, struct msg) | Tránh tạo asset cho mỗi event. Type-safe, zero alloc. |
| D-03 | Strategy SO swap NPC AI (`OnnxMovementSO` ↔ `NavMeshMovementSO` ↔ fake) | User: "đã có ONNX, có option fake thì NavMesh cũng được". |
| D-04 | uGUI (không UI Toolkit) | User chỉ định. |
| D-05 | 3D third-person camera + arrow dưới chân player | User chỉ định. |
| D-06 | KHÔNG dùng `SoCollection` của NullTale; database SO chứa `List<T>` thuần | User chỉ định. |
| D-07 | Cube/capsule placeholder, prefab override thủ công, KHÔNG registry chung | User chỉ định. |
| D-08 | Automation tooling đặt **cuối** (Phase 11-12), sau khi system stable | User chỉ định. |
| D-09 | Sentis `Worker` per-model (intent + soldier riêng), backend GPUCompute fallback CPU | Khác I/O shape, không share worker. |
| D-10 | Save = JSON DTO trong `Application.persistentDataPath`, KHÔNG ghi vào SO asset | Tránh corrupt asset lúc Play mode. |
| D-11 | `CharacterController` cho player + NPC dùng OnnxMovement (kinematic) | Sim-to-real: soldier.onnx train trên agent kinematic, dùng Rigidbody sẽ lệch. |

---

## 1. Game design (GDD-side)

### 1.1 Scope
- **Full 30 ngày**, skip CN + T7 → đếm tự động sang T2.
- Điểm học tập 0/480 (đậu ≥288), rèn luyện 100/100 ban đầu (đậu ≥60).
- Cuối ngày 30 + xong quest cuối → `UIEnding`.

### 1.2 Day cycle template
```
05:00 Tập thể dục (deadline 05:15)     GotoConfirmQuest
06:00 Dọn vệ sinh                      GotoConfirmQuest
07:00 Ăn sáng                          SceneTransitionQuest → 12_NhaAn
07:30 Học sáng (môn xoay tuần)         QuizQuest → 11_LopHoc
11:30 Nghỉ trưa                        FreeRoam (không quest)
14:00 Học chiều                        QuizQuest
17:00 Tự do                            FreeRoam
18:30 Ngủ                              SleepQuest → NextDay
```
1h game = 3p thực. Đứng yên >15p game (không tới quest area) → −5 rèn luyện. Vào sub-scene quest = đóng băng đồng hồ.

### 1.3 Scoring (`ScoreRuleSO` concrete)
- `LatePenaltyRuleSO`: miss/late → −5 rèn luyện
- `QuizScoreRuleSO`: %đúng × cap môn → +học tập, làm tròn
- `ExpulsionRuleSO`: rèn luyện ≤ 0 → `UIConfirm "Bạn bị đuổi học"` → MainMenu
- `MilitaryAcademyEndingSO` (EndingRuleSO):
  - Xuất sắc: ≥432/480 học tập **và** ≥90/100 rèn luyện
  - Tốt: 288–431 **và** 60–90
  - Trung bình: dưới

### 1.4 NPC matrix
| NPC | Loại | Movement Strategy | Dialogue Strategy |
|---|---|---|---|
| Đại đội trưởng | tĩnh | `IdleMovementSO` | `OnnxDialogueSO` |
| Học sinh schedule | di động | `OnnxMovementSO` / `NavMeshMovementSO` | `CannedDialogueSO` (hoặc none) |
| NPC trang trí | tĩnh | `IdleMovementSO` | none |

### 1.5 Quest types (out-of-the-box)
- `GotoConfirmQuestSO`: tới area → E → `UIConfirm` → skip time → +/− điểm
- `QuizQuestSO`: tới area → load sub-scene → 10 câu × 15s → %đúng → +điểm → unload
- `SceneTransitionQuestSO`: tới cửa → load sub-scene → confirm → unload
- `SleepQuestSO`: tới giường → confirm → `UILoading` → NextDay event
- `FreeRoamQuestSO`: chỉ là mốc thời gian, không yêu cầu hành động

Thêm `RhythmQuestSO`, `ArcheryQuestSO`, v.v. = kế thừa `QuestSO`, override `CreateRuntime()`, **core không sửa**.

---

## 2. Architecture overview

### 2.1 Layered dependency (chiều xuống)

```
PRESENTATION (MonoBehaviour, UI, Animator)
   PlayerController, NpcView, JoystickUI, UIRouter,
   QuestArrowHUD, ClockHUD, ScoreHUD, MinimapHUD,
   BroadcastListener<T>
        ▼ subscribes to / invokes
SERVICE (instantiated by ServiceLocatorSO.Bootstrap())
   IGameClock, IQuestRouter, ISceneRouter, IScoreSystem,
   IInteractionRouter, INPCDirector, IDialogueService,
   IMovementService, IUIRouter, ISaveService, ISentisRuntime
        ▼ runs / reads
STRATEGY SO (polymorphic logic — extension point)
   QuestSO ⟶ {GotoConfirm | Quiz | SceneTransition | Sleep | FreeRoam}
   InteractionSO ⟶ {OpenConfirm | OpenQuiz | OpenDialogue |
                    SceneTransition | SkipTime | Custom}
   MovementStrategySO ⟶ {Onnx | NavMesh | Waypoint | Idle}
   DialogueStrategySO ⟶ {Onnx | Canned | Mock}
   ScoreRuleSO, EndingRuleSO, TimeRuleSO
        ▼ reads
DATA SO (pure config, Assets/_Data/)
   DaySO[30], QuestDB, NPCDB, InteractableDB, QuizDB, AreaDB,
   ResponseTemplatesSO, TimeConfigSO, GameConfigSO, SceneRefSO,
   QuizSetSO, QuizQuestionSO, SubjectSO, ScheduleSO
        ▼ ONNX referenced as ModelAsset
RUNTIME STATE SO (mutable, reset OnEnable, NOT persisted to asset)
   PlayerStateRSO, GameClockRSO, ActiveQuestRSO, DayProgressRSO
        ▲ events flow up via BroadcastService<TMsg>
```

### 2.2 Folder layout

```
Assets/
├── _Data/
│   ├── Days/ Quests/ NPCs/ Interactables/ Quizzes/
│   ├── Strategies/ Areas/ Scenes/ Rules/ Config/ Runtime/
├── _Models/                # intent_classifier.onnx, soldier.onnx, responses.json
├── Scripts/
│   ├── Core/               # BroadcastService, ServiceLocator, RSO base
│   ├── SO/Base/            # Quest/Interaction/Movement/Dialogue/Score base
│   ├── SO/Concrete/        # All concrete strategies
│   ├── Services/
│   ├── Presentation/
│   ├── UI/
│   ├── Sentis/
│   └── Editor/             # Automation tools
└── Scenes/
    ├── 00_Bootstrap.unity 01_MainMenu.unity 02_CutScene.unity 03_CreateChar.unity
    ├── 10_World.unity 11_LopHoc.unity 12_NhaAn.unity 13_KyTucXa.unity
    └── 99_Ending.unity
```

### 2.3 Asmdef structure (1-chiều, no circular)
```
TrainAI.Core           → (no game refs)
TrainAI.SO.Base        → Core
TrainAI.Sentis         → Core, Unity.AI.Inference
TrainAI.SO.Concrete    → SO.Base, Sentis
TrainAI.Services       → Core, SO.Base, SO.Concrete
TrainAI.Presentation   → Services, SO.Base
TrainAI.UI             → Services, SO.Base
TrainAI.Editor         → all (Editor-only define constraint)
```

### 2.4 Scene lifecycle
- `00_Bootstrap` load 1 lần ở app start, không bao giờ unload (giữ ServiceLocator + Sentis).
- World additive load lên Bootstrap; sub-scene (Lớp/KTX/Ăn) additive lên World, World không unload → giữ NPC state.
- `UILoading` 3s che mọi transition.

---

## 3. SO taxonomy

### 3.1 Quy ước suffix

| Loại | Suffix | Mutability | Persist |
|---|---|---|---|
| Data config | `SO` | bất biến lúc Play | asset |
| Strategy (polymorphic) | `SO` (kế thừa abstract) | bất biến | asset |
| Runtime state | `RSO` | mutable, reset OnEnable | KHÔNG persist asset, persist qua SaveService JSON |
| Database | `DB` | bất biến (chứa list ref) | asset |

### 3.2 Base classes

```csharp
public abstract class QuestSO : ScriptableObject {
    public string id;
    public string title;
    public AreaSO area;
    public TimeRange window;       // start, deadline (in-game hour/minute)
    public int latePenalty = 5;
    public abstract IQuestRuntime CreateRuntime(QuestContext ctx);
}

public abstract class InteractionSO : ScriptableObject {
    public string promptText;
    public Sprite icon;
    public abstract UniTask Execute(InteractionContext ctx);
}

public abstract class MovementStrategySO : ScriptableObject {
    public abstract IMovementAgent Bind(Transform npc);
}

public abstract class DialogueStrategySO : ScriptableObject {
    public abstract UniTask<string> Reply(string userInput, NpcContext ctx);
}

public abstract class ScoreRuleSO : ScriptableObject {
    public abstract void Apply(ScoreContext ctx, PlayerStateRSO state);
}

public abstract class EndingRuleSO : ScriptableObject {
    public abstract EndingGrade Evaluate(PlayerStateRSO state);
}
```

### 3.3 Concrete catalog (out-of-the-box)

| Base | Concrete | Behavior |
|---|---|---|
| `QuestSO` | `GotoConfirmQuestSO`, `QuizQuestSO`, `SceneTransitionQuestSO`, `SleepQuestSO`, `FreeRoamQuestSO` | (xem §1.5) |
| `InteractionSO` | `OpenConfirmInteractionSO`, `OpenQuizInteractionSO`, `OpenDialogueInteractionSO`, `SceneTransitionInteractionSO`, `SkipTimeInteractionSO` | |
| `MovementStrategySO` | `OnnxMovementSO`, `NavMeshMovementSO`, `WaypointMovementSO`, `IdleMovementSO` | |
| `DialogueStrategySO` | `OnnxDialogueSO`, `CannedDialogueSO`, `MockDialogueSO` | |
| `ScoreRuleSO` | `LatePenaltyRuleSO`, `QuizScoreRuleSO`, `ExpulsionRuleSO` | |
| `EndingRuleSO` | `MilitaryAcademyEndingSO` | |

### 3.4 Data SO

| SO | Field chính |
|---|---|
| `DaySO` | `int dayIndex`, `Weekday weekday`, `List<QuestSO> quests` |
| `NPCSO` | `id, displayName, portrait, MovementStrategySO movement, DialogueStrategySO dialogue, ScheduleSO schedule, Vector3 spawnPos` |
| `ScheduleSO` | `List<ScheduleEntry>` (each: `int hour, int minute, AreaSO target`) |
| `AreaSO` | `id, Vector3 worldPos, Vector3 size, SceneRefSO scene, SceneRefSO subScene` |
| `InteractableSO` | `AreaSO area, InteractionSO onInteract, Condition gate` |
| `QuizSetSO` | `SubjectSO subject, List<QuizQuestionSO> questions, float perQuestionSec=15` |
| `QuizQuestionSO` | `string q, string[4] answers, int correctIndex` |
| `SubjectSO` | `id, displayName, int maxPointsPerLesson` |
| `ResponseTemplatesSO` | `TextAsset responsesJson` (loaded as dict at runtime) |
| `TimeConfigSO` | `float gameHourPerRealMinute = 1/3f, float idlePenaltyMinutes = 15` |
| `GameConfigSO` | `int totalDays=30, bool skipWeekend=true, int startRenLuyen=100, int maxHocTap=480, BackendType preferredBackend` |
| `SceneRefSO` | `string sceneName, LoadMode mode (Single/Additive)` |

### 3.5 Database SO (`List<T>` thuần, no SoCollection)

```csharp
[CreateAssetMenu(menuName="Game/DB/Quest")]
public class QuestDB : ScriptableObject {
    public List<QuestSO> all;
    public QuestSO ById(string id) => all.FirstOrDefault(q => q.id == id);
#if UNITY_EDITOR
    [ContextMenu("Auto-populate from /_Data/Quests/")]
    void AutoPopulate() { /* AssetDatabase.FindAssets("t:QuestSO") in path */ }
#endif
}
```

Tương tự: `NPCDB`, `InteractableDB`, `QuizDB`, `AreaDB`, `DayDB`, `SubjectDB`. Có Editor context menu "Auto-populate" cho designer khỏi drag thủ công.

### 3.6 Runtime State SO

```csharp
public abstract class RuntimeSO : ScriptableObject {
    void OnEnable() => Reset();
    public abstract void Reset();
}

PlayerStateRSO   : playerName, hocTap, renLuyen, currentSceneName, lastWorldPos
GameClockRSO     : day, hour, minute, weekday, frozen
ActiveQuestRSO   : current (QuestSO), runtimeInstance, deadlineRemainingSec
DayProgressRSO   : completedToday (List<string>), missedToday (List<string>)
```

---

## 4. Service layer + BroadcastService

### 4.1 BroadcastService API

```csharp
public static class BroadcastService {
    static readonly Dictionary<Type, Delegate> _handlers = new();
    public static void Subscribe<T>(Action<T> h) where T : struct;
    public static void Unsubscribe<T>(Action<T> h) where T : struct;
    public static void Send<T>(T msg) where T : struct;
    public static void Clear();        // called on MainMenu load
}
```

Quy ước: message là `struct` (zero alloc), tên `XxxMsg`. Listener subscribe trong `OnEnable`, unsubscribe trong `OnDisable`.

### 4.2 Message catalog

```
DayStartedMsg{day, weekday}      DayEndedMsg{day}
TimeTickMsg{hour, minute}
QuestActivatedMsg{quest}         QuestCompletedMsg{quest, success, scoreDelta}
QuestMissedMsg{quest}
ScoreChangedMsg{hocTap, renLuyen}
InteractZoneEnteredMsg{target}   InteractZoneExitedMsg{target}
InteractPressedMsg{target}
SceneTransitionRequestedMsg{scene}
QuizStartedMsg{set}              QuizEndedMsg{result}
DialogueRequestMsg{npc, input}   DialogueRepliedMsg{npc, text}
ExpelTriggeredMsg{}              GameEndedMsg{grade}
```

### 4.3 ServiceLocatorSO

```csharp
[CreateAssetMenu(menuName="Game/Service Locator")]
public class ServiceLocatorSO : ScriptableObject {
    // ---- Data refs (drag in Inspector) ----
    public GameConfigSO gameConfig; public TimeConfigSO timeConfig;
    public PlayerStateRSO playerState; public GameClockRSO clock;
    public ActiveQuestRSO activeQuest; public DayProgressRSO dayProgress;
    public QuestDB questDB; public NPCDB npcDB; public InteractableDB interactDB;
    public DayDB dayDB; public QuizDB quizDB; public AreaDB areaDB;
    public ModelAsset intentModel; public ModelAsset soldierModel;
    public TextAsset responseTemplates;
    public List<ScoreRuleSO> scoreRules; public EndingRuleSO endingRule;

    // ---- Service instances (Bootstrap()) ----
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

    public void Bootstrap() {
        Sentis = new SentisRuntime(intentModel, soldierModel, gameConfig.preferredBackend);
        Clock = new GameClockService(timeConfig, clock);
        Score = new ScoreSystem(scoreRules, playerState);
        Quests = new QuestRouter(questDB, dayDB, activeQuest, clock, dayProgress);
        Scenes = new SceneRouter();
        Movement = new MovementService(Sentis);
        Dialogue = new DialogueService(Sentis, responseTemplates);
        NPCs = new NPCDirector(npcDB, Movement, Clock);
        Interactions = new InteractionRouter(Quests, UI);
        UI = new UIRouter();
        Save = new SaveService(playerState, clock, dayProgress);
    }
}
```

`BootstrapScene` có 1 MonoBehaviour `BootstrapEntry` với `[SerializeField] ServiceLocatorSO services;` → gọi `services.Bootstrap()` trong `Awake`.

### 4.4 Service interfaces (highlights)

```csharp
public interface IGameClock {
    void Tick(float realDeltaSec);
    void Freeze(); void Resume();
    void SkipTo(int hour, int minute);
    int Day { get; } int Hour { get; } int Minute { get; }
}
public interface IQuestRouter {
    QuestSO Current { get; }
    void StartDay(int day);
    void Complete(QuestSO q, bool success);
    bool IsInteractableAllowed(InteractableSO i);
    string GetTodaySummary();              // for dialogue template filling
}
public interface ISceneRouter {
    UniTask LoadAdditive(SceneRefSO scene, string transitionText = null);
    UniTask UnloadAdditive(SceneRefSO scene);
    UniTask LoadSingle(SceneRefSO scene);
}
public interface ISentisRuntime : IDisposable {
    Worker IntentWorker { get; }
    Worker SoldierWorker { get; }
}
public interface IDialogueService {
    UniTask<string> Reply(NPCSO npc, string userInput);
}
public interface IMovementService {
    void RegisterAgent(Transform npc, MovementStrategySO strategy);
    void SetTarget(Transform npc, Vector3 target);
    void Unregister(Transform npc);
    void Tick(float dt);                   // batch inference 5Hz
}
public interface IUIRouter {
    UniTask<bool> ShowConfirm(string text);
    UniTask<QuizResult> ShowQuiz(QuizSetSO set);
    UniTask ShowLoading(string text, float seconds = 3f);
    UniTask ShowDialogue(NPCSO npc);
    void ShowEnding(EndingGrade grade);
    void ShowExpel();
}
public interface ISaveService {
    void Save(int slot = 1);
    bool TryLoad(int slot, out SaveDTO dto);
}
```

### 4.5 GameLoopDriver

1 MonoBehaviour ở Bootstrap, `DontDestroyOnLoad`:
```csharp
void Update() {
    var dt = Time.deltaTime;
    if (!clock.Frozen) clock.Tick(dt);
    questRouter.Tick(dt);
    movementService.Tick(dt);   // internally 5Hz throttle
}
```

---

## 5. AI integration (Sentis + Strategy)

### 5.1 SentisRuntime

```csharp
public class SentisRuntime : ISentisRuntime {
    public Worker IntentWorker { get; }
    public Worker SoldierWorker { get; }
    public SentisRuntime(ModelAsset i, ModelAsset s, BackendType backend) {
        IntentWorker  = new Worker(ModelLoader.Load(i), backend);
        SoldierWorker = new Worker(ModelLoader.Load(s), backend);
    }
    public void Dispose() { IntentWorker?.Dispose(); SoldierWorker?.Dispose(); }
}
```

Backend ưu tiên `GPUCompute`, fallback `CPU` qua `GameConfigSO.preferredBackend`.

### 5.2 Dialogue pipeline

**Tokenizer** (`VietnameseTokenizer.cs`, plain C#): nạp `vocab` từ `intent_classifier_meta.json`. Method `int[] Encode(string text, int maxLen = 32)`. Whitespace split + multi-word lookup ưu tiên longest match; OOV → `<unk>=1`; pad → `<pad>=0`.

**IntentClassifier**:
```csharp
public class IntentClassifier {
    public IntentResult Predict(int[] tokenIds) {
        using var input = new Tensor<int>(new TensorShape(1, MaxLen), tokenIds);
        _worker.Schedule(input);
        using var output = (Tensor<float>)_worker.PeekOutput();
        var probs = Softmax(output.DownloadToArray());
        return new IntentResult { intent = ArgMax(probs), score = probs.Max(), top2 = TopK(probs, 2) };
    }
}
```

**ResponseFiller**: load `responses.json` → `Dictionary<IntentId, List<string>>`. Method `string Fill(IntentId, NpcContext)` replace `{scheduled_today}` từ `QuestRouter.GetTodaySummary()`, `{meal_time}` từ TimeConfig, `{place}` từ `AreaDB.NearestTo(player)`, `{topic}` từ heuristic input (fallback `"điều đó"`).

**`OnnxDialogueSO`**:
```csharp
[CreateAssetMenu(menuName="Game/Dialogue/Onnx")]
public class OnnxDialogueSO : DialogueStrategySO {
    [SerializeField, Range(0,1)] float confidenceThreshold = 0.5f;
    [SerializeField] IntentId fallback = IntentId.OUT_OF_SCOPE;

    public override async UniTask<string> Reply(string input, NpcContext ctx) {
        var tokens = ctx.Tokenizer.Encode(input);
        var result = ctx.IntentClassifier.Predict(tokens);
        var intent = result.score < confidenceThreshold ? fallback : result.intent;
        return ctx.ResponseFiller.Fill(intent, ctx);
    }
}
```

### 5.3 Movement pipeline

**Observation builder** (theo `soldier.meta.json`):
```
obs[0..7]   = ray_dist[8]        : Physics.Raycast 8 hướng XZ-plane, max 10m → normalize /10
obs[8..15]  = ray_hit_target[8]  : 1.0 nếu hit có tag "Target", else 0
obs[16]     = vel_forward        : Dot(velocity, forward) / maxSpeed
obs[17]     = vel_lateral        : Dot(velocity, right) / maxSpeed
obs[18]     = dir_to_target_fwd  : Dot(toTarget_normalized, forward)
obs[19]     = dir_to_target_lat  : Dot(toTarget_normalized, right)
obs[20]     = dist_to_target     : clamp(distance, 0, 20) / 20
```

**Output**: `action[0]=thrust ∈ [-1,1]`, `action[1]=turn ∈ [-1,1]`.

**Apply**:
```csharp
transform.Rotate(0, turn * maxTurnRadPerSec * Mathf.Rad2Deg * dt, 0);
var moveVec = transform.forward * thrust * maxSpeed * dt;
characterController.Move(moveVec + gravity * dt);
```

**`OnnxMovementSO`** + `OnnxMovementAgent` (plain C#). MovementService giữ Dictionary<Transform, IMovementAgent>, gọi `agent.Tick(soldierWorker, dt)` ở 5Hz (throttle bằng accumulator).

**Swap fake**: Đổi `NPCSO.movementStrategy` từ `OnnxMovementSO` → `NavMeshMovementSO` (gọi `NavMeshAgent.SetDestination(target)`) hoặc `WaypointMovementSO` (lerp).

### 5.4 NPCDirector

Subscribe `TimeTickMsg`. Mỗi tick, foreach NPC trong `NPCDB`:
```
entry = npc.schedule.GetEntryAt(day, hour, minute);
if (entry.area != npc.runtimeCurrentArea)
    movement.SetTarget(npc.transform, entry.area.worldPos);
```

### 5.5 Performance budget

| Item | Ngân sách | Note |
|---|---|---|
| Intent inference | <30ms | 1 lần/lần player gửi |
| Soldier inference | ~10 NPC × 5Hz, mỗi inference 2-5ms GPU | ~150ms/s total chia 5 tick = 30ms/tick |
| Raycast | 10 NPC × 8 ray × 5Hz = 400 ray/s | OK |
| Frame budget | <2ms/frame trung bình | |

NPC > 30 → switch fallback NavMesh hoặc chunked inference.

---

## 6. Scene flow + UI router

### 6.1 Scene FSM
```
00_Bootstrap (persistent)
  → additive 01_MainMenu
      → NewGame  : unload MainMenu → 02_CutScene → 03_CreateChar → additive 10_World
      → Continue : SaveService.Load() → restore RSO → load saved scene
      → Exit     : Application.Quit
10_World (active)
  → enter zone → additive 11_LopHoc | 12_NhaAn | 13_KyTucXa
  → on quest complete → unload sub-scene, back to World
Day 30 + last quest → unload World → 99_Ending
```

### 6.2 UIRouter

`OverlayCanvas` ở Bootstrap scene (`DontDestroyOnLoad`). UIRouter quản lý 1 Canvas chứa mọi popup prefab. Instantiate khi cần, destroy khi đóng (không pre-spawn).

Popup catalog: `UIMainMenu, UICreateChar, UILoading, UIConfirm, UIQuiz, UIDialogue, UIInteractPrompt, UIQuestHUD, UIClockHUD, UIScoreHUD, UIMiniMap, UIJoystick, UIEnding, UIExpel`.

### 6.3 Input
Unity Input System asset: `Move (Vector2)`, `Interact (Button)`, `Look (Vector2)`, `MenuToggle (Button)`. PlayerController bind action callback, không dùng `Input.GetKey`.

### 6.4 Interactable detection
```
PlayerInteractor (trigger collider radius 2m trên Player):
  OnTriggerEnter → InteractZoneEnteredMsg
  OnTriggerExit  → InteractZoneExitedMsg
  Update: if facing (dot(forward, toZone) > 0.5) → enable UIInteractPrompt button
  OnInteract input → InteractPressedMsg{currentZone}

InteractionRouter (subscribes InteractPressedMsg):
  if (!questRouter.IsInteractableAllowed(msg.target)) → toast "Chưa tới giờ làm việc này"
  else msg.target.onInteract.Execute(ctx)
```

### 6.5 Day rollover
```
SleepQuestSO.Complete():
  await UIRouter.ShowLoading("Sang ngày hôm sau...", 3f)
  Broadcast DayEndedMsg
  GameClock.SetTime(firstQuestHourTomorrow - 30 minutes)
  GameClock.IncrementDay (skip Sat+Sun if skipWeekend)
  Broadcast DayStartedMsg
  QuestRouter.StartDay(newDay)
  // NPCDirector tự re-compute targets
```

Day 30 cuối quest → `Broadcast GameEndedMsg{grade}` → `UIRouter.ShowEnding(grade)`.

---

## 7. Data flow traces

### 7.1 Morning exercise (05:00 tập thể dục)
```
Clock.Tick → hour=5,min=0 → TimeTickMsg
QuestRouter scans Day[N].quests where window.start==5:00 → Quest_TheDuc
  → ActiveQuestRSO = Quest_TheDuc
  → QuestActivatedMsg
QuestArrowHUD: spawn arrow under player, point to Quest_TheDuc.area.worldPos
QuestHUD: "Tập thể dục (05:00 — 05:15)"

Player walks into Area_SanVanDong → OnTriggerEnter
  → InteractZoneEnteredMsg → UIInteractPrompt enable

Player presses E → InteractPressedMsg
InteractionRouter: gate check OK → OpenConfirmInteractionSO.Execute
  → UIConfirm "Bạn đang tập thể dục" → OK
  → SkipTimeInteractionSO.Execute → GameClock.SkipTo(5,30)
  → ActiveQuest.Complete(true)
  → QuestCompletedMsg

(if Player NOT at area by 05:15)
QuestRouter.Tick → deadline passed → ActiveQuest.Complete(false)
  → LatePenaltyRuleSO.Apply → -5 renLuyen → ScoreChangedMsg
  → QuestMissedMsg → next quest activates
```

### 7.2 Chat NPC Đại đội trưởng
```
Player interacts NPC → OpenDialogueInteractionSO → UIRouter.ShowDialogue(NPC)
UIDialogue header: "Chào [PlayerName], có chuyện gì vậy?"
Player types "Hôm nay có lịch gì" → Send → DialogueRequestMsg
DialogueService.Reply:
  tokens = Tokenizer.Encode("Hôm nay có lịch gì")
  intent = IntentClassifier.Predict(tokens) → HOI_LICH (0.93)
  template = "Hôm nay đại đội ta có {scheduled_today}. Đồng chí chú ý nhé."
  scheduled_today = QuestRouter.GetTodaySummary()
  filled = "Hôm nay đại đội ta có tập thể dục, dọn vệ sinh, học Lịch sử... Đồng chí chú ý nhé."
→ UIDialogue.Append("[Đại đội trưởng]: " + filled)
```

### 7.3 NPC học sinh tự đi theo schedule
```
06:00 TimeTickMsg → NPCDirector loops NPCDB
  for each npc: entry = npc.schedule.GetEntryAt(day=3, h=6, m=0)
    if entry.area != npc.currentArea:
       MovementService.SetTarget(npc.transform, entry.area.worldPos)

MovementService.Tick (5Hz):
  foreach (transform, agent): agent.Tick(SoldierWorker, dt)

OnnxMovementAgent.Tick:
  obs = BuildObs(21D)
  using inT = new Tensor<float>((1,21), obs)
  worker.Schedule(inT)
  using outT = (Tensor<float>) worker.PeekOutput()
  thrust = clamp(outT[0],-1,1); turn = clamp(outT[1],-1,1)
  Apply rotate + characterController.Move
```

---

## 8. Save/Load

### 8.1 Format (JSON DTO)

```json
{
  "version": 1,
  "savedAt": "2026-05-12T15:30:00",
  "playerName": "Quyen",
  "day": 7,
  "weekday": "Monday",
  "hour": 14,
  "minute": 30,
  "hocTap": 142,
  "renLuyen": 85,
  "currentScene": "10_World",
  "playerPos": {"x": 12.4, "y": 0, "z": 8.1},
  "completedQuestIds": ["Q_TheDuc_d1","Q_DonVS_d1"],
  "missedQuestIds": ["Q_LichSu_d2"]
}
```

Path: `Application.persistentDataPath/save_slot{N}.json`.

### 8.2 Triggers
- Auto-save on `DayEndedMsg`.
- Manual "Save & Quit" (optional, qua pause menu).

### 8.3 Versioning
`SaveMigrator.Migrate(int from, int to, JObject)` chain. Đổi schema = bump version + viết migration.

---

## 9. Extension recipes

### 9.1 Thêm quest type mới (`ArcheryQuestSO`)
1. Viết `ArcheryQuestSO : QuestSO`, `ArcheryQuestRuntime : IQuestRuntime`.
2. Tạo asset `Quest_BanCung_d5.asset`, drag vào `DayDB.Days[5].quests`.
3. Core: 0 dòng sửa.

### 9.2 Đổi NPC AI thật ↔ fake
1. Mở `NPC_HocSinh01.asset` → field `movementStrategy`: `MovementOnnx` → `MovementNavMesh`.
2. Play.

### 9.3 Thêm intent mới (`HOI_DIEM`)
1. Train lại ONNX với data mở rộng (pipeline có sẵn ở `AI_Training/phase_a_sentis/`).
2. Thêm `"HOI_DIEM": ["Điểm học tập: {hocTap}/480, rèn luyện: {renLuyen}/100"]` vào `responses.json`.
3. Drop ONNX + responses.json mới vào `Assets/_Models/`. Re-import. Cập nhật `ServiceLocatorSO.intentModel` nếu đổi file.
4. Core code: 0 dòng (filler tự pick template mới).

### 9.4 Thêm môn học (Sinh học)
1. Tạo `Subject_SinhHoc.asset`.
2. Tạo 10× `QuizQuestionSO`.
3. Tạo `QuizSet_SinhHoc_1.asset`.
4. Tạo `Quest_HocSinhHoc_d12.asset` (QuizQuestSO) → trỏ vào QuizSet.
5. Drag vào `DayDB.Days[12].quests`.

### 9.5 Đổi công thức điểm
Mở `QuizScoreRule.asset` → đổi field. **Hoặc** viết `WeightedQuizScoreRuleSO`, thay vào `ServiceLocatorSO.scoreRules`.

### 9.6 Thêm ending
Viết `SecretEndingRuleSO`, thay `ServiceLocatorSO.endingRule`.

---

## 10. Quest arrow + 3D camera

### 10.1 Quest arrow
- Prefab `QuestArrow3D.prefab`: cone + cylinder mesh, material emissive (vàng).
- Gắn child Player ở local `(0, 0.05, 0.4)` (5cm trên ground, trước 40cm).
- `QuestArrowHUD` script: subscribe `QuestActivatedMsg`/`QuestCompletedMsg`. Mỗi frame `arrow.LookAt(new Vector3(target.x, transform.position.y, target.z))`. Bobbing y-sin 0.05m, scale pulse 0.95↔1.05 qua AnimationCurve.

### 10.2 Third-person camera
- `CameraRig` empty parent player. `Camera` ở `(0, 1.6, -3.5)` relative rig.
- Follow bằng `Vector3.SmoothDamp` (smoothTime 0.1).
- Look input xoay yaw rig, pitch clamp [-20, 50].
- Player rotate theo camera yaw khi `Move.magnitude > 0.1`.

### 10.3 `ThirdPersonCameraConfigSO`

```csharp
[CreateAssetMenu(menuName="Game/Camera/Third Person")]
public class ThirdPersonCameraConfigSO : ScriptableObject {
    public float distance = 3.5f, height = 1.6f;
    public float yawSpeed = 120f, pitchMin = -20f, pitchMax = 50f;
    public float smoothTime = 0.1f;
    public bool invertY = false;
}
```

---

## 11. Placeholder visuals (cube/capsule)

### 11.1 Scale convention (mét)

| Object | Scale | Note |
|---|---|---|
| Player | `0.6 × 1.8 × 0.6` capsule | |
| NPC | `0.6 × 1.8 × 0.6` capsule | |
| Lớp học | `12 × 5 × 8` cube | |
| KTX | `15 × 4 × 10` cube | |
| Nhà ăn | `14 × 4.5 × 9` cube | |
| Sân vận động | `25 × 0.1 × 15` plane | trigger area |
| Đường | plane `2 × 0.05 × N` | |
| Cửa = interactable | `1.5 × 2.5 × 0.5` cube + trigger radius 2m | |

### 11.2 Color code (debug suggestion, prefab override thủ công)

```
Player          : xanh dương
NPC chỉ huy     : đỏ
NPC học sinh    : xám
Interactable    : vàng (highlight khi player nhìn)
Quest area      : xanh lá glow
Off-limit area  : đen mờ
```

User override material trực tiếp trên prefab khi có 3D model thật. **Không** dùng registry SO chung.

---

## 12. Automation tooling (Editor-only)

### 12.1 Mục tiêu
1 nút `Tools → Build Game → Generate All (Master)` → toàn bộ:
- 30 `DaySO` + ~150-200 `QuestSO` + 30 `QuizSetSO` + 10-15 `NPCSO` + 20 `InteractableSO`
- `ServiceLocatorSO` auto-wired
- 6+ scene set up với cube placeholder + trigger
- Prefab Player/NPC/Arrow built
- ONNX + responses.json auto-assigned
- Validator pass (no null ref, no compile error, asmdef OK)

### 12.2 Tool catalog (`Assets/Editor/Automation/`)

| Tool | Output |
|---|---|
| `SOGenerator.cs` | Generate Days/Quests/Quizzes/NPCs/Interactables từ `world_template.json` |
| `ServiceWizard.cs` | Build + wire `ServiceLocatorSO` |
| `SceneBuilder.cs` | Build 6+ scene với cube/area/spawn |
| `PrefabBuilder.cs` | Build Player/NPC/Arrow/HUD prefab |
| `Validator.cs` | Null ref, asmdef, compile, missing scene in BuildSettings |
| `MasterBuilder.cs` | Run all in order + report |

### 12.3 Menu

```
Tools/
├── Build Game/
│   ├── 1. Generate Data SO
│   ├── 2. Generate Strategy SO
│   ├── 3. Build Service Locator
│   ├── 4. Build Prefabs
│   ├── 5. Build Scenes
│   ├── 6. Validate Everything
│   └── ⚡ Generate All (Master)
└── Debug/
    ├── Reset Save
    ├── Skip to Day X
    └── Force Quest Complete
```

### 12.4 `world_template.json` (single source of truth)

```jsonc
{
  "days": [
    { "day": 1, "weekday": "Mon", "quests": [
        {"type":"GotoConfirm","area":"SanVanDong","start":"05:00","deadline":"05:15","title":"Tập thể dục"},
        {"type":"GotoConfirm","area":"DonVeSinh","start":"06:00","deadline":"06:30","title":"Dọn vệ sinh"},
        {"type":"SceneTransition","area":"NhaAn_Door","start":"07:00","deadline":"07:30","subscene":"12_NhaAn"},
        {"type":"Quiz","area":"LopHoc_Door","start":"07:30","deadline":"11:30","quizSet":"LichSu_d1"},
        {"type":"FreeRoam","start":"11:30","deadline":"14:00"},
        {"type":"Quiz","area":"LopHoc_Door","start":"14:00","deadline":"17:00","quizSet":"GDQP_d1"},
        {"type":"FreeRoam","start":"17:00","deadline":"18:30"},
        {"type":"Sleep","area":"KTX_Door","start":"18:30","deadline":"23:59"}
    ]}
    // ... 30 days
  ],
  "npcs": [
    {"id":"DaiDoiTruong","pos":{"x":0,"y":0,"z":5},"movement":"Idle","dialogue":"Onnx"},
    {"id":"HocSinh_01","pos":{"x":-3,"y":0,"z":2},"movement":"Onnx","schedule":[
      {"time":"05:00","area":"SanVanDong"},{"time":"06:00","area":"DonVeSinh"},
      {"time":"07:30","area":"LopHoc_Door"},{"time":"11:30","area":"FreeArea"}
    ]}
  ],
  "areas": [
    {"id":"SanVanDong","pos":{"x":-10,"y":0,"z":0},"size":{"x":25,"y":0.1,"z":15},"scene":"10_World"},
    {"id":"DonVeSinh","pos":{"x":5,"y":0,"z":10},"size":{"x":8,"y":0.1,"z":6},"scene":"10_World"},
    {"id":"LopHoc_Door","pos":{"x":5,"y":0,"z":-8},"size":{"x":1.5,"y":2.5,"z":0.5},"scene":"10_World","sub":"11_LopHoc"},
    {"id":"KTX_Door","pos":{"x":-5,"y":0,"z":-8},"size":{"x":1.5,"y":2.5,"z":0.5},"scene":"10_World","sub":"13_KyTucXa"},
    {"id":"NhaAn_Door","pos":{"x":10,"y":0,"z":-8},"size":{"x":1.5,"y":2.5,"z":0.5},"scene":"10_World","sub":"12_NhaAn"}
  ],
  "subjects": [
    {"id":"LichSu","name":"Lịch sử","lessons":12,"maxPerLesson":40},
    {"id":"GDQuocPhong","name":"GD Quốc phòng","lessons":12,"maxPerLesson":40}
  ]
}
```

### 12.5 Generator pattern (idempotent)

```csharp
public static class SOGenerator {
    [MenuItem("Tools/Build Game/1. Generate Data SO")]
    public static void GenerateAll() {
        var bp = JsonUtility.FromJson<WorldBlueprint>(
            File.ReadAllText("Assets/Editor/Automation/world_template.json"));
        GenerateAreas(bp.areas);
        GenerateSubjects(bp.subjects);
        GenerateQuizzes(bp.subjects);
        GenerateInteractables(bp.areas);
        GenerateNPCs(bp.npcs);
        GenerateDays(bp.days);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
    }
    static T CreateOrUpdate<T>(string path) where T : ScriptableObject {
        var x = AssetDatabase.LoadAssetAtPath<T>(path);
        if (x == null) { x = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(x, path); }
        return x;
    }
}
```

### 12.6 Auto-run flow (Master)
1. Pre-check: compile clean (no error)
2. `SOGenerator.GenerateAll()`
3. `PrefabBuilder.BuildAll()`
4. `SceneBuilder.BuildAll()`
5. `ServiceWizard.BuildServiceLocator()` (wire refs)
6. Trigger `CompilationPipeline.RequestScriptCompilation`, await `AssemblyReloadEvents.afterAssemblyReload`
7. `Validator.ValidateAll()` (null ref, asmdef, missing scene in BuildSettings, compile errors)
8. Open `00_Bootstrap` scene
9. Write `Library/automation_report.md` with ✅/❌ list

---

## 13. Compile/asmdef safety

### 13.1 Asmdef tree (1 chiều)
```
TrainAI.Core              → (none)
TrainAI.SO.Base           → Core
TrainAI.Sentis            → Core, Unity.AI.Inference, Cysharp.UniTask
TrainAI.SO.Concrete       → SO.Base, Sentis, Unity.AI.Inference
TrainAI.Services          → Core, SO.Base, SO.Concrete, UniTask
TrainAI.Presentation      → Services, SO.Base, InputSystem
TrainAI.UI                → Services, SO.Base, TMP
TrainAI.Editor            → all (defineConstraints: UNITY_EDITOR)
```

### 13.2 Validator checks
- `AssetDatabase.FindAssets("t:ScriptableObject")` → reflect fields → null check (with `[Required]` attribute support)
- `CompilationPipeline.GetAssemblies()` → check for compile errors
- `EditorBuildSettings.scenes` contains all 6+ scene names
- All `SceneRefSO.sceneName` points to existing scene
- All `ModelAsset` ref non-null in ServiceLocator
- `responses.json` parses + has all 8 intent keys
- Output `Library/automation_report.md`

### 13.3 Implementation phase order

```
Phase 0  Project skeleton (asmdef, folders, stubs)
Phase 1  BroadcastService + RSO base + Core utilities
Phase 2  SO base classes (Quest/Interaction/Movement/Dialogue/Score/Ending)
Phase 3  Concrete data SO + minimum-1 concrete Strategy per type
Phase 4  Services (Clock, Quest, Score, Scene, UI, Save) — stub Sentis
Phase 5  Sentis integration (Tokenizer, IntentClassifier, OnnxMovementAgent)
Phase 6  Player + NPC prefab + camera rig + quest arrow
Phase 7  UI (Confirm/Quiz/Dialogue/Loading/HUDs) uGUI
Phase 8  Save/Load (JSON DTO + migrator)
Phase 9  Scene 10_World + sub-scenes (1-day manual prototype)
Phase 10 Vertical slice test (1 day end-to-end, all systems wired)
Phase 11 Automation tooling (Generator, Builder, Validator)
Phase 12 Master Generate All → 30-day full content
Phase 13 Polish, balance, ending screen
```

Automation cuối (Phase 11-12) — chỉ làm sau khi system stable, đúng yêu cầu user.

---

## 14. Open questions

- [ ] `world_template.json` 30 ngày đầy đủ — user soạn hay Claude generate từ GDD bằng prompt?
- [ ] Quiz content (câu hỏi thật) — placeholder dummy hay user soạn thật?
- [ ] Cutscene video file (scene 02) — user cung cấp hay skip?
- [ ] Audio (BGM/SFX) — out of scope spec này, làm sau khi hỏi.
- [ ] Localization — chuẩn bị `LocalizedString` nhưng giờ chỉ Việt; có cần i18n không?

---

## 15. Out of scope (cho spec này)

- Multiplayer/server (đề tài đơn người chơi).
- Cloud save / analytics.
- Mobile-specific optimization (joystick có nhưng touch input chưa test).
- Voice/TTS NPC (chỉ text).
- ML-Agents retraining trong Unity (dùng ONNX có sẵn).

---

## 16. Acceptance criteria

Spec coi như đạt khi implementation tới Phase 12 cho ra:
1. Bấm Play ở `00_Bootstrap` → vào MainMenu → NewGame → CreateChar → World scene.
2. Time tick, quest arrow chỉ đúng nơi, NPC commander chat trả lời intent-based.
3. NPC học sinh tự di chuyển theo schedule dùng `OnnxMovementSO`.
4. Đổi 1 NPC sang `NavMeshMovementSO` qua Inspector → vẫn chạy, không lỗi.
5. Hoàn thành 1 ngày → sleep → sang ngày 2.
6. Sau Phase 12: `Tools → Build Game → Generate All` chạy không lỗi, `automation_report.md` PASS.
7. Tới day 30 → UIEnding với grade.

---

**End of spec.**

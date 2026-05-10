---
title: GDD InGame — Technical Design (SO-driven)
category: technical
tags: [datn, gdd, scriptable-object, architecture, quest, time, quiz, unity6]
sources: [raw/gdd/gdd_inGame.txt]
created: 2026-05-11
updated: 2026-05-11
---

# Technical Design — Game học kỳ quân đội (GDD InGame)

Tài liệu kiến trúc kỹ thuật cho game theo `raw/gdd/gdd_inGame.txt`. **Mọi tham số gameplay đều do `ScriptableObject` quyết định** — Quyền tạo / sửa content trong Unity Editor, không đụng C#. Kế thừa pattern từ template farming cũ ([[technical/datn-architecture]]) nhưng thay god-object Blackboard bằng typed state, đổi `BinaryFormatter` sang JSON, và thiết kế lại quest/time để khớp lifecycle 30 ngày của GDD.

> [!info] Phạm vi (Phần I)
> Doc này là *blueprint* cho Quyền code. Đã chốt: tên class, namespace, thư mục, schema SO, lifecycle. Chưa chốt (ghi `// TODO`): kích thước map, art style, audio. Bám sát doc khi viết code thì save/load + AI Phase A/B sẽ plug-in được.

> [!warning] Đọc kèm Phần II — Tích hợp Luzart framework
> Section "UI state machine", "Boot order — GameBootstrap", "Scene flow + interaction" và một phần "ScriptableObject catalog" trong doc này đã được **REFINE** ở [[technical/gdd-ingame-luzart-integration]] sau khi phát hiện project đã có sẵn:
> - **NinjaUI** (`Assets/Luzart/UIFramework/NinjaUI/`) — UI framework async/lane-based đầy đủ, không cần tự viết
> - **TweenAnimation** (`Assets/Luzart/TweenAnimationPackage/`) — animation inspector-driven
> - **NewBaseSelect** (`Assets/Luzart/NewBaseSelect/`) — toggle/switch component cho UI feedback
> - **Attributes** (`Assets/Luzart/Attributes/`) — `[ShowIf] [InfoBox] [Slider] [Button] [Foldout] [Dropdown]` cho SO Inspector
>
> Quyền nên đọc Phần II SAU Phần I — phần Quest / Time / Score / Quiz / Save vẫn áp dụng nguyên, chỉ phần UI + animation + inspector UX là thay đổi.

---

## Mục lục

1. [Triết lý thiết kế](#triết-lý-thiết-kế)
2. [Cấu trúc thư mục](#cấu-trúc-thư-mục)
3. [Layer kiến trúc tổng quan](#layer-kiến-trúc-tổng-quan)
4. [Boot order — GameBootstrap](#boot-order--gamebootstrap)
5. [ScriptableObject catalog (toàn bộ data class)](#scriptableobject-catalog)
6. [Time system](#time-system)
7. [Quest system + daily lifecycle](#quest-system--daily-lifecycle)
8. [Score system](#score-system)
9. [Quiz system](#quiz-system)
10. [Dialogue + Sentis chat integration](#dialogue--sentis-chat-integration)
11. [NPC AI movement integration](#npc-ai-movement-integration)
12. [UI state machine](#ui-state-machine)
13. [Scene flow + interaction](#scene-flow--interaction)
14. [Save / Load](#save--load)
15. [Workflow: Quyền config content](#workflow-quyền-config-content)
16. [Roadmap implementation](#roadmap-implementation-7-ng-y-demo)
17. [Anti-patterns nên tránh](#anti-patterns-nên-tránh)

---

## Triết lý thiết kế

1. **Data > Code**. Đề bài (lịch ngày, môn học, quest, điểm số, threshold xếp hạng) là *content* — vào ScriptableObject. Code chỉ là *engine* đọc content.
2. **Single source of truth**. Mỗi giá trị tồn tại đúng 1 nơi. Vd `MaxDisciplineScore = 100` chỉ ở `ScoreConfigSO`. Không hardcode trong UI lẫn ScoreManager.
3. **Typed state, không magic string**. Template cũ dùng `Dictionary<string, object>` (GameBlackboard) — debug địa ngục. Doc này xài enum + class POCO.
4. **Manager mỏng, SO dày**. Manager giữ runtime state + orchestration; SO giữ rule + data. Vd `QuestManager` không biết "5h tập thể dục" — nó đọc `QuestDefSO`.
5. **Event-driven cross-system**. Time → broadcast tick. Quest → broadcast started/completed. UI subscribe, không poll.
6. **AI Phase A/B là plugin**. Game vẫn chạy nếu bypass AI. Chat fallback rule-based, NPC fallback waypoint.

---

## Cấu trúc thư mục

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Bootstrap/        GameBootstrap, ServiceLocator
│   │   ├── Time/             TimeManager, GameTime, ITimeTickListener
│   │   ├── Save/             SaveManager, GameSaveData (JSON)
│   │   ├── Events/           GameEvents (static event hub)
│   │   └── Input/            InputReader
│   │
│   ├── Configs/              # SO TYPE definitions (class)
│   │   ├── Time/             TimeConfigSO
│   │   ├── Quest/            QuestDefSO, DayPlanSO, DayCycleConfigSO
│   │   ├── Quiz/             SubjectSO, QuizSetSO, QuestionSO
│   │   ├── Score/            ScoreConfigSO, GradeThresholdSO
│   │   ├── NPC/              NPCProfileSO, NPCScheduleSO
│   │   ├── UI/               UITextSO, UIThemeSO
│   │   ├── Scene/            SceneRouteSO, InteractableSO
│   │   └── Database/         GameDatabaseSO (root catalog)
│   │
│   ├── Systems/
│   │   ├── Quest/            QuestManager, QuestRuntimeState, QuestRunner_*
│   │   ├── Quiz/             QuizManager, QuizRuntime
│   │   ├── Score/            ScoreManager
│   │   ├── Dialogue/         DialogueManager + SentisChatBridge
│   │   ├── NPC/              NPCController, NPCMovementBrain, NPCSchedule
│   │   ├── Interaction/      InteractionManager, InteractableTrigger
│   │   ├── Scene/            SceneFlowManager
│   │   └── AI/               PhaseAChatService, PhaseBMovementService
│   │
│   ├── UI/
│   │   ├── Core/             UIManager, UIScreenBase, UIScreenStack
│   │   ├── Screens/          MainMenuScreen, CharacterCreateScreen, ...
│   │   ├── HUD/              ClockHUD, MiniMapHUD, QuestHUD, ScoreHUD
│   │   └── Components/       InteractButton, JoystickWidget
│   │
│   ├── Player/               PlayerController, PlayerInteractor, PlayerCamera
│   └── Utils/                Singleton<T>, ResourceLoader
│
├── Configs/                  # SO INSTANCE assets (Quyền tạo)
│   ├── _Database.asset       # Root, drag-drop tất cả catalog vào đây
│   ├── Time/                 GameTime.asset
│   ├── Score/                Score.asset, Grades.asset
│   ├── DayPlans/             Day01.asset, Day02.asset, ...
│   ├── Quests/               Q_Exercise_0500.asset, Q_Cleaning_0600.asset, ...
│   ├── Subjects/             S_LichSu.asset, S_QuocPhong.asset, ...
│   ├── QuizSets/             QS_LichSu_Bai01.asset, ...
│   ├── NPCs/                 NPC_DaiDoiTruong.asset, ...
│   ├── Scenes/               R_World.asset, R_Classroom.asset, ...
│   └── UIText/               UI_Vi.asset
│
├── AI/
│   ├── PhaseA_Chat/          intent_classifier_v2.onnx + slot_vocab.json + wrapper
│   └── PhaseB_Movement/      soldier.onnx + soldier.meta.json + wrapper
│
├── Prefabs/
│   ├── Player/
│   ├── NPC/
│   ├── UI/
│   └── Interactables/
│
├── Scenes/
│   ├── _Boot.unity           # entry — load GameBootstrap, then Title
│   ├── Title.unity
│   ├── World.unity
│   ├── Classroom.unity
│   ├── Dormitory.unity
│   └── Cafeteria.unity
│
└── Resources/                # KHÔNG đặt SO ở đây — load qua direct ref
```

> [!tip] Vì sao tách `Configs/` (SO type) và `Configs/` asset folder?
> Code class (`QuestDefSO.cs`) không thay đổi nhiều, instance asset (`Q_Exercise_0500.asset`) thì add liên tục. Tách giúp git diff sạch — chỉ thấy asset thay đổi mỗi lần Quyền edit lịch.

---

## Layer kiến trúc tổng quan

```
                       ┌──────────────────────────┐
                       │   GameDatabaseSO (root)  │  ← Quyền edit
                       │   refs all catalogs      │
                       └────────────┬─────────────┘
                                    │ Inspector
              ┌─────────────────────┼─────────────────────┐
              │                     │                     │
   ┌──────────▼──────┐  ┌───────────▼─────────┐  ┌────────▼────────┐
   │  TimeConfigSO   │  │   DayCycleConfigSO  │  │  ScoreConfigSO  │
   │                 │  │   ├─ DayPlanSO[]    │  │                 │
   │                 │  │   │   └─ QuestDefSO │  │                 │
   └─────────────────┘  └─────────────────────┘  └─────────────────┘

   Runtime managers (singleton, DontDestroyOnLoad):
   GameBootstrap → TimeManager → QuestManager → ScoreManager → UIManager
                                      │
                                      ↓ broadcast
                               GameEvents (static)
                                      ↓
              QuestHUD, ScoreHUD, ClockHUD, NPCController, ...

   AI plugins (optional):
   PhaseAChatService (Sentis ONNX) ← DialogueManager
   PhaseBMovementService (Sentis ONNX) ← NPCController
```

> [!info] Pattern chính
> **Static event hub** + **typed runtime state** + **SO-as-config**. Không Zenject/VContainer. Quyền dễ debug, dễ refactor sau khi nộp đồ án.

---

## Boot order — GameBootstrap

Scene `_Boot.unity` chứa duy nhất 1 GameObject `[GameBootstrap]`. Awake order:

```csharp
namespace TrainAI.Core.Bootstrap
{
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameDatabaseSO database;
        [SerializeField] private string firstScene = "Title";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            // 1. Database resolve (đọc tất cả SO refs ra runtime cache)
            GameDatabase.Init(database);
            // 2. Core managers (đăng ký vào ServiceLocator)
            ServiceLocator.Register(new SaveManager());
            ServiceLocator.Register(new TimeManager(database.timeConfig));
            ServiceLocator.Register(new ScoreManager(database.scoreConfig));
            ServiceLocator.Register(new QuestManager(database.dayCycle));
            ServiceLocator.Register(new SceneFlowManager());
            ServiceLocator.Register(new UIManager(database.uiText));
            ServiceLocator.Register(new InteractionManager());
            ServiceLocator.Register(new DialogueManager(database.npcs));
            // 3. AI plugins (chỉ init nếu asset có)
            if (database.phaseAChat != null)
                ServiceLocator.Register(new PhaseAChatService(database.phaseAChat));
            if (database.phaseBMovement != null)
                ServiceLocator.Register(new PhaseBMovementService(database.phaseBMovement));
            // 4. Load Title
            SceneManager.LoadScene(firstScene);
        }
    }
}
```

> [!warning] Init order matter
> `QuestManager` cần `TimeManager` để subscribe tick → register Time TRƯỚC Quest. `UIManager` cần text catalog → register sau database. ServiceLocator throw exception nếu request service chưa register.

---

## ScriptableObject catalog

Đây là **trái tim** của doc. Schema chi tiết từng SO type — Quyền sao chép vào C# luôn được.

### `GameDatabaseSO` — root

```csharp
[CreateAssetMenu(menuName = "TrainAI/Database/Root")]
public class GameDatabaseSO : ScriptableObject
{
    [Header("Core")]
    public TimeConfigSO timeConfig;
    public ScoreConfigSO scoreConfig;
    public DayCycleConfigSO dayCycle;
    public UITextSO uiText;
    public UIThemeSO uiTheme;

    [Header("Content catalog")]
    public List<SubjectSO> subjects;
    public List<NPCProfileSO> npcs;
    public List<SceneRouteSO> scenes;
    public List<InteractableSO> interactables;

    [Header("AI plugins (optional)")]
    public PhaseAChatConfigSO phaseAChat;
    public PhaseBMovementConfigSO phaseBMovement;

    [Header("Settings")]
    public string playerNameDefault = "Học viên";
    public float autosaveIntervalMinutes = 5f;
}
```

### `TimeConfigSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Time/Config")]
public class TimeConfigSO : ScriptableObject
{
    [Header("Tỷ lệ thời gian")]
    [Tooltip("1 giờ in-game = bao nhiêu giây thực")]
    public float secondsPerGameHour = 180f;        // GDD: 1h = 3p = 180s
    [Tooltip("Phát tick mỗi N phút game")]
    public int tickEveryGameMinutes = 1;

    [Header("Vòng lặp ngày")]
    public int firstDay = 1;
    public int totalDays = 30;
    public bool skipWeekend = true;                // GDD: skip thứ 7 + CN
    public DayOfWeek weekStart = DayOfWeek.Monday; // ngày 1 = thứ 2

    [Header("Quy tắc reset ngày")]
    [Tooltip("Sang ngày mới: thời điểm = quest đầu tiên - X phút")]
    public int dayStartOffsetMinutesBeforeFirstQuest = 30;

    [Header("Late penalty")]
    [Tooltip("Quá X phút game không tới quest hiện tại → đánh dấu Late")]
    public int lateAfterMinutes = 15;              // GDD: 15p
}
```

### `DayCycleConfigSO` + `DayPlanSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Quest/Day Cycle")]
public class DayCycleConfigSO : ScriptableObject
{
    [Tooltip("Index 0 = ngày 1, index 1 = ngày 2, ...")]
    public List<DayPlanSO> dayPlans;
}

[CreateAssetMenu(menuName = "TrainAI/Quest/Day Plan")]
public class DayPlanSO : ScriptableObject
{
    public int dayNumber;            // 1..30
    public string label;             // "Ngày 1 — chào nhập ngũ"
    [Tooltip("Quest theo thứ tự sẽ active trong ngày")]
    public List<QuestDefSO> quests;
    [Tooltip("Có cutscene mở đầu ngày không")]
    public CutsceneSO openingCutscene;   // nullable
}
```

### `QuestDefSO`

Quan trọng nhất — định nghĩa 1 nhiệm vụ.

```csharp
public enum QuestType
{
    Exercise,        // Sân vận động — UIConfirm + skip thời gian
    Cleaning,        // Khu vệ sinh — UIConfirm + skip thời gian
    Eat,             // Phòng ăn — vào scene Cafeteria → tương tác bàn → UIConfirm
    StudyMorning,    // Phòng học — vào scene Classroom → UIQuiz
    StudyAfternoon,  // Tương tự StudyMorning, khác bộ đề
    Sleep,           // Ký túc xá — UIConfirm "Đi ngủ" → next day
    FreeRoam,        // Không phải nhiệm vụ thực, chỉ là cửa sổ thời gian rảnh
    Cutscene,        // Chỉ chạy cutscene
    Custom,          // Hook code riêng (advanced minigame)
}

[CreateAssetMenu(menuName = "TrainAI/Quest/Quest Def")]
public class QuestDefSO : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // "Q_Exercise_0500"
    public string title;              // "Tập thể dục"
    [TextArea] public string descriptionTemplate;  // "Tập thể dục sáng ({deadline})"

    [Header("Type")]
    public QuestType type;

    [Header("Time window (in-game)")]
    public int startHour = 5;
    public int startMinute = 0;
    public int deadlineHour = 5;
    public int deadlineMinute = 15;
    [Tooltip("Sau khi xong, đẩy thời gian tới giờ X (GDD: 'Thời gian chuyển tới Xh')")]
    public int skipToHour = 6;
    public int skipToMinute = 0;

    [Header("Location")]
    public SceneRouteSO targetScene;     // null = cùng scene World
    public string interactableLocationKey; // khớp với InteractableSO.key

    [Header("UI")]
    public string confirmText;           // GDD: "Bạn đang tập thể dục"
    public string okButtonText = "OK";

    [Header("Quiz (chỉ dùng cho Study*)")]
    public SubjectSO subject;            // môn học của buổi
    public QuizSetSO quizSet;            // bộ 10 câu

    [Header("Score impact")]
    [Tooltip("Trừ điểm rèn luyện nếu Late hoặc Missed")]
    public int penaltyOnLate = 5;
    public int penaltyOnMissed = 5;
}
```

> [!info] Tại sao 1 SO/quest không phải mảng inline trong DayPlan?
> 1) Reuse: `Q_Exercise_0500` có thể xuất hiện ngày 1, 2, 3, ... (lifecycle lặp). 2) Diff sạch: edit 1 quest không bẩn diff DayPlan. 3) Reference: `QuestRuntimeState` chỉ giữ asset GUID, save file nhỏ.

### `SubjectSO` + `QuizSetSO` + `QuestionSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Quiz/Subject")]
public class SubjectSO : ScriptableObject
{
    public string id;                    // "S_LichSu"
    public string displayName;           // "Lịch sử"
    public Sprite icon;
    [Tooltip("Map session index → bộ đề. Khi DayPlan ref Subject, dùng QuestDef.quizSet trực tiếp.")]
    public List<QuizSetSO> sessions;     // optional helper
}

[CreateAssetMenu(menuName = "TrainAI/Quiz/Quiz Set")]
public class QuizSetSO : ScriptableObject
{
    public string id;                    // "QS_LichSu_Bai01"
    public string title;                 // "Lịch sử Việt Nam — Buổi 1"
    public List<QuestionSO> questions;   // expect 10
    public float secondsPerQuestion = 15f;
}

[CreateAssetMenu(menuName = "TrainAI/Quiz/Question")]
public class QuestionSO : ScriptableObject
{
    [TextArea] public string stem;
    public string[] options = new string[4];
    [Range(0, 3)] public int correctIndex;
    [TextArea] public string explanation;   // optional, hiển thị sau khi chọn
}
```

> [!tip] Workflow Quyền
> Tạo 1 SubjectSO "Lịch sử" → tạo nhiều QuizSetSO con (Buổi 1, Buổi 2, ...) → mỗi QuizSet có 10 QuestionSO. Mỗi QuestDefSO study chỉ cần ref QuizSet tương ứng. Đổi câu hỏi không phải build lại code.

### `ScoreConfigSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Score/Config")]
public class ScoreConfigSO : ScriptableObject
{
    [Header("Starting values")]
    public int startingDiscipline = 100;
    public int startingAcademic = 0;

    [Header("Maximum")]
    public int maxDiscipline = 100;
    public int maxAcademic = 480;

    [Header("Quiz scoring")]
    [Tooltip("1 câu trả lời đúng = bao nhiêu điểm/buổi (10 câu × 1 = 10đ/buổi)")]
    public float pointsPerQuizQuestion = 1f;
    [Tooltip("Quy đổi % hoàn thành buổi → điểm (round)")]
    public bool roundQuizScore = true;

    [Header("Discipline penalty")]
    public int penaltyLate = 5;
    public int penaltyMissed = 5;

    [Header("Game-over")]
    [Tooltip("Discipline ≤ ngưỡng này = đuổi học (UIConfirm)")]
    public int kickOutThreshold = 0;

    [Header("Grade thresholds (% of max)")]
    public List<GradeThreshold> disciplineGrades;   // Excellent ≥90, Good 60-90, Avg <60
    public List<GradeThreshold> academicGrades;     // Excellent ≥432, Good 288-431, Avg <288
}

[Serializable]
public class GradeThreshold
{
    public string label;            // "Xuất sắc"
    public int minScoreInclusive;   // 432
    public int maxScoreInclusive;   // 480
    public Color uiColor;
}
```

### `NPCProfileSO`

```csharp
public enum NPCAIMode
{
    None,               // Static, không AI
    SentisChat,         // Phase A — chỉ chat
    Movement,           // Phase B — chỉ tự đi
    ChatAndMovement,    // Cả hai
}

[CreateAssetMenu(menuName = "TrainAI/NPC/Profile")]
public class NPCProfileSO : ScriptableObject
{
    public string id;                    // "NPC_DaiDoiTruong"
    public string displayName;
    public Sprite portrait;
    public NPCAIMode aiMode;

    [Header("Dialogue (Sentis chat)")]
    public string greetingTemplate;      // "Chào {playerName}, có chuyện gì vậy?"
    public List<string> fallbackResponses;

    [Header("Movement (PPO)")]
    public NPCScheduleSO schedule;       // null nếu không di chuyển
    public float moveSpeed = 2f;
}

[CreateAssetMenu(menuName = "TrainAI/NPC/Schedule")]
public class NPCScheduleSO : ScriptableObject
{
    [Serializable]
    public struct ScheduleEntry { public int hour; public Transform locationAnchor; public string locationKey; }
    public List<ScheduleEntry> entries;  // {6h: SanVanDong, 7h: NhaAn, ...}
}
```

> [!warning] `Transform` trong SO?
> SO không serialize Transform reference (đó là scene object). Workaround: SO giữ `string locationKey`, Manager runtime resolve key → anchor trong scene đang load. Schedule entries dùng key, Inspector hiển thị danh sách dropdown từ `InteractableSO`.

### `SceneRouteSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Scene/Route")]
public class SceneRouteSO : ScriptableObject
{
    public string id;                    // "R_Classroom"
    public string sceneName;             // "Classroom" (Build Settings)
    public string loadingText;           // GDD: "Đang vào lớp học..."
    public float fadeInSeconds = 0.5f;
    public float fadeOutSeconds = 0.5f;
    public float minLoadingScreenSeconds = 3f;  // GDD: tồn tại 3s
    [Tooltip("Có pause TimeManager khi ở scene này không (GDD: scene quest đóng băng đồng hồ)")]
    public bool freezeTimeWhileLoaded = true;
}
```

### `InteractableSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/Scene/Interactable")]
public class InteractableSO : ScriptableObject
{
    public string key;                   // "SanVanDong" — khớp QuestDef.interactableLocationKey
    public string displayLabel;          // hiển thị khi nút sáng
    public InteractableKind kind;        // QuestPoint, NPC, SceneDoor
    public SceneRouteSO doorTarget;      // nullable, chỉ khi kind == SceneDoor
}

public enum InteractableKind { QuestPoint, NPC, SceneDoor }
```

### `UITextSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/UI/Text Catalog")]
public class UITextSO : ScriptableObject
{
    [Serializable] public struct Entry { public string key; [TextArea] public string text; }
    public List<Entry> entries;
    // Method: string Get(string key)
    // Vd entries: "EndDay" → "Hết ngày, đang chuyển sang ngày tiếp theo..."
}
```

### `PhaseAChatConfigSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/AI/Phase A — Chat")]
public class PhaseAChatConfigSO : ScriptableObject
{
    public ModelAsset onnxModel;         // intent_classifier_v2.onnx
    public TextAsset slotVocabJson;      // slot_vocab.json
    public TextAsset intentLabelsJson;   // 8 intent labels
    [Range(0f, 1f)] public float minConfidence = 0.5f;
    public bool useV2EntityExtractor = true;   // V1/V2 split
}
```

### `PhaseBMovementConfigSO`

```csharp
[CreateAssetMenu(menuName = "TrainAI/AI/Phase B — Movement")]
public class PhaseBMovementConfigSO : ScriptableObject
{
    public ModelAsset onnxModel;         // soldier.onnx hoặc soldier_m2.onnx
    public TextAsset metaJson;           // soldier.meta.json — 21 floats layout
    public float decisionPeriodSeconds = 0.1f;
    public float arriveDistance = 0.3f;
}
```

---

## Time system

```csharp
namespace TrainAI.Core.Time
{
    [Serializable]
    public struct GameTime
    {
        public int day;          // 1..30
        public int hour;          // 0..23
        public int minute;        // 0..59
        public DayOfWeek weekday;
    }

    public interface ITimeTickListener { void OnTick(GameTime now); }

    public class TimeManager
    {
        private readonly TimeConfigSO _cfg;
        private GameTime _now;
        private float _accumSeconds;
        private bool _frozen;
        private readonly List<ITimeTickListener> _listeners = new();

        public GameTime Now => _now;
        public bool IsFrozen => _frozen;

        public event Action<GameTime> OnTick;
        public event Action<int /*newDay*/> OnNewDay;

        public void Freeze() => _frozen = true;
        public void Resume() => _frozen = false;

        public void Tick(float deltaRealSeconds)
        {
            if (_frozen) return;
            float secsPerMin = _cfg.secondsPerGameHour / 60f;   // 3s = 1 phút game
            _accumSeconds += deltaRealSeconds;
            while (_accumSeconds >= secsPerMin)
            {
                _accumSeconds -= secsPerMin;
                AdvanceMinute(_cfg.tickEveryGameMinutes);
            }
        }

        public void JumpTo(int hour, int minute) { /* set + broadcast 1 tick */ }
        public void NextDay() { /* day++, skip weekend, reset time, OnNewDay */ }
    }
}
```

`TimeManager` được driven bởi 1 `MonoBehaviour TimeTickDriver` ở scene `_Boot` — gọi `Tick(Time.deltaTime)` mỗi frame.

> [!tip] Frozen mode
> Khi `SceneRouteSO.freezeTimeWhileLoaded == true`, `SceneFlowManager` gọi `TimeManager.Freeze()` sau load scene và `Resume()` khi rời scene. Áp dụng cho Classroom (làm quiz đóng băng đồng hồ — GDD).

### Skip cuối tuần

```csharp
public void NextDay()
{
    _now.day++;
    if (_cfg.skipWeekend && _now.weekday == DayOfWeek.Saturday)
    {
        _now.day += 2;       // skip CN
        _now.weekday = DayOfWeek.Monday;
    }
    else _now.weekday = NextWeekday(_now.weekday);
    // Reset giờ về quest đầu tiên - 30p
    var firstQuest = ServiceLocator.Get<QuestManager>().GetFirstQuestOf(_now.day);
    SetTime(firstQuest.startHour, firstQuest.startMinute - _cfg.dayStartOffsetMinutesBeforeFirstQuest);
    OnNewDay?.Invoke(_now.day);
}
```

---

## Quest system + daily lifecycle

```csharp
public enum QuestStatus { NotStarted, Active, Completed, Late, Missed }

[Serializable]
public class QuestRuntimeState
{
    public string questId;
    public QuestStatus status;
    public GameTime startedAt;
    public GameTime completedAt;
    public int scoreEarned;       // điểm học tập
    public int scorePenalty;      // điểm rèn luyện trừ
}

public class QuestManager : ITimeTickListener
{
    private readonly DayCycleConfigSO _cfg;
    private DayPlanSO _todayPlan;
    private int _currentQuestIndex;
    private readonly List<QuestRuntimeState> _todayStates = new();

    public QuestDefSO CurrentQuest => _todayPlan.quests[_currentQuestIndex];

    public void OnTick(GameTime now)
    {
        var q = CurrentQuest;
        var st = _todayStates[_currentQuestIndex];
        // Late check (GDD: 15p)
        if (st.status == QuestStatus.NotStarted &&
            MinutesSince(now, q.startHour, q.startMinute) > _cfg.lateAfterMinutes)
        {
            st.status = QuestStatus.Late;
            ServiceLocator.Get<ScoreManager>().PenalizeDiscipline(q.penaltyOnLate);
            GameEvents.QuestLate?.Invoke(q);
        }
        // Missed check (qua deadline mà chưa start)
        if (st.status == QuestStatus.NotStarted && PastDeadline(now, q))
        {
            st.status = QuestStatus.Missed;
            ServiceLocator.Get<ScoreManager>().PenalizeDiscipline(q.penaltyOnMissed);
            AdvanceToNextQuest();
        }
    }

    public void StartCurrentQuest() { /* status = Active, broadcast */ }
    public void CompleteCurrentQuest(int scoreEarned)
    {
        var q = CurrentQuest;
        var st = _todayStates[_currentQuestIndex];
        st.status = QuestStatus.Completed;
        st.scoreEarned = scoreEarned;
        ServiceLocator.Get<ScoreManager>().AddAcademic(scoreEarned);
        // Skip thời gian theo GDD
        ServiceLocator.Get<TimeManager>().JumpTo(q.skipToHour, q.skipToMinute);
        AdvanceToNextQuest();
    }
}
```

### Quest runner pattern

Mỗi `QuestType` có 1 runner riêng (Strategy pattern), không nhồi `if/switch` vào QuestManager:

```csharp
public interface IQuestRunner { void Run(QuestDefSO quest, Action<int> onComplete); }
public class ExerciseRunner : IQuestRunner { /* show UIConfirm → onComplete(0) */ }
public class StudyRunner : IQuestRunner    { /* push QuizScreen → score = đúng × pointsPerQuizQuestion */ }
public class SleepRunner : IQuestRunner    { /* UIConfirm → save → next day */ }
// etc.

public class QuestRunnerRegistry
{
    private readonly Dictionary<QuestType, IQuestRunner> _map;
    public IQuestRunner Get(QuestType t) => _map[t];
}
```

Khi player tương tác đúng location của `CurrentQuest`:
```
InteractionManager → QuestManager.StartCurrentQuest()
  → QuestRunnerRegistry.Get(quest.type).Run(quest, onComplete: score => QuestManager.CompleteCurrentQuest(score))
```

> [!info] Vì sao Strategy không Switch?
> Mỗi runner là 1 file ngắn. Add type mới (vd Advanced minigame "bắn cung") = thêm `ArcheryRunner.cs`, không sửa code cũ. Đúng GDD note: "Advanced: thay vì làm quiz thì làm các minigame khác (Code sau)".

---

## Score system

```csharp
public class ScoreManager
{
    private readonly ScoreConfigSO _cfg;
    public int Discipline { get; private set; }
    public int Academic { get; private set; }

    public event Action<int /*newDiscipline*/, int /*delta*/> OnDisciplineChanged;
    public event Action<int, int> OnAcademicChanged;
    public event Action OnKickedOut;

    public void PenalizeDiscipline(int amount)
    {
        Discipline = Mathf.Max(0, Discipline - amount);
        OnDisciplineChanged?.Invoke(Discipline, -amount);
        if (Discipline <= _cfg.kickOutThreshold) OnKickedOut?.Invoke();
    }

    public void AddAcademic(int amount)
    {
        Academic = Mathf.Min(_cfg.maxAcademic, Academic + amount);
        OnAcademicChanged?.Invoke(Academic, amount);
    }

    public string GradeFor(int score, List<GradeThreshold> table)
        => table.FirstOrDefault(t => score >= t.minScoreInclusive && score <= t.maxScoreInclusive)?.label ?? "—";
}
```

Quy đổi % → điểm cho quiz (GDD: "66% → 7, 92% → 9"):
```csharp
int correctCount = quizRuntime.CorrectCount;       // 0..10
int pointsForSession = Mathf.RoundToInt(correctCount * _cfg.pointsPerQuizQuestion);
// hoặc nếu pointsPerQuizQuestion = 1 → 7 đúng = 7đ
```

---

## Quiz system

```csharp
public class QuizRuntime
{
    private readonly QuizSetSO _set;
    private int _currentIdx;
    private float _remainingSeconds;
    public int CorrectCount { get; private set; }
    public bool IsFinished => _currentIdx >= _set.questions.Count;
    public QuestionSO Current => _set.questions[_currentIdx];

    public void Tick(float dt)
    {
        if (IsFinished) return;
        _remainingSeconds -= dt;
        if (_remainingSeconds <= 0) Answer(-1);   // skip = wrong
    }
    public void Answer(int chosenIdx)
    {
        if (chosenIdx == Current.correctIndex) CorrectCount++;
        _currentIdx++;
        _remainingSeconds = _set.secondsPerQuestion;
        // UI hiển thị xanh/đỏ + nút Continue → khi bấm gọi Next()
    }
}

public class QuizManager
{
    public QuizRuntime StartQuiz(QuizSetSO set) { /* push UIQuiz screen, init runtime */ }
    public void EndQuiz(QuizRuntime rt)
    {
        var pts = (int)(rt.CorrectCount * GameDatabase.Score.pointsPerQuizQuestion);
        // pop screen → SceneFlowManager.Return() → QuestManager.CompleteCurrentQuest(pts)
    }
}
```

> [!tip] Timer ở góc phải
> `UIQuizScreen` subscribe `QuizRuntime` → render countdown bar. Đáp án xanh/đỏ feedback rồi nút Continue (GDD specs).

---

## Dialogue + Sentis chat integration

Tham chiếu deep: [[systems/sentis-chat]], [[systems/entity-extractor]].

```csharp
public class DialogueManager
{
    private readonly Dictionary<string, NPCProfileSO> _npcById;
    public event Action<NPCProfileSO> OnDialogueOpened;
    public event Action<string /*line*/> OnLineDisplayed;

    public void OpenDialogue(NPCProfileSO npc)
    {
        OnDialogueOpened?.Invoke(npc);
        var greet = npc.greetingTemplate.Replace("{playerName}", PlayerData.Name);
        OnLineDisplayed?.Invoke(greet);
    }

    public async Task SubmitPlayerInput(string text, NPCProfileSO npc)
    {
        if (npc.aiMode == NPCAIMode.SentisChat || npc.aiMode == NPCAIMode.ChatAndMovement)
        {
            var chat = ServiceLocator.Get<PhaseAChatService>();
            var (intent, conf) = await chat.PredictIntent(text);
            var slots = chat.ExtractEntities(text);
            var response = chat.FormatResponse(intent, slots, npc, PlayerData);
            OnLineDisplayed?.Invoke(response);
        }
        else
        {
            var fallback = npc.fallbackResponses[Random.Range(0, npc.fallbackResponses.Count)];
            OnLineDisplayed?.Invoke(fallback);
        }
    }
}
```

`PhaseAChatService` wrap ONNX inference (Unity InferenceEngine 2.6, namespace `Unity.InferenceEngine`) — chi tiết [[technical/unity-integration]].

```csharp
public class PhaseAChatService
{
    private readonly Worker _worker;
    private readonly EntityExtractor _entity;
    private readonly string[] _intentLabels;
    public PhaseAChatService(PhaseAChatConfigSO cfg)
    {
        var model = ModelLoader.Load(cfg.onnxModel);
        _worker = new Worker(model, BackendType.GPUCompute);
        _entity = new EntityExtractor(cfg.slotVocabJson.text);
        _intentLabels = JsonUtility.FromJson<LabelList>(cfg.intentLabelsJson.text).labels;
    }
    // PredictIntent / ExtractEntities / FormatResponse
}
```

> [!info] V1 vs V2
> Theo [memory project_phase_a_versions], 2 model deploy song song. `PhaseAChatConfigSO` có flag `useV2EntityExtractor` — Quyền có thể tạo 2 asset config (v1/v2) và A/B test. NPC ref config nào → dùng version đó.

---

## NPC AI movement integration

Tham chiếu deep: [[systems/movement-ai]].

```csharp
public class NPCMovementBrain : MonoBehaviour
{
    [SerializeField] private NPCProfileSO profile;
    private PhaseBMovementService _service;
    private Vector3 _goal;
    private float _decisionTimer;

    private void Start()
    {
        _service = ServiceLocator.Get<PhaseBMovementService>();
        // Đăng ký lịch
        ServiceLocator.Get<TimeManager>().OnTick += UpdateGoalFromSchedule;
    }

    private void UpdateGoalFromSchedule(GameTime now)
    {
        var entry = profile.schedule.entries.LastOrDefault(e => e.hour <= now.hour);
        _goal = ResolveAnchor(entry.locationKey).position;
    }

    private void Update()
    {
        _decisionTimer += Time.deltaTime;
        if (_decisionTimer < _service.Config.decisionPeriodSeconds) return;
        _decisionTimer = 0;
        // 21-float observation theo soldier.meta.json
        var obs = ObservationBuilder.Build(transform.position, _goal, /* obstacles */);
        var action = _service.Predict(obs);  // discrete: 0..3 (lên/xuống/trái/phải/đứng)
        ApplyMove(action);
    }
}
```

`PhaseBMovementService` load `soldier.onnx`, expose `Predict(float[21]) → int`. ObservationLayout theo `soldier.meta.json` — KHÔNG được đổi index nếu không retrain.

> [!warning] Contract observation
> Theo CLAUDE.md `.wiki/`: "Phase B observation contract: 21 floats, layout cố định trong `deliverables/soldier.meta.json`. Bất kỳ thay đổi nào trong `nav_env.py` phải cập nhật meta + retrain để giữ contract." Quyền chỉ đọc meta, không đoán index.

---

## UI state machine

```csharp
public abstract class UIScreenBase : MonoBehaviour
{
    public abstract string ScreenId { get; }
    public virtual bool IsModal => false;       // modal blocks input dưới
    public virtual void OnPush(object args) {}
    public virtual void OnPop() {}
    public virtual void OnFocus() {}            // top of stack
    public virtual void OnBlur() {}
}

public class UIManager
{
    private readonly Stack<UIScreenBase> _stack = new();
    public T Push<T>(object args = null) where T : UIScreenBase { /* instantiate prefab, OnPush, push */ }
    public void Pop() { /* OnPop top, OnFocus next */ }
    public void Replace<T>() { /* clear stack + Push */ }
}
```

### Catalog screens (theo GDD)

| Screen | Type | Modal | Mô tả |
|---|---|---|---|
| `MainMenuScreen` | Replace | yes | NewGame / Continue / Quit |
| `CutsceneScreen` | Push | yes | VideoPlayer + skip |
| `CharacterCreateScreen` | Replace | yes | InputField + nút Confirm |
| `GameplayHUDScreen` | Replace | no | Joystick + Clock + MiniMap + InteractButton + QuestText + Score + Avatar |
| `LoadingScreen` | Push (overlay) | yes | Màn đen + text + 3s hold |
| `QuizScreen` | Push | yes | Câu hỏi + 4 đáp + countdown 15s |
| `ConfirmScreen` | Push | yes | Text "Bạn đang ..." + OK |
| `DialogueScreen` | Push | yes | InputField + Send + bubble |
| `EndingScreen` | Replace | yes | Kết quả 30 ngày |
| `KickedOutScreen` | Replace | yes | "Bạn bị đuổi học!" |

### `GameplayHUDScreen` widgets

```csharp
public class GameplayHUDScreen : UIScreenBase
{
    [SerializeField] private JoystickWidget joystick;
    [SerializeField] private ClockHUD clock;
    [SerializeField] private MiniMapHUD miniMap;
    [SerializeField] private InteractButton interactBtn;
    [SerializeField] private QuestHUD questHUD;
    [SerializeField] private ScoreHUD scoreHUD;
    [SerializeField] private AvatarHUD avatarHUD;
    // Subscribe GameEvents.OnTick / OnQuestChanged / OnScoreChanged
}
```

`InteractButton` listen `InteractionManager.OnInteractableInRange` → enable/disable màu theo GDD ("nút sáng / tối phụ thuộc hướng đứng").

---

## Scene flow + interaction

```csharp
public class SceneFlowManager
{
    public async Task Travel(SceneRouteSO route)
    {
        var ui = ServiceLocator.Get<UIManager>();
        var loading = ui.Push<LoadingScreen>(route.loadingText);
        await Fade(route.fadeOutSeconds);
        await SceneManager.LoadSceneAsync(route.sceneName);
        await Task.Delay(TimeSpan.FromSeconds(route.minLoadingScreenSeconds));
        if (route.freezeTimeWhileLoaded) ServiceLocator.Get<TimeManager>().Freeze();
        await Fade(route.fadeInSeconds);
        ui.Pop();   // close loading
    }
    public async Task Return() { /* back to World, Resume time */ }
}
```

### Interaction

`InteractableTrigger` (Collider trên GameObject scene):

```csharp
public class InteractableTrigger : MonoBehaviour
{
    [SerializeField] private InteractableSO data;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ServiceLocator.Get<InteractionManager>().Enter(data, this);
    }
    private void OnTriggerExit(Collider other) { /* Exit */ }
}

public class InteractionManager
{
    private InteractableSO _current;
    public event Action<InteractableSO> OnInteractableInRange;
    public event Action OnInteractableOutOfRange;

    public void Enter(InteractableSO data, InteractableTrigger _)
    {
        _current = data;
        OnInteractableInRange?.Invoke(data);
    }
    // Player nhấn nút interact (Input):
    public void Activate()
    {
        if (_current == null) return;
        var qm = ServiceLocator.Get<QuestManager>();
        if (_current.kind == InteractableKind.QuestPoint)
        {
            // GDD: "Đến nhiệm vụ nào thì mới có thể tương tác với khu vực của nhiệm vụ đó"
            if (qm.CurrentQuest.interactableLocationKey != _current.key) { /* play locked sound */ return; }
            qm.StartCurrentQuest();
        }
        else if (_current.kind == InteractableKind.NPC) { /* DialogueManager.OpenDialogue */ }
        else if (_current.kind == InteractableKind.SceneDoor) { /* SceneFlowManager.Travel */ }
    }
}
```

> [!info] Hướng camera "sáng/tối" nút
> GDD: "nút sáng khi camera quay đến vị trí có thể tương tác". Implementation: `InteractableTrigger` ngoài collider distance còn check `Vector3.Dot(camera.forward, dirToInteractable) > threshold`. Threshold đẩy vào `InteractableSO.aimDotThreshold` (default 0.6).

---

## Save / Load

**Format JSON (KHÔNG dùng BinaryFormatter)** — vì:
1. Wiki [[technical/datn-architecture]] đã cảnh báo deprecated.
2. JSON debug được, examiner đọc được file save.
3. Versioning dễ — thêm field không break.

```csharp
[Serializable]
public class GameSaveData
{
    public int saveFormatVersion = 1;
    public string playerName;
    public int day;
    public int hour;
    public int minute;
    public int discipline;
    public int academic;
    public string currentSceneId;
    public Vector3SerDe playerPosition;
    public List<DayQuestStateSer> questHistory;     // [day][questId → status, score]
    public string activeNPCDialogueId;              // null nếu không
}

public class SaveManager
{
    private string Path => Application.persistentDataPath + "/save.json";

    public void Save()
    {
        var data = BuildSnapshot();
        var json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(Path + ".tmp", json);
        File.Replace(Path + ".tmp", Path, Path + ".bak");   // atomic
    }

    public bool TryLoad(out GameSaveData data) { /* parse, version migrate */ }
}
```

> [!tip] Autosave
> Theo `GameDatabaseSO.autosaveIntervalMinutes` — `SaveManager` subscribe `TimeManager.OnTick` → mỗi N phút game gọi Save. Plus: save khi sleep, khi vào scene mới.

---

## Workflow: Quyền config content

Khi muốn thêm 1 ngày mới (vd Ngày 15):

1. **Project window** → `Assets/Configs/DayPlans/` → Right-click → Create → TrainAI/Quest/Day Plan → đặt tên `Day15.asset`.
2. Mở `Day15.asset` → set `dayNumber = 15`, `label = "Tuần 3 — kiểm tra"`.
3. Dùng các `QuestDefSO` có sẵn (`Q_Exercise_0500`, `Q_Cleaning_0600`, ...) drag vào field `quests`.
4. Nếu cần buổi học mới: tạo `QS_LichSu_Bai15.asset` (10 QuestionSO con) → assign vào `Q_StudyMorning_15.quizSet`.
5. Mở `_Database.asset` → drag `Day15.asset` vào `dayCycle.dayPlans` (hoặc edit DayCycleConfigSO trực tiếp).
6. Play → vào ngày 15 thấy quest mới.

**Không build lại C#.** Mọi rule (giờ, deadline, điểm trừ) đã trong SO.

> [!info] CustomEditor (optional)
> Có thể viết `[CustomEditor(typeof(DayCycleConfigSO))]` show timeline visual — Quyền kéo thả quest. Phase 2.

---

## Roadmap implementation (7 ngày demo)

Theo [[overview]] core pillar: ưu tiên 7 ngày demo, không bắt buộc 30. Tách checkpoint theo ngày thực:

| Ngày | Việc | Output |
|---|---|---|
| 1 | Cấu trúc thư mục + `GameBootstrap` + `ServiceLocator` + `TimeManager` + scene `_Boot` + `Title` | Đồng hồ chạy được, scene title hiện |
| 2 | Toàn bộ class SO (chỉ class, chưa asset) + `GameDatabaseSO` + 1 SO instance mỗi loại để test | SO tạo được, Inspector đẹp |
| 3 | `UIManager` + `MainMenuScreen` + `CharacterCreateScreen` + `GameplayHUDScreen` (joystick + clock) | Có thể vào game, di chuyển |
| 4 | `QuestManager` + `QuestRunnerRegistry` (Exercise + Sleep + Cleaning) + `InteractionManager` + 1 day plan demo (3 quest) | Chơi 1 ngày: tập thể dục → dọn vệ sinh → ngủ |
| 5 | `QuizManager` + `QuizScreen` + 1 SubjectSO + 1 QuizSet 5 câu + `StudyRunner` + `ScoreManager` HUD | Buổi học làm quiz, điểm cập nhật |
| 6 | `SceneFlowManager` + scene Classroom/Dormitory/Cafeteria + `LoadingScreen` + `ConfirmScreen` | Chuyển scene mượt |
| 7 | `DialogueManager` + `DialogueScreen` + tích hợp Phase A v2 | NPC chat tiếng Việt |
| (Bonus) | `NPCMovementBrain` + Phase B + `EndingScreen` + Save/Load + 7 day plans đầy đủ | Demo 7 ngày end-to-end |

> [!tip] Cắt scope
> Nếu deadline gắt: bỏ `NPCMovementBrain` + Save/Load, demo 3 ngày là đủ POC. AI Phase A là show-piece — giữ.

---

## Anti-patterns nên tránh

Tổng hợp từ kinh nghiệm template farming cũ ([[technical/datn-architecture]]):

| Anti-pattern | Tại sao tránh | Thay bằng |
|---|---|---|
| `GameBlackboard` magic-string | Không type-safe, refactor khổ | Typed POCO trong `QuestRuntimeState`, `PlayerSaveData` |
| `BinaryFormatter` save | Deprecated, security risk | JSON `JsonUtility` hoặc `Newtonsoft.Json` |
| Hardcoded LLM API key | Security | Phase A chạy local Sentis, không call API |
| 16 singleton `DontDestroyOnLoad` | Init order bug | `ServiceLocator` + 1 `GameBootstrap` |
| Resources.LoadAll auto-discover | Magic, khó trace | Direct ref qua `GameDatabaseSO` |
| Switch case `if (questType == X)` rải khắp | Thêm type = sửa N file | Strategy `IQuestRunner` registry |
| Time tick poll trong mỗi system | Lag, trùng logic | `ITimeTickListener` + 1 driver |
| Hardcode "1h = 3p" trong code | Sửa balance phải build lại | `TimeConfigSO.secondsPerGameHour` |
| UIScreen tự load asset | Coupling | `UIManager.Push<T>()` resolve prefab từ SO |

---

## Mapping GDD → SO

Cross-reference cuối, đảm bảo không miss requirement nào của GDD:

| GDD requirement | SO chịu trách nhiệm |
|---|---|
| 1h = 3p thực | `TimeConfigSO.secondsPerGameHour` |
| 30 ngày | `TimeConfigSO.totalDays` + `DayCycleConfigSO.dayPlans` |
| Skip thứ 7 + CN | `TimeConfigSO.skipWeekend` |
| Reset = quest đầu - 30p | `TimeConfigSO.dayStartOffsetMinutesBeforeFirstQuest` |
| Late 15p trừ điểm | `TimeConfigSO.lateAfterMinutes` + `QuestDefSO.penaltyOnLate` |
| Đóng băng đồng hồ trong scene quest | `SceneRouteSO.freezeTimeWhileLoaded` |
| 100đ rèn luyện max | `ScoreConfigSO.maxDiscipline` |
| 480đ học tập max | `ScoreConfigSO.maxAcademic` |
| Trừ 5đ khi late/missed | `ScoreConfigSO.penaltyLate/Missed` |
| Xếp hạng XS/Tốt/TB | `ScoreConfigSO.disciplineGrades / academicGrades` |
| Đuổi học khi rèn luyện ≤ 0 | `ScoreConfigSO.kickOutThreshold` |
| 15s/câu quiz | `QuizSetSO.secondsPerQuestion` |
| 10 câu/buổi | `QuizSetSO.questions` (count = 10) |
| Mỗi buổi 1 môn | `QuestDefSO.subject + quizSet` |
| UIConfirm "bạn đang tập thể dục" | `QuestDefSO.confirmText` |
| "Skip thời gian tới Xh" | `QuestDefSO.skipToHour/Minute` |
| Tương tác sai khu vực không kích | `InteractionManager.Activate` check `quest.locationKey == interact.key` |
| NPC đứng yên dialogue | `NPCProfileSO.aiMode = SentisChat` |
| NPC đi lại theo lịch | `NPCProfileSO.aiMode = Movement + schedule` |
| Loading màn đen 3s | `SceneRouteSO.minLoadingScreenSeconds` |
| Text loading "Đang vào lớp học..." | `SceneRouteSO.loadingText` |
| MainMenu / Ending | `MainMenuScreen` / `EndingScreen` (đọc `ScoreConfigSO.grades`) |

---

## Backlinks
- [[index]]
- [[overview]]
- [[technical/datn-architecture]] (supersedes for new gameplay) — kiến trúc cũ của template farming
- [[systems/sentis-chat]] (depends on) — Phase A integration
- [[systems/movement-ai]] (depends on) — Phase B integration
- [[technical/unity-integration]] (see also) — cách load ONNX

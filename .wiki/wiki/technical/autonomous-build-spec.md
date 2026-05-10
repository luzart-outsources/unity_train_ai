---
title: Autonomous Build — Specs / Plan / Test Plan
category: technical
tags: [datn, autonomous, gdd, ingame, plan, test, setup-tool]
sources: [raw/gdd/gdd_inGame.txt, Assets/Luzart/, Assets/Scripts/]
created: 2026-05-11
updated: 2026-05-11
---

# Autonomous build — Game học kỳ quân đội (GDD InGame)

User đi ngủ giao toàn quyền. Doc này = SPECS + PLAN + TEST PLAN trước khi code, để sáng dậy có thể đối chiếu.

## 1. Phạm vi đêm nay

**Game mới**, KHÔNG động code DATN cũ trong `Assets/Scripts/` (giữ làm legacy reference). Code mới đi vào `Assets/Scripts/TrainAI/` namespace `TrainAI.*`. Tận dụng:

- **NinjaUI** (`Assets/Luzart/UIFramework/NinjaUI/`) cho mọi UI screen
- **TweenAnimation** (`Assets/Luzart/TweenAnimationPackage/`) cho show/hide animation prefab
- **NewBaseSelect** (`Assets/Luzart/NewBaseSelect/`) cho UI feedback
- **Attributes** (`Assets/Luzart/Attributes/`) cho SO Inspector UX
- **Player.prefab** sẵn (`Assets/Prefabs/Player.prefab`) — fallback nếu cần
- **PolygonFarm characters** (`Assets/Imported Asset/PolygonFarm/Prefabs/Characters/`) cho Player/NPC dummy
- **KenneyUI sprites** (`Assets/Imported Asset/UI/KenneyUI/`) cho UI button/panel
- **DOTween** (`Assets/Plugins/Demigiant/DOTween/`) — đã import
- **UniTask, Sentis 2.6.1, InputSystem, Timeline** đã có trong manifest

## 2. Kiến trúc tổng quan

Theo [[technical/gdd-ingame-tech-design]] (Phần I) + [[technical/gdd-ingame-luzart-integration]] (Phần II). Pattern chính:

- **SOLID + Strategy** (`IQuestRunner` per QuestType)
- **Service Locator nhẹ** (`GameServices` static class — không Zenject)
- **ScriptableObject data-driven** (mọi rule + content → asset)
- **Event-driven cross-system** (TimeManager broadcast tick, ScoreManager broadcast change)
- **NinjaUI lifecycle** thay viết UIManager mới
- **UniTask async** thay coroutine

### Namespace + folder

```
Assets/Scripts/TrainAI/
├── Configs/                   # SO type (class definition)
│   ├── Time/                  TimeConfigSO
│   ├── Quest/                 QuestType, QuestDefSO, DayPlanSO, DayCycleConfigSO
│   ├── Quiz/                  SubjectSO, QuizSetSO, QuestionSO
│   ├── Score/                 ScoreConfigSO, GradeThreshold
│   ├── NPC/                   NPCProfileSO, NPCScheduleSO, NPCAIMode
│   ├── Scene/                 SceneRouteSO, InteractableSO, InteractableKind
│   ├── Audio/                 AudioCueSO, AudioBankSO, AudioCueId
│   ├── UI/                    UITextSO
│   └── Database/              GameDatabaseSO (root)
│
├── Core/                      # Static services + bootstrap
│   ├── Bootstrap/             GameBootstrap, GameServices, GameDatabase
│   ├── Time/                  TimeManager, GameTime, ITimeTickListener, TimeTickDriver
│   ├── Save/                  SaveManager, GameSaveData, JsonSerializerEx
│   ├── Events/                GameEvents (static)
│   └── Util/                  Singleton<T> (chỉ cho NPCMovement)
│
├── Systems/
│   ├── Quest/                 QuestManager, QuestRuntimeState, QuestStatus,
│   │                          IQuestRunner + 7 runners (Exercise, Cleaning, Eat,
│   │                          StudyMorning, StudyAfternoon, Sleep, FreeRoam)
│   ├── Quiz/                  QuizManager, QuizRuntime
│   ├── Score/                 ScoreManager
│   ├── Audio/                 AudioManager (master + music + sfx + ui channels)
│   ├── Dialogue/              DialogueManager (Sentis stub fallback)
│   ├── Interaction/           InteractionManager
│   ├── Scene/                 SceneFlowService
│   └── NPC/                   NPCController (movement stub)
│
├── UI/
│   ├── Screens/               10 screen classes kế thừa UIBase<T>
│   ├── HUD/                   ClockHUD, MiniMapHUD, QuestHUD, ScoreHUD,
│   │                          AvatarHUD, InteractButton (SelectToggleImage),
│   │                          JoystickWidget
│   └── Components/            ConfirmData, QuizData, DialogueData, etc.
│
├── Player/                    PlayerController (top-down 3D + joystick)
├── World/                     InteractableTrigger
└── TrainAI.Runtime.asmdef     references: NinjaUI.Runtime, UniTask, Sentis,
                                            Luzart attributes (auto-include),
                                            DOTween (.dll auto), InputSystem

Assets/Scripts/TrainAI.Editor/
├── Setup/                     OneClickSetupTool, AssetScanner, PrefabBuilder,
│                              SceneBuilder, SOInstanceCreator
└── TrainAI.Editor.asmdef      references: TrainAI.Runtime, NinjaUI.Editor

Assets/Scripts/TrainAI.Tests/
├── EditMode/                  TimeManagerTests, ScoreManagerTests,
│                              QuestManagerTests, QuizRuntimeTests,
│                              DayCycleTests
└── TrainAI.Tests.asmdef       references: TrainAI.Runtime, NUnit

Assets/Configs/TrainAI/        SO INSTANCES (tạo bởi setup tool)
├── _GameDatabase.asset
├── Time/, Score/, DayPlans/, Quests/, Subjects/, QuizSets/,
│   NPCs/, Scenes/, Audio/, UIText/, UIRegistry.asset

Assets/Prefabs/TrainAI/        PREFABS (tạo bởi setup tool)
├── Player.prefab, NPC_Generic.prefab, Interactable.prefab
├── UI/                        10 screen prefabs

Assets/Scenes/TrainAI/         SCENES (tạo bởi setup tool)
├── _Boot.unity, Title.unity, World.unity,
├── Classroom.unity, Dormitory.unity, Cafeteria.unity
```

## 3. Hệ thống Sound mới

GDD không nói rõ sound nhưng user yêu cầu "Game có hệ thống Sound nữa để dùng". Thiết kế:

### `AudioCueSO`
1 cue = 1 mục audio config:
- `AudioClip clip`
- `enum Channel { Music, SFX, UI, Ambient }`
- `float volume = 1f`, `Vector2 pitchRange = (1,1)`
- `bool loop`
- `int priority` (cao hơn → ngắt cue cùng channel khi pool đầy)

### `AudioBankSO`
- `Dictionary<AudioCueId, AudioCueSO>` (qua list serialize)
- Lookup `Get(AudioCueId)`

### `enum AudioCueId`
```
UI_Click, UI_OpenPopup, UI_ClosePopup, UI_ToastInfo, UI_ToastWarning,
Score_AcademicGain, Score_DisciplinePenalty, Quest_Started, Quest_Completed,
Quest_Late, Quest_Missed, Day_Start, Day_End, Sleep,
Quiz_Correct, Quiz_Wrong, Quiz_Tick (countdown 5s cuối),
Music_MainMenu, Music_World, Music_Classroom, Music_Cutscene,
Ambient_World, Ambient_Dormitory,
Footstep_Default
```

### `AudioManager`
- Singleton, DontDestroyOnLoad
- 4 `AudioSource` channel: Music (1), SFX pool (8), UI pool (4), Ambient (1)
- Master / per-channel volume → `[ProgressBar]` slider trong settings
- API: `Play(AudioCueId)`, `PlayMusic(AudioCueId, fade)`, `Stop(channel, fade)`
- Lưu volume vào PlayerPrefs

### Tích hợp event game
- `GameEvents.OnQuestStarted` → `AudioManager.Play(Quest_Started)`
- `ScoreManager.OnDisciplineChanged(δ)` if δ<0 → `Play(Score_DisciplinePenalty)`
- `UIManager.OnShown` → `Play(UI_OpenPopup)` (chỉ khi lane Popup)
- `QuizManager.OnAnswer(correct)` → `Play(Quiz_Correct/Wrong)`

## 4. Mapping UIId cho game GDD

Mở rộng UIId enum NinjaUI (đã có sẵn `MainMenu = 1003`, `CreateCharacter = 1010`, `GameplayHud = 3000`, `Toast = 4000`):

| UIId mới | Số | Lane | Cache | Pausable |
|---|---|---|---|---|
| `OpeningCutscene` | 1011 | Screen | ReleaseOnClose | — |
| `Ending` | 1012 | Screen | ReleaseOnClose | — |
| `KickedOut` | 6 | System | KeepLoaded | — |
| `Quiz` | 2010 | Popup | PoolOnClose | ✓ |
| `Confirm` | 2011 | Popup | KeepLoaded | ✓ |
| `Dialogue` | 2012 | Popup | KeepLoaded | ✓ |

(Dùng `MainMenu = 1003`, `CreateCharacter = 1010`, `GameplayHud = 3000`, `Loading = 1`, `Toast = 4000` có sẵn)

UI tự thêm enum trong file `TrainAI.UIIdExtensions.cs` (chỉ dùng `enum UIIdGame { OpeningCutscene = 1011, ... }` cast về UIId cũng được, hoặc xài chung enum nếu được).

## 5. Test plan (Edit Mode — pure C# logic, không Unity API)

Dùng `Unity Test Framework` + NUnit. Tests chạy nhanh, không phụ thuộc Unity scene. Asmdef `TrainAI.Tests`.

### Test cases dự kiến

**`TimeManagerTests`**
- `Tick_AdvancesMinute_AfterSecondsPerMinute()`
- `Tick_DoesNotAdvance_WhenFrozen()`
- `NextDay_SkipsWeekend_WhenSkipWeekendTrue()` (T7 → T2)
- `NextDay_AdvancesNormally_WhenSkipWeekendFalse()`
- `JumpTo_FiresOnTick_Once()`
- `Tick_Wraps_HourTo24Resets()`
- `Tick_Wraps_DayIncrement_AfterMidnight()`

**`ScoreManagerTests`**
- `PenalizeDiscipline_Subtracts_AndClampsAtZero()`
- `AddAcademic_Adds_AndClampsAtMax()`
- `Discipline_AtKickOut_Threshold_FiresEvent()`
- `GradeFor_ReturnsExcellent_WhenScoreInRange()`
- `GradeFor_ReturnsAverage_WhenScoreBelowAllRanges()`

**`QuestManagerTests`**
- `OnTick_Late_Fires_AfterLateAfterMinutes()`
- `OnTick_Missed_AdvancesQuest_WhenPastDeadline()`
- `Complete_AddsAcademicScore_AndJumpsTime()`
- `Late_AppliesPenaltyOnce_NotEveryTick()`
- `CurrentQuest_Returns_FirstQuestAtDayStart()`

**`QuizRuntimeTests`**
- `Answer_Correct_IncrementsCorrectCount()`
- `Answer_Wrong_DoesNotIncrement()`
- `Tick_Timeout_AutoAdvances_AsWrong()`
- `IsFinished_True_AfterAllQuestions()`

**`DayCycleTests`**
- `DayCycleConfigSO_FindsPlanForDay()`
- `Reset_OnNextDay_SetsTimeToFirstQuestMinusOffset()`

**Coverage target**: ≥80% cho `TrainAI.Core` + `TrainAI.Systems`. Không test UI screens (Edit Mode không spawn được).

## 6. Setup Tool 1-click

Menu Unity: **Tools → TrainAI → 🚀 One-Click Full Setup**.

### Bước thực thi

1. **Validate environment** — check NinjaUI namespace, DOTween dll, UniTask, Luzart Attributes có không. Báo lỗi rõ nếu thiếu.
2. **Tạo folder structure** — toàn bộ trong `Assets/Configs/TrainAI/`, `Assets/Prefabs/TrainAI/`, `Assets/Scenes/TrainAI/`.
3. **Tạo Tags + Layers** — `Player`, `NPC`, `Interactable`, layer `Interactable`.
4. **Tìm asset** — scan project tìm:
   - Player prefab fallback (`PolygonFarm/SM_Chr_FarmBoy_01.prefab` nếu Player.prefab không vừa)
   - NPC prefab template (`SM_Chr_Farmer_Male_01.prefab`)
   - UI sprites (`Assets/Imported Asset/UI/KenneyUI/` cho button/panel/checkbox)
   - TMP font fallback (`Assets/TextMesh Pro/Fonts/LiberationSans.ttf`)
   - Audio clip fallback — báo nếu không có (Quyền tự thêm)
5. **Tạo SO instance assets** — chạy `SOInstanceCreator`:
   - 1 GameDatabase, 1 TimeConfig, 1 ScoreConfig, 1 DayCycleConfig
   - 7 DayPlans (demo theo overview "ưu tiên 7 ngày")
   - ~15 QuestDef (5 base × 3 variant ngày)
   - 2 SubjectSO (Lịch sử, Quốc phòng) + 4 QuizSet × 10 QuestionSO mẫu (placeholder questions)
   - 5 SceneRoute (Title, World, Classroom, Dormitory, Cafeteria)
   - ~10 InteractableSO (sân vận động, dọn vệ sinh, cửa lớp, cửa ký túc, cửa nhà ăn, NPC đại đội trưởng, ...)
   - 2 NPCProfile (đại đội trưởng, học viên di chuyển)
   - 1 AudioBank với 25+ AudioCueSO con (placeholder clip — Quyền gán sau)
   - 1 UITextSO catalog
6. **Tạo prefab** — `PrefabBuilder`:
   - `Player.prefab`: Capsule placeholder + CharacterController + camera follow + tag Player
   - `NPC_Generic.prefab`: tag NPC + collider + name plate WorldOverlay UI
   - `Interactable.prefab`: SphereCollider trigger 2m + tag Interactable + InteractableTrigger script
   - 10 UI prefabs với layout cơ bản dùng KenneyUI sprites + TweenAnimation OnEnable fade
7. **Tạo scene** — `SceneBuilder`:
   - `_Boot.unity`: GameBootstrap GO + UIRoot Canvas + 6 lane child + UIManager với UIRegistrySO ref
   - `Title.unity`: camera + EventSystem + auto-show MainMenu
   - `World.unity`: ground plane 50×50 + 6 interactable spot (sân vận động, dọn vệ sinh, cửa lớp/ký túc/nhà ăn, NPC đại đội trưởng) + Player spawn point
   - `Classroom.unity`: ground 20×20 + 4 desk có InteractableTrigger (Quiz)
   - `Dormitory.unity`: ground + 2 bed có InteractableTrigger (Sleep)
   - `Cafeteria.unity`: ground + 2 table có InteractableTrigger (Eat)
   - Add toàn bộ vào Build Settings
8. **Tạo UIRegistrySO entries** — gắn 10 UI prefab vào registry, gán registry vào UIManager.
9. **Tạo AudioCue children** — 25+ AudioCueSO sub-asset bên trong AudioBank.
10. **Wire database** — link GameDatabase root tới tất cả catalog SO + đặt vào GameBootstrap.
11. **Validate result** — chạy validator:
    - UIRegistry không duplicate, mỗi entry có AssetRef
    - DayPlan có ít nhất 1 quest
    - SceneRoute scene name khớp Build Settings
    - GameDatabase ref đầy đủ
    - Báo cáo dưới Console + dialog popup.

### Tool phụ
- **Tools → TrainAI → Validate Database** — chạy validate riêng (không setup lại).
- **Tools → TrainAI → Reset Setup** — xóa folder TrainAI/ trong Configs/Prefabs/Scenes (xác nhận).
- **Tools → TrainAI → Open Boot Scene** — quick open `_Boot.unity`.

## 7. Roadmap thực thi đêm nay

| # | Phase | Files | Commit |
|---|---|---|---|
| 0 | Specs/test plan doc | 1 wiki + log update | ✓ initial |
| 1 | Asmdef + folder skeleton | `TrainAI.Runtime.asmdef`, `TrainAI.Editor.asmdef`, `TrainAI.Tests.asmdef` | scaffold |
| 2 | SO classes (16 file) | `TimeConfigSO`...`GameDatabaseSO` | so-classes |
| 3 | Test infrastructure + tests | `TimeManagerTests`...`QuizRuntimeTests` (5 file) | test-specs |
| 4 | Core systems | `TimeManager`...`SaveManager` (~12 file) | core-systems |
| 5 | UI screens | 10 screen class + Data POCO | ui-screens |
| 6 | Player + NPC + Interactable | 4 file | player-npc |
| 7 | Editor setup tool | `OneClickSetupTool` + helpers (4-5 file) | setup-tool |
| 8 | Run tests (manual via doc) + final wiki + final commit | log update | final |

## 8. Risks + mitigation

| Risk | Mitigation |
|---|---|
| Code C# không compile vì tôi không có Unity | Bám sát exact API NinjaUI/UniTask đã đọc, syntactically careful, có sample InventoryUI để tham khảo |
| DOTween dll thiếu reference asmdef | Asmdef không cần ref dll trong Plugins/, sẽ resolve qua precompiled |
| Sentis chat chưa wire ONNX | Stub `IPhaseAChatService` với fallback rule-based responses (không block game) |
| Phase B movement chưa wire | NPC đứng yên + log warning. Quyền wire sau |
| Test framework không ref được vì TrainAI.Runtime chưa compile | Tests trong asmdef riêng `TrainAI.Tests` ref `TrainAI.Runtime` — nếu Runtime lỗi compile thì Tests cũng lỗi → user phải fix Runtime trước. Doc rõ trong setup tool console log |
| Sprite sai size | Setup tool dùng `RectTransform.sizeDelta` chuẩn 1920×1080 design + anchor preset |
| Scene save không atomic | Dùng `EditorSceneManager.SaveScene` API + try-catch |

## 9. Definition of Done (sáng dậy Quyền nên thấy)

- [ ] 1 commit per phase, ≥7 commit
- [ ] Compile clean (Quyền mở Unity → console không error)
- [ ] Menu `Tools → TrainAI → 🚀 One-Click Full Setup` xuất hiện
- [ ] Click → toàn bộ asset/prefab/scene tự tạo trong < 30s
- [ ] Mở `_Boot.unity` → Play → vào MainMenu → NewGame → CharacterCreate → World scene → joystick di chuyển + clock chạy
- [ ] Đi tới sân vận động → InteractButton sáng → click → UIConfirm hiện → OK → thời gian skip + score update
- [ ] EditMode tests xanh
- [ ] Wiki updated: log entry + spec doc + tech-design Phần I/II vẫn link nhau
- [ ] CLAUDE.md không cần đụng (dùng wiki)

## 10. Rollback plan

Nếu sáng dậy Quyền thấy lỗi, có thể:
- `git log --oneline` xem từng commit incremental
- `git revert <commit>` từng phase
- Hoặc `git reset --hard b2e26bf` về trước khi tôi đụng (snapshot user đã commit trước đó)

KHÔNG xóa code DATN cũ (`Assets/Scripts/`) — tách riêng namespace nên rollback an toàn.

---

## Backlinks
- [[index]]
- [[technical/gdd-ingame-tech-design]] — Phần I gameplay
- [[technical/gdd-ingame-luzart-integration]] — Phần II UI tích hợp
- [[overview]]

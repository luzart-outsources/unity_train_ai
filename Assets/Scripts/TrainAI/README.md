# TrainAI - Game hoc ky quan doi

Code mới (rebuild theo GDD `gdd_inGame.txt`). Khong dung code DATN cu (Stardew template).

## Cach chay (sang dau)

1. Mo Unity Editor.
2. Cho Unity import + compile xong.
3. Menu `Tools > TrainAI > 1-Click Full Setup` -> bam.
4. Doi ~30s -> bao "SETUP DONE". Click OK.
5. Menu `Tools > TrainAI > Open Boot Scene` -> mo `_Boot.unity`.
6. Press Play.

Flow demo:
- Title scene -> MainMenu auto show
- Bam "Bat dau moi" -> CharacterCreate -> nhap ten -> Confirm
- Vao World scene -> Player spawn, joystick + clock chay
- Di toi 1 Interactable (vd San van dong) -> InteractButton sang
- Bam Interact -> UIConfirm "Ban dang tap the duc" -> OK -> skip thoi gian, score update
- Toast "Don ngay" + nhac chuyen scene khi vao Lop hoc/Ky tuc xa/Nha an
- Lop hoc: Quiz 10 cau 15s/cau, mau xanh/do feedback
- Ngu -> save + next day

## Cau truc

```
Assets/Scripts/TrainAI/
  Configs/         16 ScriptableObject types
  Core/            Bootstrap, GameServices, GameEvents, Time
  Systems/         Quest, Quiz, Score, Audio, Save, SceneFlow, Interaction, Dialogue
  UI/              10 screen kế thừa NinjaUI UIBase + 7 HUD widget
  Player/          PlayerController + CameraFollow
  World/           InteractableTrigger + NPCController
  Tests/           NUnit Edit Mode tests (50+ cases)
  Editor/Setup/    1-Click setup tool (auto-create SO+prefab+scene)

Assets/Configs/TrainAI/    SO instance assets (tao boi setup tool)
Assets/Prefabs/TrainAI/    Prefab + UI prefab (tao boi setup tool)
Assets/Scenes/TrainAI/     6 scene (tao boi setup tool)
```

## Design pattern + SOLID

- **Strategy**: `IQuestRunner` + 7 runner (Exercise/Cleaning/Eat/StudyMorning/StudyAfternoon/Sleep/FreeRoam)
- **Service Locator**: `GameServices` static (thay Zenject, don gian)
- **Observer/Event hub**: `GameEvents` static, manager raise, UI subscribe
- **Data-driven**: moi rule trong ScriptableObject -> Inspector edit khong code
- **Single responsibility**: AudioManager khong biet quest, ScoreManager khong biet UI, etc.
- **Open/closed**: `IQuestRunner` register vao registry -> them quest type moi khong sua code cu
- **Async/await UniTask** thay coroutine -> cancellation token chuan

## Phu thuoc da co

- NinjaUI framework (`Assets/Luzart/UIFramework/`) - UI lifecycle async/lane
- NewBaseSelect (`Assets/Luzart/NewBaseSelect/`) - SelectToggleImage/SelectSwitchImage
- TweenAnimation (`Assets/Luzart/TweenAnimationPackage/`) - DOTween wrapper
- Luzart.Attributes - [InfoBox][Slider][ShowIf][Foldout][Button] cho SO
- UniTask (com.cysharp.unitask)
- DOTween (Assets/Plugins/Demigiant)
- TextMeshPro

## Tests

`Window > General > Test Runner > EditMode > Run All`. Co ~50 test case:
- TimeManagerTests: tick, freeze, weekend skip, jump, day wrap
- ScoreManagerTests: penalty, clamp, kicked-out event, grade lookup
- QuestManagerTests: state machine, late detection, complete advance
- QuizRuntimeTests: answer correct/wrong, timeout, finished
- DayCycleTests: lookup, NPC schedule
- SaveManagerTests: save/load JSON, restore state

## Audio

GDD khong noi ro audio. He thong san sang (`AudioManager`), AudioBank co 25+ AudioCueSO
voi clip = null. Quyen drag clip vao asset trong `Assets/Configs/TrainAI/Audio/AC_*.asset`.
Volume per channel save vao PlayerPrefs.

## AI Phase A (Sentis chat)

Stub fallback (`PhaseAChatStub`) tra ve fallback responses random tu NPCProfile.
Wire ONNX qua: implement `IPhaseAChatService` -> assign `GameServices.Dialogue = new DialogueManager(myImpl)`.

## AI Phase B (PPO movement)

`NPCController` co stub - log location moi gio. Wire bang cach extend
component voi navigate-to-anchor + load `soldier.onnx`.

## Mo rong

- Them ngay > 7: tao `Assets/Configs/TrainAI/DayPlans/Day08.asset`, drag vao `DayCycle.dayPlans`
- Them mon hoc: tao `SubjectSO` + `QuizSetSO` + 10 `QuestionSO` con
- Them quest type minigame: implement `IQuestRunner` + register trong `QuestRunnerProvider.InitDefault()`
- Doi audio: drag clip vao `AC_<Id>.asset`
- Doi UI sprite: open prefab trong `Assets/Prefabs/TrainAI/UI/` -> swap Image.sprite

## Memo

- Memory `feedback_autonomous_mode`: user da yeu cau autonomous -> da skip approval gates.
- Code khong dung emoji theo CLAUDE.md.

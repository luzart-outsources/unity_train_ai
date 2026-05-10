---
title: GDD InGame — Tích hợp Luzart (NinjaUI + Tween + Select + Attributes)
category: technical
tags: [datn, gdd, luzart, ninjaui, tween, dotween, unitask, scriptable-object]
sources: [Assets/Luzart/, raw/gdd/gdd_inGame.txt]
created: 2026-05-11
updated: 2026-05-11
---

# Tích hợp Luzart Framework cho game GDD

Phần II của technical design — tập trung vào **DELTA** so với [[technical/gdd-ingame-tech-design]] sau khi audit `Assets/Luzart/`. Project đã có sẵn:

- **NinjaUI** (`Assets/Luzart/UIFramework/NinjaUI/`) — async UI framework, lane-based, registry SO
- **TweenAnimation** (`Assets/Luzart/TweenAnimationPackage/`) — DOTween wrapper inspector-friendly
- **NewBaseSelect** (`Assets/Luzart/NewBaseSelect/`) — Toggle/Switch pattern cho UI state
- **Attributes** (`Assets/Luzart/Attributes/`) — `[Button] [ShowIf] [InfoBox] [ProgressBar] [Slider] [Dropdown] [Foldout] [ReadOnly]`

Doc này thay thế các section "UI state machine" + "AnimateShow/Hide" + "Inspector UX" trong [[technical/gdd-ingame-tech-design]]. Phần Quest / Time / Score / Quiz / Save **giữ nguyên** — không liên quan UI framework.

> [!info] Lợi ích kết hợp framework
> Bỏ được ~300 dòng UI plumbing tự viết. Thay vì design lại UIManager/UIScreenBase/UIScreenStack, chỉ cần ref `UIManager.Instance` + kế thừa `UIBase<TData>`. Save thời gian + đảm bảo Pause/Resume đúng (vốn là chỗ dễ lỗi của UI tự viết).

---

## Mục lục

1. [Stack có sẵn — checklist dependency](#stack-có-sẵn--checklist-dependency)
2. [Mapping 10 màn UI GDD → NinjaUI lane + UIId](#mapping-ui-gdd--ninjaui)
3. [UI screen template cho game GDD](#ui-screen-template-cho-game-gdd)
4. [Animation: TweenAnimation cho show/hide](#animation-tweenanimation-cho-showhide)
5. [Select pattern cho feedback UI](#select-pattern-cho-feedback-ui)
6. [Inspector UX cho ScriptableObject](#inspector-ux-cho-scriptableobject)
7. [Boot order — chỉnh lại theo NinjaUI](#boot-order--chỉnh-lại-theo-ninjaui)
8. [SceneFlow + Loading qua NinjaUI](#sceneflow--loading-qua-ninjaui)
9. [Pause/Resume tự động cho HUD khi popup đè](#pauseresume-tự-động-cho-hud)
10. [Toast cho điểm số + cảnh báo](#toast-cho-điểm-số--cảnh-báo)
11. [UniTask hoá Sentis chat + Quiz timer](#unitask-hoá-sentis-chat--quiz-timer)
12. [Anti-pattern khi dùng framework có sẵn](#anti-pattern-khi-dùng-framework-có-sẵn)

---

## Stack có sẵn — checklist dependency

Đã trong `Packages/manifest.json`:

| Package | Version | Mục đích cho game |
|---|---|---|
| `com.cysharp.unitask` | git Cysharp | Async show/hide UI, scene load, Sentis predict |
| `com.unity.ai.inference` | 2.6.1 | Sentis (Phase A chat + Phase B movement) |
| `com.unity.inputsystem` | 1.14.2 | Joystick + interact button |
| `com.unity.timeline` | 1.8.9 | **Cutscene màn mở đầu (GDD: "1 video")** |
| `www.nulltale.socollection` | git | Nested SO list trong DayPlan |
| `com.unity.modules.video` | 1.0.0 | VideoPlayer cho CutsceneScreen |

**DOTween** đã import (TweenAnimation `using DG.Tweening`) — Quyền không phải cài thêm.

**Luzart** ở `Assets/Luzart/`:
- `UIFramework/NinjaUI/` — namespace `Luzart`
- `TweenAnimationPackage/` — namespace `Luzart`
- `NewBaseSelect/` — namespace `Luzart.NewBase`
- `Attributes/` — namespace `Luzart`

> [!warning] Asmdef
> NinjaUI có asmdef `NinjaUI.Runtime` reference UniTask. Code game của Quyền nên cùng asmdef hoặc asmdef khác có ref `NinjaUI.Runtime` + `UniTask`. Tránh để code game ở Default → resolve type chậm + circular.

---

## Mapping UI GDD → NinjaUI

Refactor từ doc cũ ("UI state machine" — bỏ). Mỗi màn GDD = 1 entry trong `UIRegistrySO` + 1 enum value `UIId`:

| GDD screen | UIId (đề xuất) | Lane | Cache policy | DismissByEscape | Preload | AllowMulti | Pausable | Ghi chú |
|---|---|---|---|---|---|---|---|---|
| MainMenu | `UIId.MainMenu` | `Screen` | KeepLoaded | ✗ | ✓ | ✗ | — | Vào game đầu tiên + sau Ending |
| CutScene mở đầu | `UIId.OpeningCutscene` | `Screen` | ReleaseOnClose | ✗ | ✗ | ✗ | — | VideoPlayer + Timeline |
| CharacterCreate | `UIId.CharacterCreate` | `Screen` | ReleaseOnClose | ✗ | ✗ | ✗ | — | Show 1 lần đầu game |
| GameplayHUD | `UIId.GameplayHud` | `Hud` | KeepLoaded | ✗ | ✗ | ✗ | ✗ | Hiện trong World/Classroom/etc, không bao giờ pop |
| Loading | `UIId.Loading` | `System` | KeepLoaded | ✗ | **✓ bắt buộc** | ✗ | — | GDD: 3s + chuyển scene |
| Quiz | `UIId.Quiz` | `Popup` | PoolOnClose | ✗ | ✓ | ✗ | ✓ | Đè lên HUD, HUD pause tự động |
| Confirm | `UIId.Confirm` | `Popup` | KeepLoaded | ✓ | ✓ | ✗ | ✓ | Tái sử dụng cho Tập thể dục/Dọn vệ sinh/Ăn/Ngủ |
| Dialogue | `UIId.Dialogue` | `Popup` | KeepLoaded | ✓ | ✗ | ✗ | ✓ | NPC chat |
| Ending | `UIId.Ending` | `Screen` | ReleaseOnClose | ✗ | ✗ | ✗ | — | Cuối ngày 30 |
| KickedOut | `UIId.KickedOut` | `System` | KeepLoaded | ✗ | ✓ | ✗ | — | Discipline = 0 |
| (Toast) | — | `Toast` | (built-in) | — | ✓ | ✓ | — | "Trừ 5đ rèn luyện", "Đã lưu" |

> [!tip] Vì sao Confirm là `KeepLoaded`?
> GDD dùng UIConfirm cho ÍT NHẤT 5 case (Tập thể dục, Dọn vệ sinh, Ăn, Ngủ, Đuổi học). Mỗi case 1 `confirmText` khác — cùng prefab, khác data. Giữ load 1 lần, show nhiều lần với data khác nhau qua `UIBase<ConfirmData>`.

### `UIId` enum (theo convention NinjaUI: số tăng theo lane)

```csharp
namespace TrainAI
{
    public enum UIId
    {
        None = 0,
        // Screen lane: 1000-1999
        MainMenu          = 1000,
        OpeningCutscene   = 1010,
        CharacterCreate   = 1020,
        Ending            = 1030,
        // Hud lane: 2000-2999
        GameplayHud       = 2000,
        // Popup lane: 3000-3999
        Quiz              = 3000,
        Confirm           = 3010,
        Dialogue          = 3020,
        // System lane: 4000-4999
        Loading           = 4000,
        KickedOut         = 4010,
    }
}
```

---

## UI screen template cho game GDD

Pattern cho mỗi screen = 1 class C# + 1 prefab + 1 entry registry. Ví dụ `ConfirmScreen`:

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    // Data truyền vào mỗi lần Show
    public class ConfirmData
    {
        public string Message;       // GDD: "Bạn đang tập thể dục"
        public string OkLabel = "OK";
        public UniTaskCompletionSource<bool> ResultTcs = new();
    }

    public class ConfirmScreen : UIBase<ConfirmData>
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI txtMessage;
        [SerializeField] private Button btnOk;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            btnOk.onClick.AddListener(OnOkClicked);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(ConfirmData data, CancellationToken ct)
        {
            txtMessage.text = data.Message;
            btnOk.GetComponentInChildren<TextMeshProUGUI>().text = data.OkLabel;
            return UniTask.CompletedTask;
        }

        private void OnOkClicked()
        {
            Data.ResultTcs.TrySetResult(true);
            UIManager.Instance.HideAsync(this).Forget();
        }
    }
}
```

Caller (vd `ExerciseRunner` cho quest tập thể dục):

```csharp
public class ExerciseRunner : IQuestRunner
{
    public async UniTask<int> RunAsync(QuestDefSO quest, CancellationToken ct)
    {
        var data = new ConfirmData { Message = quest.confirmText, OkLabel = quest.okButtonText };
        await UIManager.Instance.ShowAsync<ConfirmScreen>(UIId.Confirm,
            new UIContext(data), ct: ct);
        await data.ResultTcs.Task;          // chờ user bấm OK
        return 0;                            // exercise/cleaning không cộng điểm học tập
    }
}
```

> [!tip] Không cần coroutine, không cần singleton riêng
> Toàn bộ flow Quest → Confirm → wait OK → return → QuestManager.Complete xài `await` thẳng. Cancellation token propagate từ `MonoBehaviour.GetCancellationTokenOnDestroy()` cho tới UI → user destroy → mọi `await` tự huỷ.

### `QuizScreen` với UniTask timer

```csharp
public class QuizData
{
    public QuizSetSO Set;
    public UniTaskCompletionSource<int> ResultTcs = new();   // số câu đúng
}

public class QuizScreen : UIBase<QuizData>
{
    [SerializeField] private TextMeshProUGUI txtStem;
    [SerializeField] private Button[] btnAnswers = new Button[4];
    [SerializeField] private SelectSwitchImage answerFeedback;   // Luzart Select
    [SerializeField] private Image imgCountdown;                 // fillAmount 1→0

    private int currentIdx;
    private int correctCount;
    private float remaining;

    protected override async UniTask OnShownAsync(QuizData data, CancellationToken ct)
    {
        for (currentIdx = 0; currentIdx < data.Set.questions.Count; currentIdx++)
        {
            BindQuestion(data.Set.questions[currentIdx]);
            int chosen = await WaitAnswer(data.Set.secondsPerQuestion, ct);
            ShowFeedback(chosen, data.Set.questions[currentIdx].correctIndex);
            await UniTask.Delay(500, cancellationToken: ct);
        }
        data.ResultTcs.TrySetResult(correctCount);
        UIManager.Instance.HideAsync(this).Forget();
    }

    private async UniTask<int> WaitAnswer(float seconds, CancellationToken ct)
    {
        var tcs = new UniTaskCompletionSource<int>();
        for (int i = 0; i < 4; i++) { int idx = i; btnAnswers[i].onClick.AddListener(() => tcs.TrySetResult(idx)); }
        remaining = seconds;
        while (remaining > 0 && tcs.Task.Status == UniTaskStatus.Pending)
        {
            remaining -= Time.deltaTime;
            imgCountdown.fillAmount = remaining / seconds;
            await UniTask.Yield(ct);
        }
        foreach (var b in btnAnswers) b.onClick.RemoveAllListeners();
        return tcs.Task.Status == UniTaskStatus.Pending ? -1 : tcs.Task.GetAwaiter().GetResult();
    }

    private void ShowFeedback(int chosen, int correct)
    {
        if (chosen == correct) correctCount++;
        // Index 0 = neutral, 1 = correct (green), 2 = wrong (red)
        answerFeedback.Select(chosen == correct ? 1 : 2);
    }
}
```

> [!info] `await UniTask.Yield(ct)` thay Update
> Toàn bộ countdown chạy bên trong `OnShownAsync` async — không cần `MonoBehaviour.Update`. Cancel token destroy → loop tự dừng. Đây là pattern UniTask thay state machine + IEnumerator coroutine cũ kỹ.

---

## Animation: TweenAnimation cho show/hide

Mỗi UI prefab gắn 2 component cho fade-in / scale pop:

### Cách 1 — `TweenAnimation` đơn (1 hiệu ứng)

Trên prefab `ConfirmScreen.prefab`:
1. Add component `Luzart.TweenAnimation` → Type `FadeByCanvasGroup` → Duration 0.25s, Easing `OutQuad`, From 0, To 1.
2. Add component `Luzart.TweenAnimationCaller` → ref TweenAnimation trên cùng GO → TypeShow `OnEnable`.

→ Khi NinjaUI activate prefab (Show), `OnEnable` trigger TweenAnimationCaller chạy fade-in tự động. Quyền **không viết code animation** trong `ConfirmScreen.cs`.

### Cách 2 — `SequenceTweenAnimation` (combo)

Cho `EndingScreen` muốn fade in + scale logo + slide title:

```
EndingScreen (root)
├── TweenAnimationCaller (typeShow = OnEnable, ref = SequenceTweenAnimation)
├── SequenceTweenAnimation
│   └── Sequences:
│       [0] Append → TweenAnim_FadeBackground (Fade 0→1, 0.3s)
│       [1] Append → TweenAnim_ScaleLogo      (Scale 0→1, 0.4s, OutBack)
│       [2] Join   → TweenAnim_SlideTitle     (MoveLocal Y -200→0, 0.4s)
│       [3] Insert(0.7) → TweenAnim_FadeButtons (Fade buttons CG 0→1, 0.3s)
```

Tween children là các GameObject con với component `TweenAnimation` riêng — Inspector configurable, Quyền tweak duration/ease không build lại code.

### Hide animation — override `AnimateHideAsync`

Nếu cần animation ngược khi đóng (UIBase mặc định instant):

```csharp
public class ConfirmScreen : UIBase<ConfirmData>
{
    [SerializeField] private TweenAnimationBase hideAnim;

    protected override async UniTask OnBeforeHideAsync(UIHideReason r, CancellationToken ct)
    {
        if (hideAnim != null)
        {
            var t = hideAnim.Show();
            await t.ToUniTask(cancellationToken: ct);   // chờ tween xong
        }
    }
}
```

> [!tip] Không trộn DOTween + UniTask trực tiếp
> `t.ToUniTask()` là extension của UniTask convert `Tween → UniTask` đúng cách. Nếu Quyền tự `await new WaitForSeconds(...)` sẽ không huỷ được khi cancel.

---

## Select pattern cho feedback UI

`Luzart.NewBase.SelectToggleX` / `SelectSwitchX` thay if-else gắn động UI. Use cases trong GDD:

| GDD requirement | Component | Mode |
|---|---|---|
| Nút interact "sáng / tối phụ thuộc hướng đứng" | `SelectToggleImage` | bool — sprite sáng/tối |
| Joystick visible khi không phải popup | `SelectToggleGameObject` | bool — show/hide |
| MainMenu tab (NewGame/Tiếp tục/Thoát) selected | `SelectSwitchImage` | int — 0/1/2 sprite highlight |
| Quiz answer feedback (đúng/sai/neutral) | `SelectSwitchImage` | int — 0=neutral, 1=green, 2=red |
| Score grade trong Ending (XS/Tốt/TB) | `SelectSwitchTMP_Text` | int — color + text style |
| MiniMap day icon (T2/T3/T4/T5/T6) | `SelectSwitchGameObject` | int — show 1 child trong group |
| HUD avatar mood (vui/bình thường/buồn theo discipline) | `SelectSwitchImage` | int — 3 sprite |

### Ví dụ — `InteractButton` của GDD

```csharp
[RequireComponent(typeof(SelectToggleImage))]
public class InteractButton : MonoBehaviour
{
    [SerializeField] private SelectToggleImage state;   // 0=tối, 1=sáng
    [SerializeField] private Button btn;

    private void Awake()
    {
        InteractionManager.OnInteractableInRange += _ => state.Select(true);
        InteractionManager.OnInteractableOutOfRange += () => state.Select(false);
        btn.onClick.AddListener(() => InteractionManager.Instance.Activate());
    }
}
```

Designer chỉ kéo `imSelect` (ảnh button), `spSelect` (sprite sáng), `spUnSelect` (sprite tối) vào component — không sửa code.

### Ví dụ — Quiz feedback

```csharp
// answerFeedback: SelectSwitchImage với
//   imSelect: [imgAnswer0, imgAnswer1, imgAnswer2, imgAnswer3]
//   spSelect: [neutral, green, red] x 4   (4 group, 3 sprite/group)
private void ShowFeedback(int chosen, int correct)
{
    int feedbackIdx = chosen == correct ? 1 : 2;   // green or red
    answerFeedback.Select(feedbackIdx);
}
```

> [!info] Vì sao SelectSwitch?
> Tránh code lặp `if (i == correct) img.sprite = green; else img.sprite = red;` — designer sửa sprite trong prefab, không build code. Đặc biệt với quiz có 10 buổi × 10 câu = 100 lần show/hide, code clean rất quan trọng.

---

## Inspector UX cho ScriptableObject

Áp dụng Luzart Attributes vào SO định nghĩa ở [[technical/gdd-ingame-tech-design]]. Refactor `QuestDefSO`:

```csharp
using Luzart;
using UnityEngine;

[CreateAssetMenu(menuName = "TrainAI/Quest/Quest Def")]
public class QuestDefSO : ScriptableObject
{
    [Foldout("Identity")]
    [InfoBox("ID phải UNIQUE trong toàn project. Convention: Q_<Type>_<HourMinute>")]
    public string id;

    [Foldout("Identity")]
    public string title;

    [Foldout("Identity")]
    [TextArea] public string descriptionTemplate;

    [Foldout("Type & Time")]
    public QuestType type;

    [Foldout("Type & Time")]
    [Slider(0, 23)] public int startHour;

    [Foldout("Type & Time")]
    [Slider(0, 59)] public int startMinute;

    [Foldout("Type & Time")]
    [InfoBox("Sau X phút game không bắt đầu sẽ bị Late (-5đ rèn luyện)", InfoBoxType.Warning)]
    [Slider(0, 23)] public int deadlineHour;

    [Foldout("Type & Time")]
    [Slider(0, 59)] public int deadlineMinute;

    [Foldout("Type & Time")]
    [Slider(0, 23)] public int skipToHour;

    [Foldout("Type & Time")]
    [Slider(0, 59)] public int skipToMinute;

    [Foldout("Location")]
    public SceneRouteSO targetScene;

    [Foldout("Location")]
    public string interactableLocationKey;

    [Foldout("UI")]
    [HideIf("type", QuestType.StudyMorning)]
    [HideIf("type", QuestType.StudyAfternoon)]
    public string confirmText;

    [Foldout("UI")]
    [HideIf("type", QuestType.StudyMorning)]
    [HideIf("type", QuestType.StudyAfternoon)]
    public string okButtonText = "OK";

    [Foldout("Quiz (chỉ cho Study)")]
    [ShowIfAny("type", QuestType.StudyMorning, "type", QuestType.StudyAfternoon)]
    public SubjectSO subject;

    [Foldout("Quiz (chỉ cho Study)")]
    [ShowIfAny("type", QuestType.StudyMorning, "type", QuestType.StudyAfternoon)]
    public QuizSetSO quizSet;

    [Foldout("Score impact")]
    [InfoBox("Theo GDD: late hoặc miss đều trừ 5đ rèn luyện")]
    [Slider(0, 20)] public int penaltyOnLate = 5;

    [Foldout("Score impact")]
    [Slider(0, 20)] public int penaltyOnMissed = 5;

    [Button("Validate Time Window")]
    private void ValidateTimes()
    {
        if (startHour * 60 + startMinute > deadlineHour * 60 + deadlineMinute)
            Debug.LogError($"[{name}] startTime > deadlineTime");
        if (deadlineHour * 60 + deadlineMinute > skipToHour * 60 + skipToMinute)
            Debug.LogWarning($"[{name}] skipToTime nên >= deadlineTime");
    }
}
```

→ Inspector của Quyền:
- **Foldout** gom 5 group rõ ràng
- **InfoBox** giải thích rule GDD ngay tại field
- **ShowIf/HideIf** ẩn `quizSet` cho quest không phải Study
- **Slider** cho hour/minute thay text ô vuông
- **[Button]** chạy validate ngay trong Inspector, click 1 phát biết quest có lỗi

### `ScoreConfigSO` với ProgressBar

```csharp
[CreateAssetMenu(menuName = "TrainAI/Score/Config")]
public class ScoreConfigSO : ScriptableObject
{
    [Slider(0, 100)] public int startingDiscipline = 100;
    [Slider(0, 480)] public int startingAcademic = 0;

    [Header("Maximum")]
    [InfoBox("GDD: 100đ rèn luyện max, 480đ học tập max")]
    [ReadOnly] public int maxDiscipline = 100;
    [ReadOnly] public int maxAcademic = 480;

    [Header("Threshold display")]
    [ProgressBar("Excellent threshold", 0, 480)] public int excellentAcademic = 432;
    [ProgressBar("Good threshold", 0, 480)] public int goodAcademic = 288;
    // ...
}
```

### `TimeConfigSO` với Dropdown

```csharp
[CreateAssetMenu(menuName = "TrainAI/Time/Config")]
public class TimeConfigSO : ScriptableObject
{
    [DropdownNamed("60|1h = 1p (test nhanh)", "180|1h = 3p (GDD chuẩn)", "300|1h = 5p (chậm)")]
    public float secondsPerGameHour = 180f;

    [Slider(1, 60)] public int lateAfterMinutes = 15;

    [InfoBox("Nếu skipWeekend = true: ngày 6 (T7) → ngày 8 (T2 tuần sau)")]
    public bool skipWeekend = true;
}
```

> [!tip] Vì sao dropdown 3 preset thay slider seconds?
> 3 giá trị test/GDD/chậm thấy ý nghĩa hơn slider 30..600. Mỗi giá trị có label tiếng Việt → Quyền nhớ ngay "180 = chuẩn".

---

## Boot order — chỉnh lại theo NinjaUI

Doc cũ đề xuất `ServiceLocator + UIManager singleton tự viết`. Bỏ — NinjaUI có sẵn `UIManager.Instance`. Boot order mới:

```csharp
public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private GameDatabaseSO database;
    [SerializeField] private UIRegistrySO uiRegistry;     // Luzart NinjaUI registry
    [SerializeField] private string firstScene = "Title";

    private async void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // 1. Database (SO catalog)
        GameDatabase.Init(database);
        // 2. Core gameplay services (không phải UI)
        GameServices.Time     = new TimeManager(database.timeConfig);
        GameServices.Score    = new ScoreManager(database.scoreConfig);
        GameServices.Quest    = new QuestManager(database.dayCycle);
        GameServices.Save     = new SaveManager();
        GameServices.Dialogue = new DialogueManager(database.npcs);
        // 3. NinjaUI đã có UIManager.Instance singleton — chỉ assign registry nếu chưa
        // (UIManager prefab đã trong scene _Boot, registry assign trong Inspector)
        // 4. Preload critical UI (Loading, KickedOut)
        await UIManager.Instance.PreloadAsync(UIId.Loading);
        await UIManager.Instance.PreloadAsync(UIId.KickedOut);
        // 5. Load Title
        await SceneManager.LoadSceneAsync(firstScene).ToUniTask();
    }
}
```

Scene `_Boot.unity` hierarchy:
```
_Boot
├── [GameBootstrap]                      ← script ở trên
└── UIRoot (Canvas, Screen Space-Overlay)
    ├── 0_WorldOverlay
    ├── 1_Screen
    ├── 2_Hud
    ├── 3_Popup
    ├── 4_System
    ├── 5_Toast
    ├── UIBlockerOverlay
    ├── [UIManager] (Luzart, ref UIRegistry, UIInputRouter, UIBlockService)
    └── EventSystem
```

→ Khớp với hướng dẫn `Assets/Luzart/UIFramework/docs/03-ui-manager-usage-guide.md` mục 0.3.

> [!warning] `GameServices` thay `ServiceLocator` doc cũ
> Doc cũ đề xuất `ServiceLocator.Register/Get`. Đơn giản hoá: 1 static class `GameServices` với property cho từng service. Vẫn dependency-inject được qua override trong test. Bớt 1 abstraction layer, ít magic hơn.

---

## SceneFlow + Loading qua NinjaUI

Refactor `SceneFlowManager` của doc cũ:

```csharp
public class SceneFlowService
{
    private readonly TimeManager _time;
    public SceneFlowService(TimeManager time) { _time = time; }

    public async UniTask TravelAsync(SceneRouteSO route, CancellationToken ct = default)
    {
        var loadingData = new LoadingData { Text = route.loadingText };
        var loading = await UIManager.Instance.ShowAsync<LoadingScreen>(
            UIId.Loading, new UIContext(loadingData), ct: ct);
        // GDD: 3s tối thiểu + chờ scene load
        var minWait = UniTask.Delay(TimeSpan.FromSeconds(route.minLoadingScreenSeconds), cancellationToken: ct);
        var sceneLoad = SceneManager.LoadSceneAsync(route.sceneName).ToUniTask(cancellationToken: ct);
        await UniTask.WhenAll(minWait, sceneLoad);
        if (route.freezeTimeWhileLoaded) _time.Freeze();
        await UIManager.Instance.HideAsync(loading, ct: ct);
    }
}
```

```csharp
public class LoadingData { public string Text; }

public class LoadingScreen : UIBase<LoadingData>
{
    [SerializeField] private TextMeshProUGUI txt;
    protected override UniTask OnBeforeShowAsync(LoadingData data, CancellationToken ct)
    {
        txt.text = data.Text;        // GDD: "Đang vào lớp học..."
        return UniTask.CompletedTask;
    }
}
```

> [!tip] Pattern `WhenAll(min, real)`
> GDD yêu cầu Loading hiện ÍT NHẤT 3s + scene load xong. `UniTask.WhenAll(minWait, sceneLoad)` chờ cả 2 — đúng spec, không phải sleep cứng. Nếu scene load 0.5s → vẫn chờ đủ 3s. Nếu load 5s → chỉ chờ 5s không phải 8s.

---

## Pause/Resume tự động cho HUD

GDD: "Tới nơi làm nhiệm vụ, ở trong Scene nhiệm vụ rồi thì đồng hồ sẽ đóng băng thời gian mà không quay nữa." Thực tế cũng cần freeze HUD timer khi UIQuiz/UIConfirm popup đè lên.

NinjaUI handle CHO BẠN qua `PausableWhenOverlaid = true`:

```csharp
public class GameplayHud : UIBase
{
    [SerializeField] private TextMeshProUGUI txtClock;
    private bool _subscribed;

    protected override UniTask OnShownAsync(UIContext ctx, CancellationToken ct)
    {
        Subscribe();
        return UniTask.CompletedTask;
    }
    public override UniTask OnPauseAsync(CancellationToken ct)
    {
        Unsubscribe();              // popup khác đè lên → ngừng update clock
        return UniTask.CompletedTask;
    }
    public override UniTask OnResumeAsync(CancellationToken ct)
    {
        Subscribe();                // popup đóng → resume update
        return UniTask.CompletedTask;
    }
    private void Subscribe()
    {
        if (_subscribed) return;
        GameServices.Time.OnTick += OnTimeTick;
        _subscribed = true;
    }
    private void Unsubscribe()
    {
        if (!_subscribed) return;
        GameServices.Time.OnTick -= OnTimeTick;
        _subscribed = false;
    }
    private void OnTimeTick(GameTime t) => txtClock.text = $"{t.hour:D2}:{t.minute:D2}";
}
```

> [!info] Khác gì với `TimeManager.Freeze()`?
> `TimeManager.Freeze()` dừng đồng hồ ở **system level** (theo GDD: scene quest đóng băng). `OnPauseAsync` chỉ unsubscribe HUD ở **UI level** — đồng hồ vẫn chạy nhưng HUD không refresh. Hai cơ chế độc lập, dùng đúng case:
> - Vào Classroom → `TimeManager.Freeze()` (gameplay đóng băng)
> - UIConfirm đè lên World → `OnPauseAsync` HUD (gameplay vẫn chạy underneath, chỉ dừng update HUD)

---

## Toast cho điểm số + cảnh báo

NinjaUI có `ShowToastAsync` built-in. Map vào event game:

```csharp
public class ToastBindings : MonoBehaviour
{
    private void OnEnable()
    {
        GameServices.Score.OnDisciplineChanged += (newVal, delta) =>
        {
            if (delta < 0)
                UIManager.Instance.ShowToastAsync($"−{-delta} điểm rèn luyện", ToastStyle.Warning, 2f).Forget();
        };
        GameServices.Score.OnAcademicChanged += (newVal, delta) =>
        {
            if (delta > 0)
                UIManager.Instance.ShowToastAsync($"+{delta} điểm học tập", ToastStyle.Success, 2f).Forget();
        };
        GameServices.Save.OnSaved += () =>
            UIManager.Instance.ShowToastAsync("Đã lưu", ToastStyle.Info, 1.5f).Forget();
    }
}
```

→ Không cần screen riêng, không cần queue tự viết. Toast tự fade in/out, tự stack nhiều cái.

---

## UniTask hoá Sentis chat + Quiz timer

Doc cũ dùng `Task<>` cho `PhaseAChatService.PredictIntent`. Đổi sang `UniTask<>` để consistent với NinjaUI + tránh allocation:

```csharp
public class PhaseAChatService
{
    public UniTask<(string intent, float confidence)> PredictIntentAsync(string text, CancellationToken ct)
    {
        // Sentis Worker.Schedule là sync → wrap trong UniTask
        return UniTask.RunOnThreadPool(() =>
        {
            // ... preprocess + worker.Schedule + worker.PeekOutput
            return (intentLabel, confidence);
        }, cancellationToken: ct);
    }
}
```

Caller (`DialogueScreen`):

```csharp
public class DialogueScreen : UIBase<DialogueData>
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button btnSend;
    [SerializeField] private Transform bubbleRoot;

    private async void OnSendClicked()
    {
        var text = inputField.text;
        inputField.text = "";
        AddBubble(text, isPlayer: true);
        var ct = this.GetCancellationTokenOnDestroy();
        var (intent, conf) = await GameServices.Chat.PredictIntentAsync(text, ct);
        var slots = GameServices.Chat.ExtractEntities(text);
        var response = GameServices.Chat.FormatResponse(intent, slots, Data.Npc, GameServices.Player);
        AddBubble(response, isPlayer: false);
    }
}
```

> [!info] `GetCancellationTokenOnDestroy` là vàng
> NPC dialogue mở → Sentis predict 200ms → trong khoảng đó user pop dialogue → cancellation propagate → không vẽ bubble vào UI đã destroy → không lỗi `MissingReferenceException`. Mọi `await` trong UIBase nên dùng token này (hoặc token từ `OnBeforeShowAsync`).

---

## Anti-pattern khi dùng framework có sẵn

Cập nhật từ doc cũ — chỉ liệt kê thêm cái LIÊN QUAN Luzart:

| Anti-pattern | Tại sao tránh | Thay bằng |
|---|---|---|
| Tự viết `UIManager` mới | Trùng NinjaUI, double maintenance | `UIManager.Instance` (Luzart) |
| Subscribe game event trong `OnCreateAsync` | OnCreate chỉ gọi 1 lần — không unsubscribe được khi hide | Subscribe trong `OnBeforeShowAsync` / `OnShownAsync`, unsubscribe trong `OnHiddenAsync` |
| `Resources.Load` prefab UI | Bypass registry → mất ref count, mất pause/resume | Add entry vào `UIRegistrySO`, gọi `ShowAsync<T>(UIId.X)` |
| `Destroy(uiView.gameObject)` thủ công | Phá flow CachePolicy | `UIManager.Instance.HideAsync(handle)` |
| Coroutine cho UI animation | Không cancel khi destroy → MissingRef | `TweenAnimation` + `tween.ToUniTask(ct)` |
| `if (sprite == green) ... else ...` rải khắp | Code lặp, designer phải sửa code | `SelectSwitchImage` + sprite array |
| Hardcode `ToastStyle.Info` magic string | Không gợi ý IDE | Dùng enum NinjaUI có sẵn |
| Inspector ScriptableObject thấy hết field rỗng | Quyền hoang mang field nào cần điền cho type nào | `[ShowIf]` `[HideIf]` ẩn field không liên quan |
| Comment `// TODO: validate timing` | Không ai chạy | `[Button("Validate")]` Quyền click 1 phát |
| Tự viết popup queue cho daily reward (nếu có) | UIPopupQueue đã sẵn | `EnqueuePopupAsync(id, ctx, priority)` |
| Tự viết input blocker khi load | Bug khi nhiều chỗ block đồng thời | `using (UIManager.Instance.PushBlock("loading_quiz")) { ... }` |

---

## Cập nhật Mapping GDD → Tech (full)

Bổ sung cột UIId/Lane vào bảng cuối doc cũ:

| GDD requirement | SO chịu trách nhiệm | UIId | Lane | Component Luzart hỗ trợ |
|---|---|---|---|---|
| MainMenu (NewGame/Tiếp tục/Thoát) | `MainMenuConfigSO` | `MainMenu` | Screen | `SelectSwitchImage` (highlight) |
| CutScene 1 video | `CutsceneSO` (Timeline) | `OpeningCutscene` | Screen | `TweenAnimation` skip-fade |
| CharacterCreate InputField | — | `CharacterCreate` | Screen | — |
| GameplayHUD joystick + clock + minimap + interact + quest + score + avatar | — | `GameplayHud` | Hud | `SelectToggleImage` (interact btn), `SelectSwitchImage` (avatar mood) |
| UILoading màn đen + 3s + text | `SceneRouteSO.minLoadingScreenSeconds + loadingText` | `Loading` | System (Preload!) | `TweenAnimation` fade |
| UIQuiz 15s + 4 đáp + xanh/đỏ | `QuizSetSO.secondsPerQuestion` | `Quiz` | Popup (Pausable) | `SelectSwitchImage` (feedback) |
| UIConfirm "bạn đang ..." | `QuestDefSO.confirmText` | `Confirm` | Popup (Pausable, KeepLoaded) | — |
| UIDialogueNPC InputField + Send + bubble | `NPCProfileSO` | `Dialogue` | Popup (Pausable) | `TweenAnimation` bubble pop |
| UIEnding xếp hạng XS/Tốt/TB | `ScoreConfigSO.disciplineGrades / academicGrades` | `Ending` | Screen | `SelectSwitchTMP_Text` (rank label color) |
| Đuổi học khi rèn luyện ≤ 0 | `ScoreConfigSO.kickOutThreshold` | `KickedOut` | System | — |
| Toast trừ điểm / lưu game | `ScoreConfigSO.penalty*` | — (built-in) | Toast | `UIManager.ShowToastAsync` |
| Đóng băng đồng hồ trong scene quest | `SceneRouteSO.freezeTimeWhileLoaded` | — | — | `TimeManager.Freeze()` + `OnPauseAsync` HUD |
| Pause khi popup đè lên | — | — | — | `PausableWhenOverlaid=true` + `OnPauseAsync/OnResumeAsync` |
| Cancel async khi destroy | — | — | — | `this.GetCancellationTokenOnDestroy()` |

---

## Roadmap chỉnh lại (vì đã có framework)

So với roadmap 7 ngày trong doc cũ, **giảm ~1.5 ngày** vì không phải code UI plumbing:

| Ngày | Việc | Output |
|---|---|---|
| 1 | Boot scene + UIManager prefab + UIRegistrySO + GameServices + TimeManager | Đồng hồ chạy, UIManager log "Initialized" |
| 2 | Toàn bộ SO class với Luzart attributes + 1 SO instance mỗi loại | Inspector Foldout/InfoBox đẹp |
| 3 | MainMenuScreen + CharacterCreateScreen + GameplayHud (Hud lane) + Joystick | Vào game, di chuyển, clock chạy |
| 4 | QuestManager + 3 runner (Exercise/Cleaning/Sleep) + InteractionManager + InteractButton (`SelectToggleImage`) | 1 ngày demo: 3 quest |
| 5 | QuizScreen với UniTask timer + ScoreManager + Toast trừ/cộng điểm | Buổi học làm quiz |
| 6 | SceneFlowService + LoadingScreen + ConfirmScreen (KeepLoaded) | Chuyển scene mượt |
| 7 | DialogueScreen + Phase A v2 service (UniTask) + EndingScreen với `SelectSwitchTMP_Text` | NPC chat, ending xếp hạng |
| Bonus | NPCMovementBrain Phase B + Save/Load + 7 day plans full + Tween polish | Demo end-to-end 7 ngày |

---

## Backlinks
- [[index]]
- [[technical/gdd-ingame-tech-design]] — Phần I, kiến trúc gameplay (Quest/Time/Score/Quiz/Save) — doc này bổ sung phần UI
- [[systems/ninjaui-framework]] — chi tiết UIManager + lane stack + lifecycle
- [[decisions/remove-addressables-add-unitask]] — quyết định DirectPrefab + UniTask cho NinjaUI
- [[technical/datn-architecture]] — kiến trúc cũ template farming (legacy, KHÔNG dùng cho game GDD mới)
- [[systems/sentis-chat]] — Phase A integration via UniTask

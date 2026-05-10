# Luzart TweenAnimation Package

Tween/animation framework cho Unity dựa trên DOTween, kèm bộ attribute + property drawer tự build (ShowIf/HideIf/EnableIf/DisableIf/ReadOnly/Dropdown/Slider/ProgressBar/InfoBox/Button/ShowInInspector) và một editor utility `ResetParentKeepScale`.

## Yêu cầu

- Unity 2021.3+ (dùng C# 9 features: pattern matching, switch expression, target-typed `new`)
- **DOTween** (hoặc DOTween Pro). Sử dụng `DG.Tweening` namespace, bao gồm `DOText`, `DOFade`, `DOVirtual`, v.v.
- **TextMeshPro** (bắt buộc — có trong Unity Package Manager)

Các dependency khác của bản gốc (ví dụ `LocalizedText`) **đã được gỡ** để code chạy standalone.

## Cấu trúc

```
TweenAnimationPackage/
├── Runtime/
│   ├── TweenAnimationBase.cs          # Abstract base + ITweenAnimation interface
│   ├── TweenAnimation.cs              # Component chính + factory + settings
│   ├── TweenAnimationWorker.cs        # Strategy workers (Move/Scale/Fade/Float/Text/UnityEvent/...)
│   ├── SequenceTweenAnimation.cs      # Composer Append/Join/Insert
│   ├── TweenAnimationCaller.cs        # Auto-trigger Awake/Start/OnEnable
│   └── Attributes/
│       ├── ConditionalAttributes.cs   # ShowIf/HideIf/EnableIf/DisableIf/ReadOnly/Button/ShowInInspector
│       └── ExtendedAttributes.cs      # InfoBox/ProgressBar/Foldout/ColorPicker/Dropdown/Slider
└── Editor/
    ├── ConditionalPropertyDrawer.cs   # Drawer cho ShowIf/HideIf/EnableIf/DisableIf (+ ShowIfAny/All)
    ├── ReadOnlyPropertyDrawer.cs
    ├── InfoBoxPropertyDrawer.cs
    ├── ProgressBarPropertyDrawer.cs
    ├── DropdownPropertyDrawer.cs
    ├── SliderPropertyDrawer.cs
    ├── ShowInInspectorEditor.cs       # Custom editor cho mọi MonoBehaviour (Button + ShowInInspector)
    └── ResetParentKeepScale.cs        # Menu: Luzart/Reset Parent Scale & Keep Child Size
```

## Cách copy sang project khác

1. Copy toàn bộ folder `TweenAnimationPackage/` vào trong `Assets/` của project mới (ví dụ `Assets/Luzart/TweenAnimationPackage/`).
2. Cài DOTween từ Asset Store hoặc Package Manager.
3. Đảm bảo có TextMeshPro (File → Import TMP Essential Resources nếu Unity hỏi).
4. Unity sẽ auto-compile. Thư mục `Editor/` được Unity nhận diện tự động.

> **Lưu ý về namespace**: Tất cả nằm trong `namespace Luzart`. Nếu project khác đã có namespace trùng, dùng find-replace để đổi (ví dụ `MyCompany.Tweens`).

## Sử dụng

### 1. Animation đơn lẻ
Gắn component `TweenAnimation` + `TweenAnimationCaller` lên GameObject. Chọn `EAnimation` (Move/Scale/Fade/Text/Float/UnityEvent/...) và cấu hình `Duration`, `Ease`, `Loop`...

```csharp
// Từ code:
var tween = GetComponent<TweenAnimation>();
tween.Show();   // play
tween.Stop();   // kill
```

### 2. Sequence animation
Gắn `SequenceTweenAnimation` → drag các `TweenAnimation` khác vào list. Chọn `SequenceType` cho từng entry:
- `Append`: nối tiếp sau entry trước
- `Join`: chạy song song với entry Append gần nhất
- `Insert`: chèn tại `InsertTime` tuyệt đối

> **Quan trọng**: children của Sequence mà cần `From=current-at-my-turn` (ví dụ `Move về vị trí hiện tại`) phải đặt `Values.Timing = Lazy` và `OverrideFrom=false`. Nếu để `Eager` (default), `From` sẽ bị cache ngay khi `Sequence.Show()` gọi (trước khi các child chạy).

### 3. Auto-trigger
`TweenAnimationCaller` cho phép tự gọi `Show()` tại `Awake` / `Start` / `OnEnable`.

### 4. Attributes cho code của bạn
```csharp
public class Example : MonoBehaviour
{
    public bool useAdvanced;

    [ShowIf(nameof(useAdvanced), true)]
    public float advancedSetting;

    [ReadOnly]
    public int computedValue;

    [Slider(0f, 100f)]
    public float health = 75f;

    [ProgressBar("HP", 0f, 100f)]
    public float hp = 80f;

    [Dropdown("Easy", "Normal", "Hard")]
    public string difficulty = "Normal";

    [InfoBox("Tooltip hiển thị trong Inspector", InfoBoxType.Warning)]
    public string note;

    [Button("Reset Stats")]
    private void ResetStats() { health = 100f; }

    [ShowInInspector]
    private int _privateCounter = 42;
}
```

### 5. Editor menu
- `Luzart/Reset Parent Scale & Keep Child Size` — chọn một hoặc nhiều `RectTransform` → localScale về (1,1,1) nhưng giữ nguyên size visual của children (kể cả font size TMP/UGUI).

## Các loại animation support (EAnimation enum)

| Enum | Worker | Target component | Value |
|---|---|---|---|
| Move | TweenAnimationMove | Transform | Vector3 (world pos) |
| MoveLocal | TweenAnimationMoveLocal | Transform | Vector3 (local pos) |
| MoveAnchors | TweenAnimationMoveAnchors | RectTransform | Vector3 (anchored) |
| Scale | TweenAnimationScale | Transform | Vector3 (localScale) |
| Euler | TweenAnimationEuler | Transform | Vector3 (eulerAngles) |
| SizeDelta | TweenAnimationSizeDelta | RectTransform | Vector3 |
| AnchorMin | TweenAnimationAnchorMin | RectTransform | Vector3 |
| AnchorMax | TweenAnimationAnchorMax | RectTransform | Vector3 |
| FadeByCanvasGroup | TweenAnimationFade | CanvasGroup (auto-add) | float alpha |
| TextMeshProDOText | TweenAnimationTextMeshPro | TextMeshProUGUI | string |
| Float | TweenAnimationFloat | (none — DOVirtual) | float + UnityEvent<float> |
| UnityEvent | TweenAnimationUnityEvent | (none — DOVirtual) | UnityEvent (fire sau Duration) |

## Phụ thuộc về source

Code trong bundle này **chỉ** phụ thuộc vào:
- `UnityEngine` / `UnityEditor`
- `DG.Tweening` (DOTween)
- `TMPro` (TextMeshPro)

Không còn phụ thuộc `LocalizedText` hay bất cứ util nào của Luzart codebase gốc.

## API giá trị From/To — quan trọng

`TweenValueSettings` có 3 field chính điều khiển hành vi From/To:

| Field | Ý nghĩa |
|---|---|
| `Timing` (EValueTiming) | `Eager` (default): resolve + snap ngay khi `Show()` được gọi. `Lazy`: hoãn đến khi tween thật sự chạy. |
| `OverrideFrom` (bool) | `false` (default): From = giá trị hiện tại của target. `true`: From = giá trị user nhập. |
| `OverrideTo` (bool) | `true` (default): To = giá trị user nhập. `false`: To = giá trị hiện tại (hiếm). |

### Khi nào dùng `Eager` vs `Lazy`?

**Eager (default)** — Dùng cho animation standalone:
- Popup scale-in (0→1): object snap về (0,0,0) ngay khi `Show()` gọi, rồi chờ `DelayStart`, rồi tween lên (1,1,1). Suốt thời gian delay, object vô hình (scale=0) — đúng ý.
- Move from current to target: `OverrideFrom=false` → From auto-resolve ngay lúc Show() → tween từ vị trí lúc gọi Show.

**Lazy** — Dùng cho Sequence children cần "From = vị trí khi tới lượt":
- Sequence: `[A: Move tới X] → [B: Move về current]`. B phải đặt `Timing=Lazy` và `OverrideFrom=false` để resolve "current" ngay khi B bắt đầu play (chứ không phải lúc Sequence.Show gọi, khi đó A chưa play).

### Lưu ý về `To`

**To luôn resolve ở Eager phase** (bất kể `Timing`). Lý do: DOTween (ví dụ `DOMove`) bake `targetValue` ngay khi tween được tạo — không thể defer. Nếu cần "To=current lúc play", pattern hiện không support trực tiếp; workaround: tạo một tween riêng ở `AppendCallback` (thay vì phụ thuộc setting tự động).

## Ghi chú khi fork/chỉnh

- `_duration` trong `TweenSequenceSettings` chỉ là display field (`[ReadOnly]`). Getter `Duration` của interface tự cộng `DelayStart` và nhân `LoopCount`.
- `IsTimingPhase(isRuntime)` ở `TweenAnimationWorker<T>` là helper duy nhất quyết định "phase hiện tại có phải phase đã chọn?". Muốn thêm worker type mới, dùng helper này để đồng nhất hành vi.
- Không còn sentinel `-1` cho "chưa set". Thay bằng `OverrideFrom/OverrideTo` bool tường minh. An toàn với mọi giá trị số (kể cả âm).

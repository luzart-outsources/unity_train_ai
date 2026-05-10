# NinjaUI — Base UI framework cho Ninja School Online

Base code lấy kinh nghiệm từ 3 project (Lumber Inc, Story Horror, MARIA).
Xem `/docs/01-ui-manager-analysis.md` và `/docs/02-ui-manager-design.md` để hiểu rationale.

## Namespace

Toàn bộ framework types (`UIBase`, `UIBase<TData>`, `UIManager`, `UIContext`, `UIId`, `UIConfig`, ...) nằm trong **`namespace Luzart`**. Khi viết UI concrete trong code game:

```csharp
using Luzart; // required — tất cả type của framework đều ở đây

public class InventoryUI : UIBase<InventoryData> { ... }
```

> Ghi chú: tên product/thư mục vẫn là `NinjaUI/` (assembly `NinjaUI.Runtime`, menu `Tools → NinjaUI → ...`, `Create → NinjaUI → UI Registry`) — chỉ C# namespace là `Luzart` để đồng nhất với các package khác trong `Assets/Luzart/`.

## Cấu trúc thư mục

```
NinjaUI/
├── Runtime/
│   ├── Core/
│   │   ├── UILayer.cs            # Enum 6 lane
│   │   ├── UIId.cs               # Enum ID compile-time
│   │   ├── UIState.cs            # State machine + HideReason + CachePolicy
│   │   ├── UIContext.cs          # Data payload + options
│   │   ├── UIConfig.cs           # Metadata per UI
│   │   ├── UIRegistrySO.cs       # ScriptableObject chứa config
│   │   ├── UIBase.cs             # Base class (no data)
│   │   ├── UIBaseGeneric.cs      # UIBase<TData> type-safe
│   │   ├── UIHandle.cs           # Handle trả về từ ShowAsync
│   │   ├── IUIService.cs         # Interface facade
│   │   └── UIManager.cs          # Implementation chính
│   ├── Loading/
│   │   ├── IUIAssetProvider.cs
│   │   └── AddressableUIAssetProvider.cs
│   ├── Stack/
│   │   └── UILayerStack.cs       # Stack per lane
│   ├── Input/
│   │   ├── IEscapeResponder.cs
│   │   └── UIInputRouter.cs      # Chain of responsibility
│   └── NinjaUI.Runtime.asmdef
└── Samples/
    └── InventoryUI.Example.cs    # Tham khảo, không build
```

## Phụ thuộc

- **UniTask** (`com.cysharp.unitask`) — bắt buộc.
- **Addressables** (`com.unity.addressables`) — bắt buộc cho default provider.
- **DOTween** — optional. Base không phụ thuộc.

## Setup nhanh (5 phút)

### 1. Copy `NinjaUI/` vào `Assets/`

### 2. Tạo scene structure
```
Canvas (root, Screen Space - Overlay, sort order 0)
├── WorldOverlay   (RectTransform, anchors stretch)
├── Screen
├── Hud
├── Popup
├── System
├── Toast
└── UIManager      (GameObject, component UIManager)
    └── UIInputRouter (optional, same GameObject)
```

### 3. Tạo UIRegistrySO asset
Right-click Project → Create → NinjaUI → UI Registry.
Thêm entries, ví dụ:

| Id | StringId | AssetRef | Lane | CachePolicy | Preload | DismissByEscape |
|---|---|---|---|---|---|---|
| Loading | loading | → UI/Loading.prefab | System | KeepLoaded | ✓ | ✗ |
| Disconnect | disconnect | → UI/Disconnect.prefab | System | KeepLoaded | ✓ | ✗ |
| Toast | toast | → UI/Toast.prefab | Toast | KeepLoaded | ✓ | ✗ |
| MainMenu | main_menu | → UI/MainMenu.prefab | Screen | KeepLoaded | ✗ | ✗ |
| Inventory | inventory | → UI/Inventory.prefab | Popup | KeepLoaded | ✗ | ✓ |
| Shop | shop | → UI/Shop.prefab | Popup | PoolOnClose | ✗ | ✓ |
| QuestReward | quest_reward | → UI/QuestReward.prefab | Popup | ReleaseOnClose | ✗ | ✗ |

### 4. Gán references vào UIManager component
- Registry → UIRegistrySO vừa tạo.
- 6 lane roots → 6 RectTransform trong hierarchy.
- Input Router → optional.

### 5. Viết UI concrete
```csharp
public class InventoryUI : UIBase<InventoryData>
{
    protected override UniTask OnBeforeShowAsync(InventoryData data, CancellationToken ct)
    {
        txtGold.text = data.Gold.ToString();
        return UniTask.CompletedTask;
    }
}
```

### 6. Gọi từ gameplay
```csharp
var inv = await UIManager.Instance.ShowAsync<InventoryUI>(
    UIId.Inventory,
    new UIContext(new InventoryData { Gold = 1000 }));
```

## API chính

### Show

```csharp
// Generic typed
UniTask<T> ShowAsync<T>(UIId, UIContext, UIShowOptions, CancellationToken)

// Non-generic
UniTask<UIHandle> ShowAsync(UIId, UIContext, UIShowOptions, CancellationToken)

// Server-driven
UniTask<UIHandle> ShowByStringIdAsync(string, UIContext, ...)
```

### Hide

```csharp
UniTask HideAsync(UIHandle, UIHideOptions, CancellationToken)
UniTask HideAsync(UIId, UIHideOptions, CancellationToken)
UniTask CloseTopPopupAsync(UIHideOptions, CancellationToken)
UniTask CloseAllPopupsAsync(UIHideOptions, CancellationToken)
```

### Preload / Release

```csharp
UniTask PreloadAsync(UIId, IProgress<float>, CancellationToken)
void Release(UIId)
```

### Query

```csharp
bool IsVisible(UIId)
bool TryGetVisible(UIId, out UIBase)
```

### Events

```csharp
event Action<UIBase> OnShown;
event Action<UIBase, UIHideReason> OnHidden;
```

## Pattern khuyến nghị

### Cache policy nào cho UI nào?

| UI | CachePolicy | Lý do |
|---|---|---|
| Loading, Disconnect, Toast | `KeepLoaded` + `PreloadOnBoot` | Không thể load lúc mất mạng |
| Inventory, Shop, Mail, MainMenu | `KeepLoaded` | Mở thường xuyên, nặng |
| Quest dialog, Settings | `PoolOnClose` | Mở vừa phải |
| Intro cutscene, first-time tutorial | `ReleaseOnClose` | Chỉ mở 1-2 lần/session |

### DismissByEscape

| Trường hợp | Giá trị |
|---|---|
| Popup thường (Inventory, Shop) | `true` |
| Reward bắt buộc xem | `false` |
| Quảng cáo xem hết | `false` |
| Confirm dialog (tự xử lý ESC) | `false`, override `HandleEscape()` |

### Lane chọn thế nào?

- **Screen**: MainMenu, Lobby, CharacterSelect — là "màn chính", đè lên nhau theo kịch bản.
- **Popup**: Inventory, Shop, Confirm — stack LIFO + pause/resume, đóng popup top thì popup dưới tự resume.
- **Hud**: HP bar, minimap, hotbar — nhiều element song song, không pause nhau.
- **Toast**: thông báo flash. `AllowMultiInstance = true`.
- **System**: Loading, Disconnect, ForceUpdate — topmost, bắt buộc PreloadOnBoot.
- **WorldOverlay**: damage number, name tag. `AllowMultiInstance = true` + object pool bên trong UI.

## Testing

`IUIService` và `IUIAssetProvider` là interface → mock được.
Ví dụ `AddressableUIAssetProvider` có thể thay bằng `LocalPrefabUIAssetProvider` trong unit test.

```csharp
// Pseudo test
var mockProvider = new MockUIAssetProvider();
var manager = new UIManagerTestHarness(mockProvider);
await manager.ShowAsync(UIId.Inventory);
Assert.IsTrue(manager.IsVisible(UIId.Inventory));
```

## Tính năng nâng cao (đã built-in v2)

### UIPopupQueue — show tuần tự

Cho tutorial chain, daily reward combo, event offer sequential:

```csharp
// 3 popup tuần tự, popup sau chỉ show khi popup trước đã đóng.
await UIManager.Instance.EnqueuePopupAsync(UIId.QuestReward, new UIContext(data1));
await UIManager.Instance.EnqueuePopupAsync(UIId.DailyLogin, new UIContext(data2));
await UIManager.Instance.EnqueuePopupAsync(UIId.EventOffer, new UIContext(data3), priority: 100);
// priority cao hơn sẽ chèn lên đầu queue.
```

### UIBlockService — input blocker ref-counted

```csharp
using (UIManager.Instance.PushBlock("loading_character"))
{
    await LoadCharacterAsync();
} // tự động release khi exit scope

// Lồng nhiều block: chỉ unblock khi tất cả release.
using (UIManager.Instance.PushBlock("wait_server"))
using (UIManager.Instance.PushBlock("cinematic"))
{
    // blocker overlay active
}
```

Setup: gán `UIBlockService` component + `blockerOverlay` GameObject full-screen vào UIManager.

### Preload by Addressable label

```csharp
await UIManager.Instance.PreloadByLabelAsync("ui_boot");
await UIManager.Instance.PreloadByLabelAsync("ui_login", progress: myProgressBar);
```

Hoặc set `bootPreloadLabels` trong UIManager inspector → tự preload lúc Start.

### ShowToast shortcut

```csharp
UIManager.Instance.ShowToastAsync("Nhận 100 gold!", ToastStyle.Success).Forget();
UIManager.Instance.ShowToastAsync("Mất mạng", ToastStyle.Error, duration: 3f).Forget();
```

Cần implement `UIToast : UIBase<ToastData>` + đăng ký `UIId.Toast` trong Registry.

### Diagnostic events

```csharp
UIManager.Instance.OnLoadStarted   += id => Analytics.Log($"ui_load_start_{id}");
UIManager.Instance.OnLoadCompleted += id => Analytics.Log($"ui_load_done_{id}");
UIManager.Instance.OnLoadFailed    += (id, ex) => Sentry.CaptureException(ex);
UIManager.Instance.OnShown         += view => Debug.Log($"Shown {view.Id}");
UIManager.Instance.OnHidden        += (view, reason) => Debug.Log($"Hidden {view.Id} ({reason})");
UIManager.Instance.OnReleased      += id => Debug.Log($"Released {id}");
```

### Editor validator

Menu: **Tools → NinjaUI → Validate All Registries**.
Check: duplicate UIId, duplicate StringId, missing AssetRef, config mâu thuẫn.
Chạy trước mỗi build để catch lỗi sớm.

## Config cheatsheet

> Chỉ 9 field: 5 required (Id, StringId, AssetRef, Lane, CachePolicy) + 4 optional (PreloadOnBoot, AllowMultiInstance, DismissByEscape, PausableWhenOverlaid). Các setup visual (backdrop, raycast, animation) thuộc về prefab.

| UI | Lane | CachePolicy | Preload | MultiInstance | DismissByEscape | Pausable |
|---|---|---|---|---|---|---|
| Loading | System | KeepLoaded | ✓ | ✗ | ✗ | ✗ |
| Disconnect | System | KeepLoaded | ✓ | ✗ | ✗ | ✗ |
| Toast | Toast | KeepLoaded | ✓ | ✓ | ✗ | ✗ |
| MainMenu | Screen | KeepLoaded | ✗ | ✗ | ✗ | ✓ |
| Inventory | Popup | KeepLoaded | ✗ | ✗ | ✓ | ✓ |
| Shop | Popup | PoolOnClose | ✗ | ✗ | ✓ | ✓ |
| QuestReward | Popup | ReleaseOnClose | ✗ | ✗ | ✗ | ✓ |
| DailyLogin | Popup | ReleaseOnClose | ✗ | ✗ | ✗ | ✓ |
| HealthBar HUD | Hud | KeepLoaded | ✗ | ✗ | ✗ | ✗ |
| DamageNumber | WorldOverlay | KeepLoaded | ✗ | ✓ | ✗ | ✗ |

> Queue priority (tutorial chain, daily reward combo) truyền qua tham số `EnqueuePopupAsync(id, ctx, priority: 100)`, không còn ở config.

## Khi nào extend thêm?

Base này **đủ dùng** cho:
- Mở/đóng popup, screen, HUD, toast.
- Stack popup + pause/resume.
- Popup queue tuần tự (tutorial, reward chain).
- Back button mobile/PC với chain of responsibility.
- Addressables load async + preload by label.
- Server-driven popup qua string ID.
- Input blocker ref-counted.
- CachePolicy 3 tier (ReleaseOnClose / PoolOnClose / KeepLoaded).
- Diagnostic events cho analytics.
- Editor validation.

**Cần extend** nếu muốn:
- LRU memory eviction khi vượt ngưỡng RAM.
- ViewModel + ReactiveProperty (UniRx/R3).
- DI (VContainer/Zenject) — interface `IUIService` đã sẵn sàng.
- Animation presets (DOTween Sequence library).
- Safe area / multi-resolution helper.
- In-game debug window (list active UI + handle count).
- Hot-reload UI prefab lúc runtime (remote config).
- Timeout + retry policy ở provider.

## Migration checklist (nếu đang dùng base khác)

1. Map UI prefab cũ → entry trong UIRegistrySO (kèm lane + cache policy).
2. Đổi class base: `public class InventoryUI : UIBase<InventoryData>`.
3. Thay `UIManager.Instance.ShowUI()` → `await UIManager.Instance.ShowAsync()`.
4. Xóa `closeBtn.onClick.AddListener(Hide)` — base tự handle qua `OnCloseRequested`.
5. Xóa các dict path hardcoded.
6. Convert Resources.Load → Addressables asset reference.

## Performance tips

### Preload đúng cách
- **Boot group** (`bootPreloadLabels`): chỉ Loading, Disconnect, Toast, Alert. Đây là UI **bắt buộc chạy được lúc mất mạng**.
- **Login group** (preload sau khi vào login screen): MainMenu, CharacterSelect.
- **Lobby group** (preload sau khi vào lobby): Inventory, Shop, Quest, Mail.
- **KHÔNG preload all** lúc boot — kéo dài splash time + tốn RAM.

### CachePolicy đúng cách
- **KeepLoaded**: UI mở > 3 lần/phút (Inventory, Shop, Chat). Ngốn RAM nhưng mở tức thì.
- **PoolOnClose**: UI mở 1-3 lần/phút (Quest, Mail, Settings). Release instance, giữ asset.
- **ReleaseOnClose**: UI mở < 1 lần/session (Intro, first tutorial, one-time dialog). Giải phóng cả asset.

### Tránh leak CancellationToken
- Luôn pass `this.GetCancellationTokenOnDestroy()` hoặc scope CTS của feature.
- Khi feature bị destroy, CTS sẽ cancel → UIManager tự cleanup.

```csharp
// GOOD
await UIManager.Instance.ShowAsync<InventoryUI>(
    UIId.Inventory, ctx,
    ct: this.GetCancellationTokenOnDestroy());

// BAD — không có ct, MonoBehaviour destroy nhưng task vẫn chạy
await UIManager.Instance.ShowAsync<InventoryUI>(UIId.Inventory, ctx);
```

### Tránh Destroy/Instantiate liên tục
Popup mở-đóng liên tục (VD DamageNumber trong combat) **bắt buộc** `CachePolicy = KeepLoaded` + `AllowMultiInstance = true` + object pooling bên trong UIBase (spawn/despawn text element).

### Giảm GC pressure
- Dùng `UniTask` (struct, không allocate) thay `Task`.
- Tránh `string.Format` trong `OnBeforeShow` → dùng `StringBuilder` hoặc cache sẵn.
- Text binding: set text 1 lần trong `OnBeforeShow`, không trong `Update`.

## Troubleshooting

### "UIConfig not found for UIId=X"
- UIId chưa được thêm vào UIRegistrySO asset.
- **Fix**: Tools → NinjaUI → Validate All Registries.

### Popup thiếu nền đen mờ (backdrop)
- Framework không quản backdrop — backdrop là prefab-level.
- **Fix**: trong prefab, tạo Image full-screen màu đen alpha 0.6 là sibling / child phía dưới content. Set `raycastTarget = true` để chặn click nền. Muốn click-outside-to-close → gắn Button lên Image đó, gọi `OnCloseButtonClicked()`.

### ESC không đóng popup
- `DismissByEscape = false` trong config (phải vậy với reward bắt buộc, ads).
- UIInputRouter chưa được gán vào UIManager.
- `UIBase.HandleEscape()` bị override sai (trả false nhầm khi muốn dismiss).

### UI không hiện sau ShowAsync
- `AnimateShowAsync` mặc định là no-op — nếu subclass override fade/scale mà thiếu component (CanvasGroup, RectTransform target) → UI ở state khởi tạo của prefab.
- **Fix**: kiểm tra override `AnimateShowAsync` trong subclass, đảm bảo reference component cần thiết được gán trong prefab.

### Asset Addressables báo "not found"
- `AssetRef` chỉ vào prefab chưa được mark Addressable.
- Build Addressables chưa chạy sau khi thêm prefab mới.
- **Fix**: Window → Asset Management → Addressables → Groups → Build → New Build.

### Popup chồng nhau không pause nhau
- Config `PausableWhenOverlaid = false`.
- Hoặc 2 popup khác lane (Screen vs Popup) → không pause nhau (đúng behavior).

### OnHidden fire 2 lần
- Gọi `HideAsync` đồng thời từ nhiều chỗ.
- **Fix**: base đã guard `State == Hiding/Hidden` → nếu vẫn xảy ra, check xem có gọi `Destroy(view.gameObject)` thủ công không.

## FAQ

**Q: Có cần VContainer/Zenject không?**
A: Không bắt buộc. Dev có thể gọi `UIManager.Instance.ShowAsync()` trực tiếp. Interface `IUIService` chỉ để dễ inject khi muốn test hoặc team muốn DI thuần.

**Q: Có hỗ trợ UI Toolkit (UIElements) không?**
A: Base hiện chỉ hỗ trợ uGUI (Canvas + RectTransform). UI Toolkit cần provider + lane stack riêng.

**Q: Làm sao để popup A bật popup B rồi chờ B đóng?**
A: Dùng queue hoặc await task:
```csharp
// Cách 1: gọi trực tiếp (B sẽ đè lên A, A pause)
var bHandle = await UIManager.Instance.ShowAsync(UIId.PopupB);
await WaitUntilHidden(bHandle); // hàm helper tự viết

// Cách 2: dùng EnqueuePopupAsync — A enqueue B, queue tự chờ B close xong mới cho item tiếp theo
```

**Q: Có thể hot-reload UI prefab khi app đang chạy không?**
A: Chưa có built-in. Cần extend `IUIAssetProvider` để swap handle + re-instantiate. Thường chỉ cần cho editor workflow.

**Q: Làm sao debug xem UI nào đang visible?**
A: Subscribe `OnShown`/`OnHidden` + log. Hoặc tự viết debug window lặp qua `stacks[lane].All`.

**Q: ShowAsync quá chậm lần đầu mở popup?**
A: Chưa preload. Cho UI quan trọng đặt `PreloadOnBoot = true` hoặc preload theo group sau login.

**Q: Project có nhiều UIRegistrySO được không?**
A: Được. Mỗi scene/feature một registry. UIManager nhận 1 registry mặc định, có thể merge nhiều registry vào 1 runtime dictionary nếu cần (tự extend).

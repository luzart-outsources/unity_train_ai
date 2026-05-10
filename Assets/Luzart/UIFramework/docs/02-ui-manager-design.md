# Ninja School Online — UIManager Base Design

> Tài liệu kiến trúc cho base code trong `D:\TestAI\NinjaUI\Runtime\`.

## 1. Mục tiêu

- **Async-first** — mọi show/hide/load đều `UniTask`, không coroutine.
- **Config-driven** — metadata UI nằm trong `ScriptableObject`, không hardcode path.
- **Lane-based** — mỗi loại UI đi 1 controller riêng, không monolith.
- **Handle-based** — `ShowAsync` trả về `UIHandle` để hide/cancel chính xác.
- **Reference-counted** — asset remote giữ/release đúng vòng đời.
- **Pluggable loader** — `IUIAssetProvider` có thể thay Addressables ↔ AssetBundle ↔ Local.
- **Type-safe** — generic `UIBase<TData>`, không cast object khắp nơi.
- **Observable** — mọi state change đều raise event để debug/analytics.

## 2. Phụ thuộc

| Dependency | Bắt buộc? | Dùng làm gì |
|---|---|---|
| UniTask | **Bắt buộc** | async/await Unity |
| Addressables | Bắt buộc cho default provider | Load prefab UI |
| DOTween | Optional | Animation helper (base không phụ thuộc) |
| UniRx/R3 | Optional | Nếu muốn ViewModel reactive |

## 3. Phân loại UI (Lane)

| Lane | Ví dụ Ninja School | Quy tắc |
|---|---|---|
| `WorldOverlay` | Name tag, damage number | Gắn 3D, không block input |
| `Screen` | MapView, CharacterSelect, Lobby | Exclusive trong lane (chỉ 1 active) |
| `Popup` | Inventory, Shop, Quest, Mail | Stack LIFO, pause/resume |
| `HUD` | HP/MP bar, minimap, chat | Persistent, không stack |
| `System` | Loading, disconnect, maintenance | Topmost, override all |
| `Toast` | Thông báo nhận item, warning | Queue riêng, auto-fade |

## 4. Kiến trúc tổng quan

```
┌───────────────────────────────────────────────────────┐
│  IUIService (facade cho gameplay code)                │
│  ├─ ShowAsync<T>(UIId, data, options, ct)             │
│  ├─ ShowByStringIdAsync(string, data, options, ct)    │
│  ├─ EnqueuePopupAsync(UIId, ctx, priority, ct)        │
│  ├─ HideAsync(UIHandle, reason)                       │
│  ├─ CloseTopPopupAsync()                              │
│  ├─ PreloadAsync(UIId) / PreloadByLabelAsync(label)   │
│  ├─ Release(UIId)                                     │
│  ├─ ShowToastAsync(message, style, duration)          │
│  ├─ PushBlock(reason) → IDisposable                   │
│  └─ Events: OnLoadStarted/Completed/Failed,           │
│             OnShown, OnHidden, OnReleased             │
└─────────────────┬─────────────────────────────────────┘
                  │
┌─────────────────▼─────────────────────────────────────┐
│  UIManager (MonoBehaviour, singleton-accessible)      │
│  ├─ UIRegistrySO registry                             │
│  ├─ IUIAssetProvider assetProvider                    │
│  ├─ UILayerStack[6] stacks (per lane)                 │
│  ├─ UIPopupQueue popupQueue                           │
│  ├─ UIBlockService blockService                       │
│  ├─ UIInputRouter inputRouter                         │
│  └─ Dictionary<UIBase, CTS> activeCts                 │
└─────────────────┬─────────────────────────────────────┘
                  │
    ┌─────────────┼────────────┬─────────────┬─────────┐
    ▼             ▼            ▼             ▼         ▼
┌────────┐  ┌──────────┐  ┌─────────┐  ┌─────────┐ ┌───────┐
│ Asset  │  │  Layer   │  │  Popup  │  │  Block  │ │ Input │
│Provider│  │  Stacks  │  │  Queue  │  │ Service │ │Router │
└────────┘  └──────────┘  └─────────┘  └─────────┘ └───────┘
```

## 5. Flow ShowAsync (chuẩn)

```
1. Resolve UIConfig từ registry (by enum hoặc string id).
2. Check existing handle:
   - Nếu đang loading cùng UIId → await handle đó (request coalescing).
   - Nếu đã visible + !allowMultiInstance → bring to front + return.
3. Provider.LoadAsync(entry, ct) → GameObject prefab handle.
4. Pool.GetOrInstantiate(prefab, layerRoot[lane]).
5. view.Id = id; view.Data = data.
6. view.OnCreateAsync(ctx) (lần đầu).
7. view.OnBeforeShowAsync(ctx).
8. Lane controller áp luật:
   - Screen: pause/replace current screen.
   - Popup: push stack, pause top (nếu exclusive).
   - System: overlay lên topmost.
   - Toast: queue.
9. AnimateShow() (DOTween / custom).
10. view.OnShownAsync(ctx).
11. inputRouter.Register(view).
12. Return UIHandle.
```

## 6. Flow HideAsync (chuẩn)

```
1. Validate handle còn valid.
2. view.OnBeforeHideAsync(reason).
3. AnimateHide().
4. Lane cleanup:
   - Popup: pop stack, resume popup dưới.
   - Screen: restore back stack nếu có.
5. inputRouter.Unregister(view).
6. view.OnHiddenAsync(reason).
7. Pool.Return(view) hoặc Destroy.
8. CachePolicy:
   - KeepLoaded: retain asset.
   - PoolOnClose: destroy instance, retain asset.
   - ReleaseOnClose: destroy instance + release asset.
9. Fire event OnHidden.
```

## 7. UIConfig fields

| Field | Kiểu | Giải thích |
|---|---|---|
| `id` | `UIId` (enum) | Compile-time key, dev dùng nội bộ |
| `stringId` | `string` | Alias cho server-driven ("quest_reward") |
| `lane` | `UILayer` | Screen/Popup/HUD/System/Toast/WorldOverlay |
| `assetRef` | `AssetReferenceGameObject` | Addressable reference |
| `cachePolicy` | `UICachePolicy` | None / PoolOnClose / KeepLoaded |
| `dismissByEscape` | `bool` | Back button có đóng không |
| `exclusiveInLane` | `bool` | Có auto-pause UI khác cùng lane không |
| `preloadOnBoot` | `bool` | Load sẵn lúc game khởi động |
| `allowMultiInstance` | `bool` | Cho mở nhiều instance cùng lúc không |
| `blocksRaycast` | `bool` | Có chặn click xuống UI dưới không |
| `sortPriorityInLane` | `int` | Sort trong cùng lane |

## 8. Lifecycle UIBase

```
None → Loading → Loaded → Showing → Visible
                                         │
                                         ├─→ Paused → Visible (resume)
                                         │
                                         └─→ Hiding → Hidden → (Released)
```

```csharp
public abstract class UIBase : MonoBehaviour
{
    public UIId Id { get; internal set; }
    public UILayer Lane { get; internal set; }
    public UIState State { get; internal set; }

    public virtual UniTask OnCreateAsync(UIContext ctx, CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnBeforeShowAsync(UIContext ctx, CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnShownAsync(UIContext ctx, CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnBeforeHideAsync(UIHideReason r, CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnHiddenAsync(UIHideReason r, CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnPauseAsync(CT ct) => UniTask.CompletedTask;
    public virtual UniTask OnResumeAsync(CT ct) => UniTask.CompletedTask;
    public virtual bool HandleEscape() => false;
    public virtual void OnRelease() {}
}
```

Generic variant cho type-safe data:
```csharp
public abstract class UIBase<TData> : UIBase
{
    protected TData Data { get; private set; }
    // Override OnBeforeShowTyped(TData, ct) thay cho OnBeforeShowAsync
}
```

## 9. Memory management

**Reference counting:**
- Mỗi `ShowAsync` → `provider.Retain(entry, handle)`.
- Mỗi `HideAsync` → `provider.Release(entry, handle)`.
- Khi refcount=0 + policy=ReleaseOnClose → `Addressables.Release`.

**LRU eviction:**
- Background job check mỗi N giây.
- Nếu total loaded size > threshold → evict UI ít access nhất (trừ `KeepLoaded`).

**Critical preload:**
- `Loading`, `Disconnect`, `Toast` bắt buộc `preloadOnBoot=true`.
- Game không thể download lúc đang mất mạng.

## 10. Back button / ESC

Chain of responsibility (học từ Lumber):
```csharp
// Duyệt từ topmost xuống
for (int i = responders.Count - 1; i >= 0; i--)
    if (responders[i].HandleEscape()) return;
```

Mobile: hook `Application.quitting` + Android back key.
PC: `Input.GetKeyDown(KeyCode.Escape)`.

## 11. Anti-patterns cần tránh

- **Không** `Resources.Load` trong base.
- **Không** hardcode path string trong code runtime.
- **Không** nhét business flow (tutorial, scenario, next/back chapter) vào `UIManager`.
- **Không** dùng `FindObjectOfType` cho service lookup.
- **Không** 1 stack chung cho Popup + Screen + HUD.
- **Không** reflection auto-discover popup (khó debug).

## 12. Checklist chất lượng

- [ ] Gameplay code gọi base chỉ qua `IUIService`, không `UIManager.Instance` trực tiếp (khuyến khích).
- [ ] Mỗi UI có entry trong `UIRegistrySO` — không code hardcoded path.
- [ ] Popup nặng có `cachePolicy = KeepLoaded` (Inventory, Shop).
- [ ] Popup hiếm mở có `cachePolicy = ReleaseOnClose` (Intro, 1-time tutorial).
- [ ] `Loading`, `Disconnect`, `Toast` có `preloadOnBoot = true`.
- [ ] Popup bắt buộc (quảng cáo, reward) có `dismissByEscape = false`.
- [ ] Mọi `ShowAsync` có pass `CancellationToken` từ scope phù hợp.
- [ ] Unit test: mock `IUIAssetProvider` để test UIManager không cần Addressables.

## 13. Tính năng nâng cao (v2 — bổ sung từ technical doc của user)

### 13.1 UIPopupQueue

Queue service show popup tuần tự. Flow:
1. `EnqueueAsync(id, ctx, priority, ct)` insert vào queue theo priority giảm dần.
2. Process loop: show item đầu, chờ đến khi `State` không còn Showing/Visible/Paused.
3. Show item tiếp theo.
4. Queue trống → fire `OnQueueEmpty`.

Dùng cho:
- **Tutorial chain**: bước 1 → bước 2 → bước 3 (không chồng).
- **Daily reward combo**: login bonus → 7-day reward → event offer.
- **Server push tuần tự**: reconnect → maintenance notice → announcement banner.

Popup show trực tiếp qua `ShowAsync` không đi qua queue → vẫn có thể chồng lên.

### 13.2 UIBlockService

Input blocker với reference counting + reason tracking:
- `PushBlock(reason)` → trả `IDisposable`, tăng refCount.
- `Dispose()` giảm refCount.
- RefCount > 0 → overlay GameObject active (chặn GraphicRaycaster toàn màn hình).
- RefCount = 0 → overlay inactive.

Lợi ích:
- Pattern `using` an toàn, không bị leak block.
- Nhiều chỗ cùng block được (lồng nhau).
- `GetActiveReasons()` debug dễ.
- `ForceClearAll()` cho error recovery.

### 13.3 Diagnostic events

6 events để analytics + debug:

| Event | Khi nào raise |
|---|---|
| `OnLoadStarted(id)` | Bắt đầu load asset từ provider |
| `OnLoadCompleted(id)` | Load + instantiate thành công |
| `OnLoadFailed(id, exception)` | Exception trong load/show |
| `OnShown(view)` | AnimateShow xong, user bắt đầu tương tác |
| `OnHidden(view, reason)` | AnimateHide xong, instance đã pooled/destroyed |
| `OnReleased(id)` | Asset handle được release |

### 13.4 Preload by Addressable label

`PreloadByLabelAsync(label, progress, ct)`:
- Gọi `Addressables.DownloadDependenciesAsync(label)`.
- Dùng cho boot group: `"ui_boot"`, `"ui_login"`, `"ui_gameplay"`.
- Cấu hình `UIManager.bootPreloadLabels[]` → tự preload lúc Start.

### 13.5 Backdrop + ESC — KHÔNG phải concern của framework

Quyết định thiết kế: **backdrop (nền mờ modal) là prefab-level, không phải config**.

Framework không render backdrop vì:
- Backdrop là pure visual — Image full-screen màu đen alpha, kẹp raycast. Unity native xử lý tốt qua `Image.raycastTarget` + `CanvasGroup.blocksRaycasts`.
- Có config "BackdropPolicy" ở framework thì vẫn phải có GameObject backdrop trong prefab → 2 nguồn truth, lệch nhau là bug.
- Designer làm prefab tự biết có cần backdrop không, muốn màu/alpha/click-outside behavior gì — flexibility cao hơn enum 4 giá trị.

Tương tự, ESC policy rút gọn về 1 bool `DismissByEscape`:
- Mặc định `true` → `UIBase.HandleEscape()` gọi `OnCloseButtonClicked()`.
- `false` → popup reward/ads/force-update, ESC không đóng.
- Cần hành vi custom (hỏi confirm, mở submenu...) → override `HandleEscape()` trả `true` sau khi xử lý xong → stop propagation.

3 case đó đủ cover, không cần enum 3 giá trị riêng.

### 13.6 Editor validator

`Tools → NinjaUI → Validate All Registries`:
- Scan tất cả `UIRegistrySO` asset trong project.
- Check duplicate UIId, duplicate StringId, missing AssetRef, Id=None.
- Cảnh báo config mâu thuẫn:
  - `AllowMultiInstance=true + CachePolicy=KeepLoaded` (chỉ cache 1 instance, instance khác destroy).
  - `PreloadOnBoot=true + CachePolicy=ReleaseOnClose` (preload xong không giữ → lần mở đầu vẫn phải load lại).
  - `Lane=Toast + AllowMultiInstance=false` (toast thường cần nhiều cái).
- Chạy trước mỗi build để catch sớm.

## 14. Extension point tương lai

- **VContainer/Zenject** — inject `IUIService` vào gameplay systems.
- **Remote UI config** — tải `UIRegistrySO` data từ CDN JSON.
- **A/B testing** — swap `UIConfig.assetRef` theo user bucket.
- **Analytics hook** — subscribe `OnShown/OnHidden` để log.
- **Safe area / orientation** — helper trong `UIBase`.
- **Localization** — binder tự động lookup qua `UIContext.Locale`.

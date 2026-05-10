---
title: NinjaUI Framework (Luzart)
category: systems
tags: [luzart, ninjaui, ui, framework, async, unitask]
sources: [Assets/Luzart/UIFramework/NinjaUI/]
created: 2026-05-11
updated: 2026-05-11
---

# NinjaUI — UI Framework (Luzart namespace)

UI framework riêng nằm trong `Assets/Luzart/UIFramework/NinjaUI/`, KHÔNG phải code DATN — đây là module reusable do user (chủ wiki) tự maintain. Stack: async-first qua UniTask, lane-based stacks, reference-counted asset provider, ScriptableObject registry.

## Mục đích

Khác với `Assets/Scripts/UI/` của DATN (UIManager đơn giản gắn theo Stardew template), NinjaUI là production-grade UI infrastructure:
- Async show/hide (UniTask), không IEnumerator coroutine
- Lane stack (WorldOverlay/Screen/Hud/Popup/System/Toast) — mỗi lane có ngữ nghĩa riêng (LIFO pause, flat list, topmost...)
- Provider abstraction (`IUIAssetProvider`) → swap giữa direct prefab / Addressables / AssetBundle CDN không đụng UIManager
- Cache policy per UI: `ReleaseOnClose`, `PoolOnClose`, `KeepLoaded`
- Pause/Resume cho UI bị popup khác đè lên
- Popup queue (priority) cho boot screens / reward popup

## Cấu trúc thư mục

```
Assets/Luzart/UIFramework/NinjaUI/
├── Runtime/
│   ├── Core/              UIManager, UIBase, UIConfig, UIRegistrySO, UIId, UIContext
│   ├── Loading/           IUIAssetProvider, DirectPrefabUIAssetProvider
│   ├── Stack/             UILayerStack
│   ├── Queue/             UIPopupQueue
│   ├── Services/          UIBlockService
│   └── NinjaUI.Runtime.asmdef
├── Editor/                MissingScriptCleaner, SpriteRemap, TMPFont tools, UIRegistryValidator
└── Samples/               InventoryUI.Example.cs
```

## Asmdef

`NinjaUI.Runtime.asmdef`:
```json
{ "name": "NinjaUI.Runtime", "rootNamespace": "Luzart", "references": ["UniTask"] }
```

→ Chỉ cần UniTask. Đã bỏ `Unity.Addressables` + `Unity.ResourceManager` khi chuyển sang DirectPrefab provider.

## UIManager (singleton MonoBehaviour)

Trung tâm điều phối, persistent qua DontDestroyOnLoad. API chính:

```csharp
UIManager.Instance.ShowAsync<InventoryUI>(UIId.Inventory, ctx, opts, ct);
UIManager.Instance.HideAsync(handle, opts, ct);
UIManager.Instance.HideAllExceptSystemAsync();    // dùng trước SceneManager.LoadScene
UIManager.Instance.EnqueuePopupAsync(UIId.Reward, ctx, priority: 5);
UIManager.Instance.ShowToastAsync("Đã lưu", ToastStyle.Info, 2f);
UIManager.Instance.PushBlock("loading");          // input blocker, IDisposable
```

State per UI: `None → Loaded → Showing → Visible → Hiding → Hidden → Released` (+ `Paused`).

Coalescing: nhiều `ShowAsync(UIId.X)` đồng thời → 1 task duy nhất, các caller share kết quả qua `UniTaskCompletionSource.Task.Preserve()`.

## IUIAssetProvider abstraction

Interface 5 method:
- `LoadAsync(config, ct)` — return GameObject prefab
- `Release(config)` — giảm ref count
- `GetDownloadSizeAsync` / `PreloadAsync` / `PreloadByLabelAsync` — relevant với Addressable/CDN, no-op với DirectPrefab

Implementations:
- **`DirectPrefabUIAssetProvider`** (default sau 2026-05-11) — prefab giữ trong `UIConfig.AssetRef` qua direct GameObject reference
- ~~`AddressableUIAssetProvider`~~ — đã xoá 2026-05-11. Xem [[decisions/remove-addressables-add-unitask]].

## UIConfig (per-UI metadata)

Field chính:
- `UIId Id` (enum) + `string StringId` (cho server-driven popup)
- `GameObject AssetRef` — direct prefab reference (drag-drop trong Inspector)
- `UILayer Lane` — Popup/Screen/Hud/...
- `UICachePolicy CachePolicy` — Release/Pool/KeepLoaded
- `bool PreloadOnBoot`, `AllowMultiInstance`, `DismissByEscape`, `PausableWhenOverlaid`

Quy tắc thêm field (per source comment): chỉ thêm nếu *framework* phải đọc field đó ở runtime. Nếu chỉ UI tự đọc → đặt trong UI script hoặc prefab.

## UIRegistrySO

ScriptableObject chứa list `UIConfig`. Designer tạo asset (`Right-click → Create → NinjaUI → UI Registry`), gán vào `UIManager.registry`. Lookup qua `BuildLookup()` cache `Dictionary<UIId, UIConfig>` + `Dictionary<string, UIConfig>` cho both direct + server-driven access.

Validator (`UIRegistryValidator.cs`) Editor menu `Tools/NinjaUI/Validate All Registries`: check duplicate Id, missing AssetRef, prefab có UIBase component không, mâu thuẫn config (vd `AllowMultiInstance + KeepLoaded`).

## UILayerStack

Wrapper logic stack cho mỗi lane. Method:
- `Push(view)` → return list UI cần Pause (popup đang Visible khi popup mới đẩy lên)
- `Pop(view)` → return list UI cần Resume
- `BringToTop(view)` (singleton mode), `FindById(id)`

## UIPopupQueue

Priority queue cho popup boot/reward. `EnqueueAsync(id, ctx, priority)` → popup chỉ show khi popup trước đã hide. Tránh chồng popup cùng lúc.

## Tích hợp với phần khác trong project

- **DATN UIManager** (`Assets/Scripts/UI/UIManager.cs`) — code Stardew template — KHÔNG dùng NinjaUI. Coexist song song. Quyền có thể migrate sau nếu muốn pro hơn.
- **Sentis chat UI** (`PhaseAChatUI.cs` trong `Assets/AI/Scripts/`) — chưa dùng NinjaUI, hiện viết tay Canvas. Có thể migrate sau.
- **Phase B test scenes** (`PhaseB_MovementTest.unity`) — không có UI nên không quan tâm.

## Khi nào dùng NinjaUI vs DATN UIManager?

> [!info] Khuyến nghị
> Game ĐATN demo (7 ngày): dùng DATN UIManager (đã viết sẵn, integrate dialogue/shop/inventory). NinjaUI dành cho project lớn hơn cần lane stack + async lifecycle. Nếu Quyền không cần production-grade UI → giữ DATN UIManager, để NinjaUI làm reusable cho project khác.

## Liên quan

- [[decisions/remove-addressables-add-unitask]] — quyết định bỏ Addressables, thêm UniTask
- [[systems/datn-scene-locations]] — DATN có UI fade riêng, không qua NinjaUI
- [[technical/datn-architecture]] — DATN UIManager là singleton khác

---
## Backlinks
- [[index]]
- [[decisions/remove-addressables-add-unitask]]

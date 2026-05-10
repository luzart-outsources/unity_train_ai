---
title: Bỏ Addressables, thêm UniTask
category: decisions
tags: [luzart, ninjaui, addressables, unitask, package, refactor]
sources: [Assets/Luzart/UIFramework/NinjaUI/, Packages/manifest.json]
created: 2026-05-11
updated: 2026-05-11
---

## Bỏ Addressables, thêm UniTask vào project

**Date**: 2026-05-11
**Decided by**: User
**Status**: active

### Context

Sau khi import DATN code (xem [[decisions/import-datn-game-base]]), project có thêm `Assets/Luzart/UIFramework/NinjaUI/` — UI framework user tự viết trước đó. Compile error đỏ toàn bộ NinjaUI vì:
1. Asmdef `NinjaUI.Runtime.asmdef` reference `Unity.Addressables` + `Unity.ResourceManager` — package KHÔNG có trong `Packages/manifest.json` của user.
2. Code dùng `using Cysharp.Threading.Tasks;` (UniTask) — cũng KHÔNG có trong manifest.

User yêu cầu: "thêm UniTask, bỏ Addressables — không cần Addressables".

### Options considered

1. **Add cả Addressables lẫn UniTask** (giữ NinjaUI nguyên trạng)
   - Pro: zero code change, framework chạy ngay
   - Con: Addressables setup phức tạp (phải build group, label, content catalog) — overkill cho ĐATN demo
   - Con: thêm 1 layer streaming asset không cần thiết

2. **Bỏ Addressables, dùng Resources.Load** (path-based)
   - Pro: standard alternative, không cần catalog
   - Con: prefab phải để trong `Resources/` folder → bloat build, không kiểm soát được scope load
   - Con: API khác Addressables → phải refactor `UIConfig` từ `AssetReferenceGameObject` sang string path

3. **Bỏ Addressables, dùng direct GameObject reference** ⭐ chọn
   - Pro: đơn giản nhất — designer drag-drop prefab vào `UIConfig.AssetRef` trong Inspector
   - Pro: prefab graph load cùng UIRegistrySO → predictable RAM
   - Pro: API chỉ thay 1 line trong UIConfig (`AssetReferenceGameObject` → `GameObject`)
   - Con: prefab giữ trong RAM cả lúc UI chưa show (acceptable cho <50 popup)

### Decision

**Chọn Option 3** — direct GameObject reference. Implementation:

1. **Add UniTask vào manifest.json** (top of dependencies):
   ```json
   "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
   ```

2. **Bỏ Addressables references** khỏi `NinjaUI.Runtime.asmdef`:
   ```diff
   - "references": ["Unity.Addressables", "Unity.ResourceManager", "UniTask"]
   + "references": ["UniTask"]
   ```

3. **`UIConfig.cs`**: `AssetReferenceGameObject AssetRef` → `GameObject AssetRef`. Bỏ `using UnityEngine.AddressableAssets;`.

4. **Xoá `AddressableUIAssetProvider.cs`** (+ .meta), **tạo mới `DirectPrefabUIAssetProvider.cs`**:
   - Implement `IUIAssetProvider` với in-memory cache + ref count
   - `LoadAsync` return ngay `UniTask.FromResult(config.AssetRef)`
   - `PreloadAsync`, `GetDownloadSizeAsync`, `PreloadByLabelAsync` thành no-op (return CompletedTask hoặc 0L)
   - `ReleaseAll()` chỉ clear cache dict (không có handle để Addressables.Release)

5. **`UIManager.cs`**:
   - `assetProvider ??= new AddressableUIAssetProvider()` → `new DirectPrefabUIAssetProvider()`
   - Cast `is AddressableUIAssetProvider addr` → `is DirectPrefabUIAssetProvider direct`
   - Update tooltip header `bootPreloadLabels` (giờ là legacy field, no-op)

6. **`UIRegistryValidator.cs`** (Editor):
   - `e.AssetRef.RuntimeKeyIsValid()` → null check trên GameObject
   - Bonus: thêm check prefab root có component kế thừa `UIBase` không (catch sớm bug "drop wrong prefab")

7. **`IUIAssetProvider.cs`** + **`UIRegistrySO.cs`**: update XML doc-comment (bỏ mention "Addressables" khi không còn liên quan).

### Consequences

- **NinjaUI compile clean** với UniTask 2.5.x từ git URL chính thức của Cysharp.
- **Mất binding cũ trong UIRegistrySO asset** (nếu user đã tạo trước đây): `AssetReferenceGameObject` ↔ `GameObject` là 2 type Unity không serialise cross-format. User phải drag-drop lại từng prefab vào `AssetRef` field. One-time chi phí — nếu chưa có UIRegistrySO asset thì miễn.
- **Không có streaming** = build size lớn hơn (mọi prefab UI nằm trong build từ đầu). Acceptable với ĐATN demo, chỉ vấn đề khi build mobile có quota size.
- **`bootPreloadLabels` Inspector field giữ nguyên** nhưng no-op — fallback nếu sau này swap sang Addressable provider.
- **Interface `IUIAssetProvider` không đổi shape** → caller (UIManager) không phải refactor logic, chỉ đổi `new` keyword.

### Follow-up tasks

- [ ] Mở Unity, đợi UniTask resolve qua git (lần đầu ~30s nếu mạng OK)
- [ ] Nếu đã có UIRegistrySO asset → re-bind prefab vào AssetRef
- [ ] Console clean compile error → confirm thành công
- [ ] Optional: xoá `bootPreloadLabels` field khỏi UIManager nếu chắc không quay lại Addressables

### File changes (atomic)

| File | Action |
|---|---|
| `Packages/manifest.json` | + `com.cysharp.unitask` git URL |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/NinjaUI.Runtime.asmdef` | rewrite, references chỉ còn `UniTask` |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Core/UIConfig.cs` | rewrite, `AssetRef` type GameObject |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Loading/AddressableUIAssetProvider.cs` | **deleted** (+ .meta) |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Loading/DirectPrefabUIAssetProvider.cs` | **new** |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Core/UIManager.cs` | 4 edit: provider name, tooltip, comment, cast |
| `Assets/Luzart/UIFramework/NinjaUI/Editor/UIRegistryValidator.cs` | replace `RuntimeKeyIsValid` → null + UIBase check |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Loading/IUIAssetProvider.cs` | doc-comment update |
| `Assets/Luzart/UIFramework/NinjaUI/Runtime/Core/UIRegistrySO.cs` | doc-comment update (bỏ "AssetReference" mention) |

---
## Backlinks
- [[systems/ninjaui-framework]]
- [[index]]

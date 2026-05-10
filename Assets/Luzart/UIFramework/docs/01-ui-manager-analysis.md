# Phân tích so sánh 3 hệ thống UIManager

> Tài liệu tham khảo cho thiết kế base UIManager của **Ninja School Online** (Unity, Addressables + AssetBundle).

## 1. Project khảo sát

| # | Project | Đường dẫn | Đặc tính |
|---|---------|-----------|----------|
| 1 | Lumber Inc | `D:\Unity Main\lumber-inc-lfs` | Production-grade, dual popup system (Screw + WilliamExtra) |
| 2 | Story Horror | `D:\Unity Training\story-hornor-game` | MVP clean, SO-registry driven |
| 3 | MARIA | `D:\Unity Training\MARIA` | Simple mobile, Resources.Load hardcoded |

---

## 2. Lumber Inc

### 2.1 File quan trọng

- `Assets/Screw/Scripts/UI/Popup/Popup.cs` — Base popup 200+ dòng.
- `Assets/Screw/Scripts/UI/Popup/AbstractPopupController.cs` — Generic controller quản stack.
- `Assets/Screw/Scripts/UI/Popup/PopupContainer.cs` — `Stack<Popup>` LIFO (230 dòng).
- `Assets/Screw/Scripts/UI/Popup/EscapePropagator.cs` — Back/ESC chain of responsibility.
- `Assets/Screw/Scripts/UI/Popup/MVCViewPopup.cs` — Popup generic `<TViewData>`.
- `Assets/WilliamExtra/Adhoc/UI/Popup/APopupController.cs` — Advanced controller (247 dòng).
- `Assets/WilliamExtra/Adhoc/UI/Popup/PopupHolder.cs` — Wrapper Main/Sub visibility (229 dòng).
- `Assets/AssetManager/AssetManager.cs` — Addressables + Variant system.
- `Assets/Scripts/Core_Misc/UIController.cs` — Canvas mode switching (World/FPS/Village/Minigame).

### 2.2 Điểm mạnh

**Main + Sub Visibility (quan trọng nhất).**
`PopupHolder` tách 2 trục visibility độc lập:
- `MainVisibility` — trạng thái open/close của popup.
- `SubVisibility` — trạng thái pause/resume khi bị popup khác đè lên.
- `ActualVisibility = Min(Main, Sub)`.

Ý nghĩa: popup A đang mở, popup B đè lên → A pause (Sub=0) nhưng Main vẫn =1. Khi B đóng, A chỉ cần restore Sub, không mất state (scroll, input, animation).

**Stack management chuẩn.**
`PopupContainer.Show(popup, data, dismissCurrent, pauseCurrent)`:
- Nếu current dismissible → pop + dismiss.
- Nếu không → pause current, push new popup.
- `DismissAllPopups` duyệt từ top xuống, tôn trọng `WorldSwitchingPersistent`.

**IEscapeResponder chain.**
```csharp
foreach (var r in responders) if (r.HandleEscape()) break;
```
Popup có flag riêng `DismissByBackey` tách với `Dismissable` → popup có thể close nhưng ESC không đóng (ví dụ bắt buộc xem quảng cáo xong).

**MVCView pattern.**
`MVCViewPopup<TViewData>` auto-bind data qua `view.Setup(d)` / `view.TearDown()`. Support `IDisposable` cleanup.

**Type-safe generic API.**
`Show<AlertPopup>(data)` — compile-time safety, không cần string ID.

### 2.3 Điểm yếu

- **Không dùng async/await** — tất cả coroutine + DOTween callback. Không fit flow Addressables download.
- **Không có string ID routing** — không map được server JSON → popup.
- **Không có popup queue built-in** — phải viết tay.
- **Reflection auto-discover** popup qua public properties — magic, khó debug.
- **FindFirstObjectByType()** làm service locator — O(n), tight coupling.
- **Mode-based UIController** (CameraMode → canvas group) không phù hợp MMORPG 1 mode chính.

### 2.4 Đánh giá chung

- **Giá trị tham khảo cao nhất** trong 3 project về flow control.
- Kiến trúc tách lane (`PopupController` / `SheetController` / specialized controllers) rất đáng học.
- Học **ý tưởng**, không copy implementation (framework-specific, Screw/WilliamExtra utilities nặng).

---

## 3. Story Horror

### 3.1 File quan trọng

- `Assets/Luzart/Utility/Script/UIBase/UIManager.cs` — 290 dòng, manager chính.
- `Assets/Luzart/Utility/Script/UIBase/UIBase.cs` — 100 dòng, base view.
- `Assets/Luzart/Utility/Script/UIBase/UIRegistrySO.cs` — 59 dòng, SO config.
- `Assets/Luzart/Utility/Script/Events/GameEventChannel.cs` — SO event bus.
- `Assets/Luzart/Utility/Script/Other/Observer.cs` — pub/sub string-based.

### 3.2 Điểm mạnh

**UIRegistrySO — pattern config tốt nhất.**
```csharp
public class UIEntry
{
    public UIName uiName;
    public UIBase prefab;      // direct ref
    public int layerIndex;     // 0..4
    public bool useCache;
}
```
Metadata nằm trong asset, không trong code. Dev chỉ cần kéo prefab vào inspector.

**5 layer cố định, rõ nghĩa.**
```
rootOb[0]: Screen        (sort 0)
rootOb[1]: Popup         (sort 1)
rootOb[2]: HUD           (sort 2)
rootOb[3]: System        (sort 3)
rootOb[4]: Toast         (sort 4)
```
Clear separation, không xung đột sort order.

**ScriptableObject event channel.**
`GameEventChannel<T>` — asset-based, decoupled, hot-swap trong editor.

**Lifecycle đơn giản, dễ extend.**
`Setup()` chỉ gọi 1 lần, `Show/Hide` virtual, `TweenAnimationCaller` tích hợp DOTween.

### 3.3 Điểm yếu

- **Prefab reference cứng** trong Registry — không scale cho 200+ UI, không hot-update online.
- **Không async/Addressables** — coroutine + `Instantiate(prefab)` trực tiếp.
- **Không có back button handler** tập trung — mỗi UI tự xử lý `closeBtn`.
- **Singleton dependency** — `GameFlowController.Instance`, `NotebookManager.Instance` xài khắp nơi → tight coupling.
- **Không pooling** — Instantiate/Destroy mỗi lần non-cached.
- **Không có ViewModel/MVP** — UI trực tiếp gọi Manager.
- **`listScreenActive` chung 1 list** — popup, screen, HUD cùng stack, logic bị trộn.

### 3.4 Đánh giá chung

- **Clean nhất, dễ hiểu nhất** trong 3 project.
- Phù hợp game small/medium (< 50 UI), single-player.
- **Giữ lại**: pattern `UIRegistrySO`, 5-layer fixed, Show/Hide lifecycle structure.
- **Nâng cấp**: thay prefab ref → `AssetReferenceGameObject`, thay coroutine → UniTask.

---

## 4. MARIA

### 4.1 File quan trọng

- `Assets/_GameLuzart/Utility/Script/UIBase/UIManager.cs` — 634 dòng, manager phình to.
- `Assets/_GameLuzart/Utility/Script/UIBase/UIBase.cs` — 99 dòng.
- `Assets/_GameLuzart/Utility/Script/UIBase/UIPack.cs` — 307 dòng, IAP shop.
- `Assets/_GameLuzart/GameManager/Script/Manager/DataManager.cs` — SingletonSaveLoad.

### 4.2 Điểm mạnh

- **API đơn giản** — `ShowUI(UIName.MainMenu)`, dev nào cũng dùng được.
- **5-layer structure** tương tự Story Horror.
- **Coroutine tracking per MonoBehaviour** — `GameUtil` tự động stop coroutine cũ.

### 4.3 Điểm yếu (nhiều hơn mạnh)

- **`Dictionary<UIName,string>` hardcoded 100+ entries trong `UIManager.cs`** — phình to, parse string kiểu `"1,0,UIMainMenu"`.
- **Enum UIName 100+ entries** — Tut1..Tut3, Level1_0..Level4_5 — rất khó maintain.
- **Resources.Load only** — không Addressables, không remote update. Build size to.
- **UIManager ôm business flow** — `ShowScenario`, `ShowNextScenario`, `ShowBackScenario`, top bar logic, next/back policy. Đây là god object.
- **Không async, không race safety** — load multiple UI đồng thời là crash.
- **Singleton khắp nơi** — không testable.
- **Không có queue, không preload, không retry** — không phù hợp game online.

### 4.4 Đánh giá chung

- **Tham khảo để biết cái gì KHÔNG nên giữ.**
- Không nên dùng làm base cho Ninja School Online.
- Điều tránh rõ nhất: **hardcode metadata UI trong runtime code**.

---

## 5. Bảng so sánh tổng hợp

| Tiêu chí | Lumber | Story Horror | MARIA |
|---|---|---|---|
| Load asset | AssetManager + Addressables (partial) | Direct prefab ref | Resources.Load |
| Async | Coroutine | Coroutine | Coroutine |
| Stack | `Stack<Popup>` + pause/resume | `listScreenActive` flat | `listScreenActive` flat |
| Visibility | **Main+Sub float 0-1** | Binary active | Binary active |
| Registry | SO container + reflection | **SO UIRegistrySO** | Dict hardcoded |
| Data passing | MVCView `<TData>` + IDataProvider | `Init()` + SO event | `Init()` + DataResource |
| Back button | **IEscapeResponder chain** | Chỉ closeBtn | Scenario back/next manual |
| Pooling | IPoolee + SetActive | `useCache` flag | `isCache` flag |
| DI | FindFirstObjectByType | Singleton | Singleton |
| MVP/MVVM | MVCView + IDataProvider | View + Manager query | View gộp Presenter |
| Queue popup | Không | Không | Không |
| Preload | Không | Không | Không |
| String ID | Không | Không | Không |
| **Fit Ninja School** | **60%** | **40%** | **35%** |

---

## 6. Kết luận chọn lọc

| Lấy từ | Cái gì | Lý do |
|---|---|---|
| **Lumber** | Main/Sub visibility | Giữ state popup khi bị đè |
| **Lumber** | IEscapeResponder chain | Back button chuẩn mobile/PC |
| **Lumber** | MVCView `<TData>` | Type-safe data binding |
| **Lumber** | Stack per-lane + pause/resume | Flow control chuẩn MMORPG |
| **Lumber** | Queue/request gating (từ `PopupQueuer`) | Chống tranh chấp popup |
| **Story Horror** | `UIRegistrySO` pattern | Config-driven, inspector-friendly |
| **Story Horror** | 5-layer fixed | Không xung đột sort order |
| **Story Horror** | Clean lifecycle | Dễ extend, dễ hiểu |
| **MARIA** | (nothing) | — |

Tự thêm cho game online:
- **Addressables + UniTask** — thay coroutine.
- **String ID alias song song với enum** — server-driven popup.
- **Reference counting + LRU** — quản memory bundle.
- **CachePolicy enum** (None / KeepLoaded / PoolOnClose) — thay `bool isCache`.
- **IUIAssetProvider** interface — pluggable loader (Addressables / AssetBundle / Local).
- **Lane controller** — không nhét mọi thứ vào 1 UIManager monolith.

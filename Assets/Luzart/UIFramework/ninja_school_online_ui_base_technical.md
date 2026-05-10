# Ninja School Online UI Base Technical

> ⚠️ **File này là proposal pre-implementation** — ghi lại các ý tưởng thiết kế lúc brainstorm, KHÔNG phản ánh implementation cuối cùng. Nhiều field/policy liệt kê ở đây đã bị cắt khi triển khai vì không có use case thật (ví dụ `UIBackdropPolicy`, `UIEscapePolicy`, `UIPreloadPolicy`, `blockInput`, `destroyOnHide`, `sortPriority`, `keepLoadedAcrossScenes`, `addressableLabel`...). Xem [`NinjaUI/README.md`](./NinjaUI/README.md) + [`docs/03-ui-manager-usage-guide.md`](./docs/03-ui-manager-usage-guide.md) để biết API và config thật.

## 1. Mục tiêu

Thiết kế một base UI đủ tốt cho game online, thỏa các tiêu chí:

- Dễ dùng cho gameplay code.
- Dễ config và dễ sửa khi số lượng màn hình tăng mạnh.
- Không biến thành `god object`.
- Tách rõ flow của `screen`, `popup`, `hud`, `toast`, `system overlay`.
- Hỗ trợ `Addressables` và remote content tải từ bundle.
- Chịu được async load, retry, preload, cache, release.
- An toàn khi nhiều request UI xảy ra gần nhau.
- Dễ debug và dễ thêm tool validate cho designer.

## 2. Đánh giá 3 hệ thống tham khảo

## 2.1. `story-hornor-game`

Tham chiếu chính:

- [UIManager.cs](D:\Unity Training\story-hornor-game\Assets\Luzart\Utility\Script\UIBase\UIManager.cs)
- [UIRegistrySO.cs](D:\Unity Training\story-hornor-game\Assets\Luzart\Utility\Script\UIBase\UIRegistrySO.cs)
- [UIBase.cs](D:\Unity Training\story-hornor-game\Assets\Luzart\Utility\Script\UIBase\UIBase.cs)

Điểm mạnh:

- Dùng `UIRegistrySO` để map `UIName -> prefab/layer/cache`. Đây là hướng đúng hơn hẳn `Resources.Load`.
- Có phân layer root tương đối rõ: screen, popup, HUD, system, toast.
- API gọi đơn giản, dễ dùng cho gameplay code: `ShowUI`, `HideUiActive`, `ShowToast`, `ShowLoading`.
- `UIBase` có vòng đời cơ bản, có `Show`, `Hide`, `RefreshUI`, có hook animation.
- Có cache/non-cache per entry, đủ tốt cho project nhỏ và vừa.

Điểm yếu:

- Vẫn là `UIManager` kiểu monolith. Tất cả loại UI đều nhét vào cùng một active list.
- Chưa có phân lane thực sự. Popup, screen, HUD, loading đều dùng chung logic.
- Chưa có async orchestration. `LoadUI` hiện tại chỉ instantiate prefab local từ registry, chưa có `Addressables`, chưa có race handling.
- Không có queue cho popup hoặc request gating.
- Không có back stack chuẩn cho screen.
- Không có state machine cho view. Trạng thái đang dựa nhiều vào `gameObject.activeSelf`.
- `RefreshUI` kiểu global/static callback dễ sinh side effect khi dự án lớn.

Đánh giá:

- Đây là base tham khảo tốt nhất trong hai hệ Luzart.
- Phần đáng giữ lại nhất là tư duy `registry-driven configuration`.
- Không nên bê nguyên vì nó chưa đủ mạnh cho game online có remote UI assets.

## 2.2. `MARIA`

Tham chiếu chính:

- [UIManager.cs](D:\Unity Training\MARIA\Assets\_GameLuzart\Utility\Script\UIBase\UIManager.cs)
- [UIBase.cs](D:\Unity Training\MARIA\Assets\_GameLuzart\Utility\Script\UIBase\UIBase.cs)

Điểm mạnh:

- API sử dụng ngoài gameplay đơn giản.
- Có khái niệm `scenario` và vài helper cho top bar, next/back.
- Có cache cơ bản.

Điểm yếu lớn:

- `dir` hardcode rất dài trong code, nhét cả metadata, layer, path vào string parse tay.
- Dùng `Resources.Load`, không hợp với pipeline online hiện tại.
- Logic flow game như scenario, top bar, next/back nằm trong `UIManager`. Đây là dấu hiệu manager ôm cả business flow.
- `UIManager` bị phình: quản lý screen, scenario, top UI, loading, toast, path config, back/next policy.
- Maintainability thấp. Mỗi lần thêm UI mới phải sửa enum, sửa dict, sửa flow code.
- `HideAllUiActive(params UIName[] ignoreUI)` viết theo vòng lặp lồng nhau, rất dễ hide nhầm khi có nhiều ignore.
- Không có async safety, không có request dedupe, không có remote asset strategy.

Đánh giá:

- Chỉ nên xem như ví dụ lịch sử của một hệ base đơn giản.
- Không nên dùng làm base cho Ninja School Online.
- Thứ nên tránh rõ nhất là hardcode metadata UI trong code runtime.

## 2.3. `lumber-inc-lfs`

Tham chiếu chính:

- [AbstractPopupController.cs](D:\Unity Main\lumber-inc-lfs\Assets\Screw\Scripts\UI\Popup\AbstractPopupController.cs)
- [PopupContainer.cs](D:\Unity Main\lumber-inc-lfs\Assets\Screw\Scripts\UI\Popup\PopupContainer.cs)
- [Popup.cs](D:\Unity Main\lumber-inc-lfs\Assets\Screw\Scripts\UI\Popup\Popup.cs)
- [PopupQueuer.cs](D:\Unity Main\lumber-inc-lfs\Assets\Scripts\UI\PopupQueuer.cs)
- [SheetController.cs](D:\Unity Main\lumber-inc-lfs\Assets\Screw\Scripts\UI\Sheets\SheetController.cs)

Điểm mạnh nhất:

- Không cố nhét mọi thứ vào một `UIManager`.
- Tách controller theo loại UI:
  - `PopupController` cho modal stack.
  - `SheetController` cho full panel/sheet flow.
  - Các controller specialized như `OfferPopupController`, `OfflinePopupController`, `PaymentPopupController`.
- `PopupContainer` có stack chuẩn:
  - popup hiện tại có thể `Pause`.
  - popup dưới được `Resume`.
  - có `dismissCurrent`, `pauseCurrent`, `DismissAllPopups`.
- `PopupQueuer` là ý tưởng rất đáng học:
  - popup chỉ show khi thỏa context.
  - tránh tranh chấp giữa tutorial, sheet, popup, flow scene.
- `HandleEscape` tách riêng trong controller là đúng tư duy.
- `Popup` có lifecycle khá rõ: `OnBeginShow`, `OnShown`, `OnPaused`, `OnResumed`, `OnBeginDismiss`, `OnDismissed`.
- Cho phép specialized controller cho từng lane thay vì một manager chung đầy `if`.

Điểm yếu:

- Kiến trúc này rất mạnh ở popup/sheet, nhưng không phải một UI framework tổng thể hoàn chỉnh cho mọi loại UI.
- Cấu hình vẫn thiên về prefab reference trong scene/controller hơn là registry tập trung chuẩn cho content online.
- Chưa giải quyết riêng bài toán `Addressables`/remote download của UI show flow.
- Có khá nhiều singleton controller, nếu team không kỷ luật naming và ownership thì trace flow hơi mất công.
- Có nhiều behavior framework-specific, học ý tưởng thì tốt hơn học nguyên xi implementation.

Đánh giá:

- Đây là hệ có chất lượng kiến trúc tốt nhất trong 3 project nếu xét về flow control và separation of concerns.
- Thứ đáng học nhất là:
  - tách lane/controller,
  - stack popup chuẩn,
  - queue/request gating,
  - lifecycle rõ,
  - escape/back handling.
- Nhưng nó chưa phải câu trả lời hoàn chỉnh cho bài toán UI base online của bạn.

## 3. Kết luận so sánh

Nếu phải xếp theo giá trị tham khảo cho base mới:

1. `lumber-inc-lfs`: tốt nhất về kiến trúc điều phối UI.
2. `story-hornor-game`: tốt nhất về config theo registry trong nhóm Luzart.
3. `MARIA`: chỉ nên tham khảo để biết cái gì không nên giữ.

Kết luận thực dụng:

- Không nên copy nguyên 1 base nào.
- Nên lấy:
  - `lumber` cho flow control.
  - `story-hornor` cho registry-driven config.
- Nên bỏ:
  - `Resources.Load`.
  - dict path hardcode trong code.
  - `UIManager` ôm luôn tutorial flow, top bar, scenario, next/back business logic.

## 4. Base nên có cho Ninja School Online

## 4.1. Nguyên tắc thiết kế

Base mới nên bám 7 nguyên tắc:

- `Config-driven`, không hardcode path runtime.
- `Async-first`, vì UI có thể phải chờ content download.
- `Lane-based`, không nhét popup và screen chung một stack.
- `Handle-based`, mọi `ShowAsync` nên trả về handle để quản lý vòng đời.
- `Feature-decoupled`, flow game không được nhét vào manager gốc.
- `Reference-counted`, vì asset remote phải có chiến lược giữ/release.
- `Observable`, phải có event/log để debug.

## 4.2. Phân loại UI

Tôi khuyên chia UI thành 5 lane:

- `Screen`
  - Màn hình chính: login, character, bag, map, dojo, shop chính.
  - Mỗi lane chỉ có 1 active chính.
- `Popup`
  - Modal stack: confirm, reward, settings, mail detail.
  - Có pause/resume như `lumber`.
- `HUD`
  - Thành phần gameplay persistent: HP/MP, quick slot, minimap, chat compact.
  - Không nên đi chung stack với popup.
- `System`
  - Loading, reconnecting, maintenance, force update, full block overlay.
  - Luôn có ưu tiên cao nhất.
- `Toast`
  - Queue riêng, không đi qua popup stack.

Nếu game sau này có social panel hoặc chat panel phức tạp, có thể thêm:

- `Panel`
  - Side panel, bottom sheet, expandable menu.

## 4.3. Kiến trúc đề xuất

### Lớp facade

`UIService`

Đây là API public để gameplay gọi. Nó không trực tiếp instantiate hay quản stack.

Ví dụ API:

```csharp
public interface IUIService
{
    UniTask<UIHandle<T>> ShowAsync<T>(UIId id, UIContext context = default, UIOpenOptions options = default, CancellationToken ct = default)
        where T : UIView;

    UniTask HideAsync(UIId id, UIHideOptions options = default);
    UniTask HideAsync(UIHandle handle, UIHideOptions options = default);

    bool TryGetVisible(UIId id, out UIView view);
    bool IsVisible(UIId id);

    UniTask WarmupAsync(IEnumerable<UIId> ids, CancellationToken ct = default);
    UniTask PreDownloadByLabelAsync(string label, CancellationToken ct = default);

    void ShowToast(string message, ToastStyle style = ToastStyle.Info);
    IDisposable PushBlock(string reason);
}
```

### Registry

`UIRegistryDatabase : ScriptableObject`

Mỗi entry nên có:

- `UIId id`
- `UILane lane`
- `AssetReferenceGameObject prefabRef`
- `bool cache`
- `bool singletonPerLane`
- `bool destroyOnHide`
- `bool preloadAtBoot`
- `bool keepLoadedAcrossScenes`
- `bool requireDownloadedDependencies`
- `bool blockInput`
- `bool allowMultiInstance`
- `int sortPriority`
- `string addressableLabel`
- `UIBackdropPolicy backdropPolicy`
- `UIEscapePolicy escapePolicy`
- `UIPreloadPolicy preloadPolicy`

Ý chính:

- Metadata nằm trong asset config, không nằm trong code.
- Runtime không được parse string kiểu `"1,0,UILogin"`.

### Resource provider

`IUIAssetProvider`

```csharp
public interface IUIAssetProvider
{
    UniTask<UILoadResult> LoadAsync(UIEntry entry, CancellationToken ct);
    UniTask<long> GetDownloadSizeAsync(UIEntry entry, CancellationToken ct);
    UniTask DownloadDependenciesAsync(UIEntry entry, IProgress<float> progress, CancellationToken ct);
    void Retain(UIEntry entry, object owner);
    void Release(UIEntry entry, object owner);
}
```

Implementation nên có:

- `AddressablesUIAssetProvider`
- `LocalPrefabUIAssetProvider`
- nếu thật sự cần, `AssetBundleUIAssetProvider`

Điểm quan trọng:

- `UIService` không biết chi tiết Addressables hay AssetBundle.
- Toàn bộ việc load remote nằm dưới provider.

### Lane controllers

Tách thành các controller độc lập:

- `UIScreenLaneController`
- `UIPopupLaneController`
- `UIHudLaneController`
- `UISystemLaneController`
- `UIToastLaneController`

Lợi ích:

- Mỗi lane có luật riêng.
- Không viết `if (uiType == Popup)` khắp nơi.
- Dễ unit test.

### View base

`UIView : MonoBehaviour`

Không nên giữ `UIBase` quá mỏng như các project cũ. Nên có lifecycle rõ:

```csharp
public abstract class UIView : MonoBehaviour
{
    public UIId Id { get; internal set; }
    public UILane Lane { get; internal set; }
    public UIState State { get; internal set; }

    public virtual UniTask OnCreateAsync(UIContext context) => UniTask.CompletedTask;
    public virtual UniTask OnBeforeShowAsync(UIContext context) => UniTask.CompletedTask;
    public virtual UniTask OnShowAsync(UIContext context) => UniTask.CompletedTask;
    public virtual UniTask OnBeforeHideAsync(UIHideReason reason) => UniTask.CompletedTask;
    public virtual UniTask OnHideAsync(UIHideReason reason) => UniTask.CompletedTask;
    public virtual void OnRelease() {}
}
```

State nên rõ:

- `None`
- `Loading`
- `Loaded`
- `Showing`
- `Visible`
- `Hiding`
- `Hidden`
- `Releasing`
- `Released`

## 4.4. Flow show/hide chuẩn

### `ShowAsync`

Flow nên là:

1. Resolve `UIEntry` từ registry.
2. Route request tới lane phù hợp.
3. Coalesce request nếu cùng `UIId` đang loading.
4. Download dependency nếu thiếu.
5. Load prefab qua provider.
6. Instantiate dưới đúng layer root.
7. Inject `UIContext`.
8. Chạy `OnCreateAsync` nếu là lần đầu.
9. Chạy `OnBeforeShowAsync`.
10. Lane áp luật riêng:
    - screen: replace current hoặc keep previous.
    - popup: push/pause current.
    - system: overlay lên trên.
11. Chạy animation/show.
12. Đăng ký handle, event, back state.

### `HideAsync`

Flow nên là:

1. Validate view hiện còn active.
2. Chạy `OnBeforeHideAsync`.
3. Chạy animation hide.
4. Lane cleanup:
   - popup stack resume popup dưới.
   - screen stack restore nếu có back stack.
5. Nếu `destroyOnHide` thì release instance.
6. Nếu `cache` thì chỉ deactivate và giữ handle.
7. Nếu không còn owner thì provider release asset handle.

## 4.5. Bài toán async và phân luồng

Đây là phần quyết định base có sống lâu được hay không.

### Vấn đề thường gặp

- Người chơi bấm rất nhanh 2 lần mở cùng 1 popup.
- Đang download popup A thì user chuyển scene.
- Popup B phụ thuộc atlas chưa tải xong.
- Server trả state mới làm cùng lúc nhiều UI cần refresh.
- Mất mạng lúc đang load remote UI.

### Cách xử lý nên có

- `Request coalescing`
  - Nếu cùng `UIId` đang load thì share cùng một task.
- `CancellationToken`
  - Mỗi request show/hide phải cancel được.
- `Main thread marshal`
  - Instantiation và Unity API chạy main thread.
- `Task ownership`
  - Lane nào tạo request thì lane đó sở hữu vòng đời request.
- `Soft fail`
  - Nếu load thất bại thì trả `UILoadError`, không để state nửa sống nửa chết.
- `Timeout + retry policy`
  - Chỉ nên đặt ở provider layer, không đặt ở view.

### Dễ phân luồng nghĩa là gì trong UI online

Với game của bạn, "dễ phân luồng" không nên hiểu là cho UI chạy multithread bừa bãi. Nó nên hiểu là:

- Tách `load asset`, `download remote`, `instantiate`, `business decision`, `animation`, `back navigation` thành các bước độc lập.
- Mỗi bước có owner rõ.
- Logic network hoặc asset pipeline không chui vào view script.

## 4.6. Popup stack nên học từ `lumber`

Phần này tôi khuyên bê ý tưởng, không bê code.

Popup lane nên có:

- stack chuẩn `Push/Pop`.
- `pauseCurrent`.
- `dismissCurrent`.
- `resume underlying`.
- `escape/back handling`.
- `queue`.
- `priority`.
- `modal backdrop policy`.

Ví dụ:

```csharp
public interface IPopupLaneController
{
    UniTask<UIHandle> PushAsync(UIEntry entry, UIContext context, UIOpenOptions options, CancellationToken ct);
    UniTask PopAsync(bool animated = true);
    UniTask DismissAllAsync(Predicate<UIView> filter = null);
    UIView Current { get; }
}
```

Nên có thêm `UIPopupQueueService` như `PopupQueuer`:

- popup event offer
- popup reconnect
- popup reward combo
- popup tutorial

Không phải popup nào tới cũng show ngay.

## 4.7. Screen flow không nên làm như `MARIA`

Sai lầm lớn ở `MARIA` là flow gameplay và flow UI trộn vào nhau.

Base mới nên tách:

- `UIService`
  - chỉ quản vòng đời view.
- `FeatureFlowController`
  - ví dụ `LoginFlowController`, `CharacterFlowController`, `GuildFlowController`.
- `StorySequenceController`
  - nếu có tutorial hoặc cutscene flow.

Nói ngắn:

- `UIManager` không nên biết `Tut1`, `Level1_3_2`, `CheckShowBtnBack`.
- Đó là việc của flow/controller cấp feature.

## 4.8. Cấu hình phù hợp `Addressables` + bundle remote

Vì game của bạn có tải data từ bundle, UI base nên chuẩn bị cho 3 mode:

- `Local resident`
  - UI luôn có sẵn trong app: login, reconnect, maintenance.
- `Addressables cached remote`
  - tải lần đầu, cache local, show lần sau nhanh.
- `Hot update remote`
  - content có thể đổi mà không cần build app mới.

Khuyến nghị:

- Tất cả prefab UI lớn nên đi qua `AssetReferenceGameObject`.
- Các atlas/font/material dùng chung nên preload theo label.
- `UIEntry` nên có `addressableLabel` để preload theo cụm:
  - `ui_login`
  - `ui_inventory`
  - `ui_shop`
  - `ui_social`
  - `ui_worldmap`

### Chính sách preload

- Boot:
  - `ReconnectPopup`
  - `MaintenancePopup`
  - `ForceUpdatePopup`
  - `LoadingOverlay`
  - `Toast`
- Login xong:
  - `MainHUD`
  - `BagScreen`
  - `CharacterScreen`
  - `MailPopup`
- Vào map cụ thể:
  - preload UI đặc thù map nếu cần

### Chính sách release

- Popup hiếm:
  - release khi hide.
- Popup thường xuyên:
  - giữ instance cache.
- Screen nặng:
  - giữ asset handle nhưng destroy instance nếu memory pressure cao.
- HUD:
  - persistent.

## 4.9. Handle và ownership

Tôi khuyên mọi `ShowAsync` trả về `UIHandle`.

```csharp
public readonly struct UIHandle
{
    public UIId Id { get; }
    public UIView View { get; }
    public int InstanceId { get; }
    public bool IsValid { get; }
}
```

Lợi ích:

- Không phải đi tìm bằng `UIName` suốt.
- Có thể hide đúng instance khi popup cho phép multi-instance.
- Dễ debug leaked UI.

## 4.10. Event và debug

Base ngon phải dễ debug.

Nên có event:

- `OnUILoadStarted`
- `OnUILoadCompleted`
- `OnUILoadFailed`
- `OnUIShown`
- `OnUIHidden`
- `OnUIReleased`
- `OnLaneStateChanged`

Nên có debug window:

- danh sách UI đang visible
- UI đang loading
- handle count
- addressable handle count
- queue popup
- memory cost ước lượng
- last error

## 4.11. Editor tooling

Đây là phần thường bị thiếu nhưng rất đáng đầu tư.

Nên có editor tool để:

- validate duplicate `UIId`
- validate thiếu `prefabRef`
- validate layer không hợp lệ
- validate entry vừa `cache` vừa `allowMultiInstance`
- validate popup mà `escapePolicy` sai
- generate class hằng số `UIIds`
- generate report các asset UI theo label

Nếu làm tốt editor tool, chi phí bảo trì sẽ giảm mạnh hơn bất kỳ trick runtime nào.

## 5. Thiết kế đề xuất cuối cùng

## 5.1. Hệ nên chọn

Nên chọn hybrid architecture:

- `story-hornor` cho `registry-driven config`
- `lumber` cho `lane controller + popup stack + queue`
- không dùng kiến trúc `MARIA`

## 5.2. Sơ đồ trách nhiệm

```text
Gameplay / Feature Controller
        |
        v
      UIService
        |
        +-- UIRegistryDatabase
        +-- IUIAssetProvider
        +-- UIScreenLaneController
        +-- UIPopupLaneController
        +-- UIHudLaneController
        +-- UISystemLaneController
        +-- UIToastLaneController
        +-- UIBlockService
        +-- UIPopupQueueService
```

## 5.3. Những gì tuyệt đối không nhét vào base manager

- tutorial step list
- top bar business logic
- rule next/back riêng của từng feature
- server packet parsing
- inventory refresh logic
- scene quest flow
- string path hardcode

## 5.4. Những gì phải có trong base

- registry
- async load orchestration
- lane separation
- popup stack
- queue
- blocker/reference counter
- typed handle
- cache policy
- release policy
- diagnostics

## 6. Roadmap implement đề xuất

## Phase 1: Foundation

- Tạo `UIId`, `UILane`, `UIState`, `UIContext`, `UIEntry`.
- Tạo `UIRegistryDatabase`.
- Tạo `UIView`.
- Tạo `IUIAssetProvider` và implementation Addressables.
- Tạo `UIService`.

## Phase 2: Lane controllers

- `UIScreenLaneController`
- `UIPopupLaneController`
- `UISystemLaneController`
- `UIToastLaneController`

## Phase 3: Reliability

- request dedupe
- cancellation
- preload labels
- release policy
- debug panel
- validation editor

## Phase 4: Feature integration

- login flow
- main HUD
- inventory/bag
- mail
- shop
- reconnect/maintenance/update popups

## 7. Kết luận chốt

Nếu hỏi "hệ nào ngon nhất" trong 3 project:

- Về tư duy kiến trúc: `lumber-inc-lfs` ngon nhất.
- Về cách config dễ hiểu trong nhóm UIManager kiểu cũ: `story-hornor-game` ổn nhất.
- Về base để dùng trực tiếp cho game online mới: không cái nào đủ ngon nếu giữ nguyên.

Base tốt nhất cho Ninja School Online nên là:

- registry-driven như `story-hornor`,
- lane/controller-driven như `lumber`,
- async asset provider cho `Addressables`,
- có popup queue, handle, state machine, preload/release policy,
- và tuyệt đối không để gameplay flow chảy ngược vào `UIManager`.

Nếu đi đúng hướng này, bạn sẽ có một base:

- dễ mở rộng khi số lượng UI tăng,
- dễ sửa khi thay asset pipeline,
- dễ phân trách nhiệm cho team,
- và đủ bền cho game online chạy lâu.

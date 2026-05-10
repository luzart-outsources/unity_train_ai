# NinjaUI — Hướng dẫn sử dụng chi tiết

> File này là cookbook thực tế cho **Ninja School Online**. Đọc theo thứ tự nếu mới, tra cứu theo scenario nếu đã quen.

---

## Mục lục

- [0. Setup ban đầu](#0-setup-ban-đầu)
- [1. Viết 1 UI mới từ đầu](#1-viết-1-ui-mới-từ-đầu)
- [2. Scenario: Login flow](#2-scenario-login-flow)
- [3. Scenario: Mở Inventory + stack với Shop](#3-scenario-mở-inventory--stack-với-shop)
- [4. Scenario: Daily reward chain (popup queue)](#4-scenario-daily-reward-chain-popup-queue)
- [5. Scenario: Reconnect / Disconnect](#5-scenario-reconnect--disconnect)
- [6. Scenario: Server-driven popup](#6-scenario-server-driven-popup)
- [7. Scenario: Tutorial sequence](#7-scenario-tutorial-sequence)
- [8. Scenario: Combat HUD + damage number](#8-scenario-combat-hud--damage-number)
- [9. Scenario: Shop mua item với confirm](#9-scenario-shop-mua-item-với-confirm)
- [10. Scenario: Chat với panel có thể pin](#10-scenario-chat-với-panel-có-thể-pin)
- [11. Testing & Mock](#11-testing--mock)
- [12. Cách thêm loại UI mới (enum UIId)](#12-cách-thêm-loại-ui-mới)
- [13. Patterns & Anti-patterns](#13-patterns--anti-patterns)

---

## 0. Setup ban đầu

### 0.0 Namespace

Tất cả type của framework (`UIBase`, `UIBase<TData>`, `UIManager`, `UIContext`, `UIId`, `UIConfig`, `UIHandle`, ...) đều nằm trong **`namespace Luzart`**. Mọi file UI tự viết đều cần:

```csharp
using Luzart;
```

Tên product và các identifier hiển thị cho user (thư mục `NinjaUI/`, assembly `NinjaUI.Runtime`, Unity menu `Tools → NinjaUI → ...`, `Create → NinjaUI → UI Registry`, log prefix `[NinjaUI]`) **không đổi** — chỉ C# namespace đổi sang `Luzart` cho đồng nhất với các package khác trong `Assets/Luzart/` (TweenAnimation, Attributes, Editor drawers...).

### 0.1 Cài dependency

Trong `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.unity.addressables": "1.21.19",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask"
  }
}
```

### 0.2 Copy thư mục `NinjaUI/` vào `Assets/`

```
Assets/
└── NinjaUI/
    ├── Runtime/
    ├── Editor/
    ├── Samples/
    └── README.md
```

### 0.3 Tạo scene hierarchy

```
MainScene
└── UIRoot (Canvas, Screen Space - Overlay, sort order 0)
    ├── 0_WorldOverlay (RectTransform, anchors stretch)
    ├── 1_Screen
    ├── 2_Hud
    ├── 3_Popup
    ├── 4_System
    ├── 5_Toast
    ├── UIBlockerOverlay (Image full-screen đen alpha 0.01, inactive mặc định)
    ├── UIManager (GameObject, component UIManager + UIInputRouter + UIBlockService)
    └── EventSystem
```

**Lưu ý**: UIBlockerOverlay đặt giữa layer System và Toast (sort order giữa để block nhưng Toast vẫn hiện trên).

### 0.4 Tạo UIRegistrySO

1. Right-click Project → Create → NinjaUI → UI Registry.
2. Đặt tên `UIRegistry.asset` trong `Assets/Data/`.
3. Mở asset trong Inspector.
4. Thêm entries (xem cheatsheet trong README).

### 0.5 Mark prefab thành Addressable

Với mỗi prefab UI:
1. Tick "Addressable" trong Inspector (góc phải trên).
2. Đặt address dạng: `ui/popup/inventory`, `ui/system/loading`, ...
3. Optional: assign label (`ui_boot`, `ui_login`, `ui_gameplay`).
4. Window → Asset Management → Addressables → Groups → Build → New Build → Default Build Script.

### 0.6 Gán references vào UIManager

Inspector của UIManager GameObject:
- **Registry**: UIRegistry.asset.
- **WorldOverlay/Screen/Hud/Popup/System/Toast Root**: 6 RectTransform.
- **Input Router**: component UIInputRouter trên cùng GameObject.
- **Block Service**: component UIBlockService trên cùng GameObject.
- **Preload On Start**: ✓.
- **Boot Preload Labels**: `["ui_boot"]`.

---

## 1. Viết 1 UI mới từ đầu

### 1.1 Tạo data class (nếu UI cần data)

```csharp
// Assets/Scripts/UI/Inventory/InventoryData.cs
using System.Collections.Generic;

public class InventoryData
{
    public int Gold;
    public int Diamond;
    public List<ItemData> Items;
    public int MaxSlots;
}

public class ItemData
{
    public int Id;
    public string Name;
    public int Quantity;
    public ItemType Type;
}
```

### 1.2 Tạo UI class kế thừa `UIBase<TData>`

```csharp
// Assets/Scripts/UI/Inventory/InventoryUI.cs
using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : UIBase<InventoryData>
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI txtGold;
    [SerializeField] private TextMeshProUGUI txtDiamond;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private InventorySlot itemSlotPrefab;
    [SerializeField] private Button btnClose;
    [SerializeField] private Button btnShop;
    [SerializeField] private Button btnSort;

    private readonly List<InventorySlot> slotPool = new();

    // Setup refs + subscribe events 1 lần.
    protected override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
    {
        btnClose.onClick.AddListener(OnCloseButtonClicked);
        btnShop.onClick.AddListener(OnShopClicked);
        btnSort.onClick.AddListener(OnSortClicked);
        return UniTask.CompletedTask;
    }

    // Bind data mỗi lần show.
    protected override UniTask OnBeforeShowAsync(InventoryData data, CancellationToken ct)
    {
        if (data == null) return UniTask.CompletedTask;

        txtGold.text = data.Gold.ToString("N0");
        txtDiamond.text = data.Diamond.ToString("N0");
        BindItems(data.Items);

        // Subscribe event game (gold change, item pickup...)
        PlayerEvents.OnGoldChanged += HandleGoldChanged;
        return UniTask.CompletedTask;
    }

    protected override UniTask OnHiddenAsync(InventoryData data, UIHideReason reason, CancellationToken ct)
    {
        // Unsubscribe để tránh leak
        PlayerEvents.OnGoldChanged -= HandleGoldChanged;
        return UniTask.CompletedTask;
    }

    private void BindItems(List<ItemData> items)
    {
        // Pool/reuse slots
        while (slotPool.Count < items.Count)
            slotPool.Add(Instantiate(itemSlotPrefab, itemContainer));

        for (int i = 0; i < slotPool.Count; i++)
        {
            if (i < items.Count)
            {
                slotPool[i].gameObject.SetActive(true);
                slotPool[i].Bind(items[i]);
            }
            else
            {
                slotPool[i].gameObject.SetActive(false);
            }
        }
    }

    private void HandleGoldChanged(int newGold)
    {
        txtGold.text = newGold.ToString("N0");
    }

    private async void OnShopClicked()
    {
        // Inventory sẽ tự pause nhờ PausableWhenOverlaid + Exclusive logic.
        var shopData = ShopService.GetCurrentShopData();
        await UIManager.Instance.ShowAsync<ShopUI>(UIId.Shop, new UIContext(shopData),
            ct: this.GetCancellationTokenOnDestroy());
    }

    private void OnSortClicked()
    {
        Data.Items.Sort((a, b) => a.Type.CompareTo(b.Type));
        BindItems(Data.Items);
    }
}
```

### 1.3 Thiết kế prefab

- Root GameObject: `InventoryUI` + component `InventoryUI` (class vừa viết). Thêm `CanvasGroup` nếu subclass override `AnimateShowAsync` để fade (không bắt buộc bởi base).
- Layout bên trong tự do.
- Kéo các ref vào Inspector.

### 1.4 Mark Addressable

- Address: `ui/popup/inventory`.
- Label: `ui_gameplay`.

### 1.5 Thêm entry vào UIRegistrySO

| Field | Value |
|---|---|
| Id | Inventory |
| StringId | `inventory` |
| AssetRef | (kéo prefab vào) |
| Lane | Popup |
| CachePolicy | KeepLoaded |
| PreloadOnBoot | false |
| AllowMultiInstance | false |
| DismissByEscape | true |
| PausableWhenOverlaid | true |

> Setup visual (backdrop mờ nền, blocksRaycasts, animation show/hide) thuộc về prefab — gắn `CanvasGroup` trên root cho blocksRaycasts, thêm Image child cho backdrop, override `AnimateShowAsync/AnimateHideAsync` cho animation.

### 1.6 Gọi từ gameplay

```csharp
public class PlayerController : MonoBehaviour
{
    private async void OnInventoryKeyPressed()
    {
        var data = InventoryService.GetPlayerInventory();
        await UIManager.Instance.ShowAsync<InventoryUI>(
            UIId.Inventory,
            new UIContext(data),
            ct: this.GetCancellationTokenOnDestroy());
    }
}
```

---

## 2. Scenario: Login flow

**Yêu cầu**: Splash → Login → CharacterSelect → MainMenu (Lobby).

### 2.1 Config

| UI | Lane | CachePolicy | Preload | Notes |
|---|---|---|---|---|
| Splash | Screen | ReleaseOnClose | ✓ (bắt buộc show đầu tiên) | Destroy sau login |
| Login | Screen | ReleaseOnClose | ✗ | Chỉ login 1 lần |
| CharacterSelect | Screen | PoolOnClose | ✗ | User có thể quay lại chọn char khác |
| MainMenu | Screen | KeepLoaded | ✗ | Mở thường xuyên |

### 2.2 Controller flow

```csharp
public class LoginFlowController : MonoBehaviour
{
    private async void Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        // 1. Splash
        var splash = await UIManager.Instance.ShowAsync<SplashUI>(UIId.Splash, ct: ct);
        await UniTask.Delay(2000, cancellationToken: ct);

        // 2. Preload login group trong khi splash đang hiện
        using (UIManager.Instance.PushBlock("preload_login"))
        {
            await UIManager.Instance.PreloadByLabelAsync("ui_login", ct: ct);
        }

        // 3. Show Login (Screen exclusive → Splash tự replace)
        await UIManager.Instance.HideAsync(UIId.Splash, ct: ct);
        await UIManager.Instance.ShowAsync<LoginUI>(UIId.Login, ct: ct);

        // 4. Chờ user login xong (LoginUI raise event)
        var userData = await LoginUI.WaitForLoginResultAsync(ct);

        // 5. Preload lobby group
        using (UIManager.Instance.PushBlock("preload_lobby"))
        {
            await UIManager.Instance.PreloadByLabelAsync("ui_gameplay", ct: ct);
        }

        // 6. CharacterSelect
        var selectData = new CharacterSelectData { Characters = userData.Characters };
        await UIManager.Instance.ShowAsync<CharacterSelectUI>(UIId.CharacterSelect,
            new UIContext(selectData), ct: ct);

        // 7. Chờ chọn character
        var charId = await CharacterSelectUI.WaitForSelectionAsync(ct);

        // 8. Enter lobby
        await UIManager.Instance.ShowAsync<MainMenuUI>(UIId.MainMenu,
            new UIContext(new MainMenuData { CharacterId = charId }), ct: ct);
    }
}
```

---

## 3. Scenario: Mở Inventory + stack với Shop

**Yêu cầu**: User mở Inventory → bấm button "Shop" → Shop mở đè lên, Inventory pause giữ state → đóng Shop → Inventory tự resume, scroll position giữ nguyên.

### 3.1 Flow

```csharp
// Inventory đang mở, user bấm button Shop bên trong:
private async void OnShopButtonClicked()
{
    // Show Shop. Vì Popup lane + PausableWhenOverlaid=true → Inventory tự pause.
    await UIManager.Instance.ShowAsync<ShopUI>(UIId.Shop,
        new UIContext(ShopService.Get()),
        ct: this.GetCancellationTokenOnDestroy());
}

// User bấm close trên Shop → base tự hide Shop → Inventory tự resume.
// Không cần code thêm.
```

### 3.2 Kiểm chứng

- Inventory scroll về giữa list → mở Shop → đóng Shop → Inventory vẫn ở giữa list. ✓
- Inventory có countdown timer → mở Shop → timer pause → đóng Shop → timer resume.

Làm sao? Override `OnPauseAsync` / `OnResumeAsync`:
```csharp
public class InventoryUI : UIBase<InventoryData>
{
    public override UniTask OnPauseAsync(CancellationToken ct)
    {
        timerCoroutine?.Stop();
        return UniTask.CompletedTask;
    }

    public override UniTask OnResumeAsync(CancellationToken ct)
    {
        timerCoroutine = StartTimer();
        return UniTask.CompletedTask;
    }
}
```

---

## 4. Scenario: Daily reward chain (popup queue)

**Yêu cầu**: User login vào → show tuần tự: Attendance → Daily bonus → 7-day login gift → Event offer. Không chồng. User đóng popup này mới qua popup kế tiếp.

### 4.1 Config

Mỗi UI: `Lane = Popup`, `CachePolicy = ReleaseOnClose`. Queue là opt-in ở call site: gọi qua `EnqueuePopupAsync(id, ctx, priority)` thay vì `ShowAsync` → UI sẽ show tuần tự qua `UIPopupQueue`, không chồng popup khác trong queue.

### 4.2 Code flow

```csharp
public class DailyRewardFlowController : MonoBehaviour
{
    public async UniTask RunAfterLoginAsync(CancellationToken ct)
    {
        var ui = UIManager.Instance;
        var data = await RewardService.FetchPendingRewardsAsync(ct);

        // Enqueue tuần tự. Mỗi EnqueueAsync trả task, await task = chờ popup đóng.
        if (data.HasAttendance)
            ui.EnqueuePopupAsync(UIId.Attendance, new UIContext(data.Attendance), ct: ct).Forget();

        if (data.HasDailyBonus)
            ui.EnqueuePopupAsync(UIId.DailyBonus, new UIContext(data.DailyBonus), ct: ct).Forget();

        if (data.Has7DayGift)
            ui.EnqueuePopupAsync(UIId.SevenDayGift, new UIContext(data.SevenDay), ct: ct).Forget();

        // Event offer priority cao hơn → chèn lên đầu queue
        if (data.HasEventOffer)
            ui.EnqueuePopupAsync(UIId.EventOffer, new UIContext(data.EventOffer),
                priority: 100, ct: ct).Forget();

        // Chờ queue xử lý xong
        var tcs = new UniTaskCompletionSource();
        ui.PopupQueue.OnQueueEmpty += () => tcs.TrySetResult();
        await tcs.Task;

        Debug.Log("All daily popups shown, unlock gameplay");
    }
}
```

### 4.3 Edge case

- **User click "skip all"**: `ui.PopupQueue.Clear()`.
- **User disconnect giữa chừng**: `ct.Cancel()` → queue tự drop pending + cancel current.
- **Popup trong queue fail load**: task throw exception, queue tiếp tục item sau.

---

## 5. Scenario: Reconnect / Disconnect

**Yêu cầu**: Mất mạng → show DisconnectUI với spinner + countdown → reconnect thành công → auto close → lỗi → show ForceUpdateUI.

### 5.1 Config

| UI | Lane | CachePolicy | Preload | DismissByEscape |
|---|---|---|---|---|
| Disconnect | System | KeepLoaded | ✓ (boot preload) | ✗ |
| ForceUpdate | System | KeepLoaded | ✓ (boot preload) | ✗ |

**Bắt buộc PreloadOnBoot** — không thể download asset lúc đã mất mạng.

### 5.2 Network handler

```csharp
public class NetworkController : MonoBehaviour
{
    private UIHandle disconnectHandle;

    private async void OnConnectionLost()
    {
        disconnectHandle = await UIManager.Instance.ShowAsync<DisconnectUI>(
            UIId.Disconnect,
            new UIContext(new DisconnectData { AutoRetrySeconds = 5 }));

        var success = await RetryConnectionAsync(maxAttempts: 3);

        if (success)
        {
            await UIManager.Instance.HideAsync(disconnectHandle);
            UIManager.Instance.ShowToastAsync("Đã kết nối lại", ToastStyle.Success).Forget();
        }
        else
        {
            await UIManager.Instance.HideAsync(disconnectHandle);
            await UIManager.Instance.ShowAsync<ForceUpdateUI>(UIId.ForceUpdate);
        }
    }
}
```

---

## 6. Scenario: Server-driven popup

**Yêu cầu**: Server gửi packet `{"type":"guild_invite","data":{...}}`. Client show đúng popup tương ứng mà không cần biết compile-time.

### 6.1 Map trong UIRegistrySO

| Id | StringId | Prefab |
|---|---|---|
| GuildInvite | `guild_invite` | GuildInvitePopup |
| PartyInvite | `party_invite` | PartyInvitePopup |
| Announcement | `announcement` | AnnouncementPopup |

### 6.2 Handler

```csharp
public class ServerPushHandler
{
    public async void HandlePushMessage(string json)
    {
        var msg = JsonUtility.FromJson<ServerPushMessage>(json);

        try
        {
            await UIManager.Instance.ShowByStringIdAsync(msg.type,
                new UIContext(msg.data));
        }
        catch (KeyNotFoundException)
        {
            Debug.LogWarning($"Server push type '{msg.type}' chưa có trong UIRegistry. Bỏ qua.");
        }
    }
}

[Serializable]
public class ServerPushMessage
{
    public string type;
    public string data; // raw JSON, parse ở UI tương ứng
}
```

### 6.3 UI tự parse data

```csharp
public class GuildInviteUI : UIBase<string> // raw JSON
{
    protected override UniTask OnBeforeShowAsync(string rawJson, CancellationToken ct)
    {
        var invite = JsonUtility.FromJson<GuildInviteData>(rawJson);
        txtGuildName.text = invite.GuildName;
        txtInviter.text = invite.InviterName;
        return UniTask.CompletedTask;
    }
}
```

---

## 7. Scenario: Tutorial sequence

**Yêu cầu**: User lần đầu vào game → tutorial 5 bước. Mỗi bước: show popup hướng dẫn + highlight UI target + chờ user làm action → qua bước sau.

### 7.1 Config

| UI | Lane | Queue | Dismiss | Backdrop |
|---|---|---|---|---|
| TutorialStep | Popup | ✓ (priority cao) | ✗ (bắt buộc theo) | TransparentBlock |

### 7.2 Controller

```csharp
public class TutorialController
{
    private readonly TutorialStep[] steps = {
        new() { StepId = 1, Title = "Di chuyển", TargetUI = "move_joystick" },
        new() { StepId = 2, Title = "Tấn công", TargetUI = "attack_button" },
        new() { StepId = 3, Title = "Mở bag", TargetUI = "inventory_button" },
        new() { StepId = 4, Title = "Nhặt item", TargetUI = null },
        new() { StepId = 5, Title = "Học skill", TargetUI = "skill_panel" },
    };

    public async UniTask RunAsync(CancellationToken ct)
    {
        foreach (var step in steps)
        {
            // Enqueue với priority cao để override daily popup
            var handle = await UIManager.Instance.EnqueuePopupAsync(
                UIId.TutorialStep,
                new UIContext(step),
                priority: 1000,
                ct: ct);

            // Chờ user hoàn thành action (UI gọi tcs.SetResult khi xong)
            await step.CompletionTask;

            await UIManager.Instance.HideAsync(handle);
            UserData.SaveTutorialProgress(step.StepId);
        }

        UIManager.Instance.ShowToastAsync("Hoàn thành hướng dẫn!", ToastStyle.Success).Forget();
    }
}
```

---

## 8. Scenario: Combat HUD + damage number

**Yêu cầu**: HUD luôn hiện (HP/MP/minimap). Damage number bật tắt nhanh (mỗi đòn đánh spawn 1 text).

### 8.1 Config

| UI | Lane | CachePolicy | AllowMulti | Exclusive |
|---|---|---|---|---|
| GameplayHud | Hud | KeepLoaded | ✗ | ✗ |
| DamageNumber | WorldOverlay | KeepLoaded | ✓ | ✗ |

### 8.2 HUD

```csharp
public class GameplayHudUI : UIBase<GameplayHudData>
{
    protected override UniTask OnShownAsync(GameplayHudData data, CancellationToken ct)
    {
        CombatEvents.OnHpChanged += HandleHpChanged;
        CombatEvents.OnMpChanged += HandleMpChanged;
        return UniTask.CompletedTask;
    }
    // ... không bao giờ hide trong combat
}
```

Show 1 lần lúc enter combat scene:
```csharp
await UIManager.Instance.ShowAsync<GameplayHudUI>(UIId.GameplayHud,
    new UIContext(HudData.Create()));
```

### 8.3 Damage number (dùng object pool trong UI)

Vì `DamageNumber` có thể spawn 20 lần/giây → **KHÔNG** dùng ShowAsync cho mỗi damage. Thay vào đó, có 1 `DamageNumberLayer` (show 1 lần khi vào combat) quản pool bên trong:

```csharp
public class DamageNumberLayer : UIBase
{
    [SerializeField] private DamageText textPrefab;
    private Queue<DamageText> pool = new();

    public void SpawnDamage(Vector3 worldPos, int damage, DamageType type)
    {
        var txt = pool.Count > 0 ? pool.Dequeue() : Instantiate(textPrefab, transform);
        txt.Play(worldPos, damage, type, onComplete: () => pool.Enqueue(txt));
    }
}

// Gameplay code
var layer = UIManager.Instance.TryGetVisible(UIId.DamageNumberLayer, out var v)
    ? v as DamageNumberLayer : null;
layer?.SpawnDamage(hitPos, 123, DamageType.Critical);
```

**Rule**: UIManager chỉ quản top-level UI. Chi tiết nội bộ (damage number, chat message, slot) → UI tự quản pool bên trong.

---

## 9. Scenario: Shop mua item với confirm

**Yêu cầu**: Inventory → Shop → click "Mua" → Confirm dialog → chờ server → toast success.

### 9.1 Flow code

```csharp
public class ShopUI : UIBase<ShopData>
{
    private async void OnBuyClicked(ItemData item)
    {
        // 1. Confirm dialog (đè lên Shop, Shop pause)
        var confirmData = new ConfirmData {
            Title = "Xác nhận mua",
            Message = $"Bạn có chắc mua {item.Name} với giá {item.Price} gold?",
            YesText = "Mua",
            NoText = "Huỷ"
        };
        var confirm = await UIManager.Instance.ShowAsync<ConfirmDialogUI>(
            UIId.Confirm, new UIContext(confirmData));
        var result = await confirm.WaitForResultAsync();

        if (!result) return;

        // 2. Block input + gọi server
        using (UIManager.Instance.PushBlock($"buying_{item.Id}"))
        {
            try
            {
                await ShopService.PurchaseAsync(item.Id,
                    ct: this.GetCancellationTokenOnDestroy());

                UIManager.Instance.ShowToastAsync(
                    $"Đã mua {item.Name}!", ToastStyle.Success).Forget();
            }
            catch (NotEnoughGoldException)
            {
                UIManager.Instance.ShowToastAsync(
                    "Không đủ vàng", ToastStyle.Error).Forget();
            }
            catch (Exception e)
            {
                Debug.LogError($"Purchase failed: {e}");
                UIManager.Instance.ShowToastAsync(
                    "Lỗi hệ thống, thử lại sau", ToastStyle.Error).Forget();
            }
        } // block tự release
    }
}
```

---

## 10. Scenario: Chat với panel có thể pin

**Yêu cầu**: Chat có 2 mode: mini (HUD, luôn hiện) và expanded (popup, user mở ra). Expanded có thể pin để không auto close.

### 10.1 Config 2 UI

| UI | Lane | CachePolicy |
|---|---|---|
| ChatMini | Hud | KeepLoaded |
| ChatExpanded | Popup | KeepLoaded |

### 10.2 Share state

```csharp
public class ChatStateSingleton
{
    public List<ChatMessage> Messages = new();
    public bool IsPinned;
    public event Action OnNewMessage;
}

public class ChatMiniUI : UIBase
{
    public override UniTask OnShownAsync(UIContext ctx, CancellationToken ct)
    {
        ChatStateSingleton.Instance.OnNewMessage += Refresh;
        return UniTask.CompletedTask;
    }

    public async void OnExpandClicked()
    {
        await UIManager.Instance.ShowAsync<ChatExpandedUI>(UIId.ChatExpanded);
        // Mini vẫn hiện (khác lane Popup vs Hud, không pause nhau)
    }
}
```

---

## 11. Testing & Mock

### 11.1 Mock IUIAssetProvider

```csharp
public class MockUIAssetProvider : IUIAssetProvider
{
    public Dictionary<UIId, GameObject> Prefabs = new();

    public UniTask<GameObject> LoadAsync(UIConfig c, CancellationToken ct)
        => UniTask.FromResult(Prefabs[c.Id]);

    public void Release(UIConfig c) { }
    public UniTask<long> GetDownloadSizeAsync(UIConfig c, CancellationToken ct)
        => UniTask.FromResult(0L);
    public UniTask PreloadAsync(UIConfig c, IProgress<float> p, CancellationToken ct)
        => UniTask.CompletedTask;
    public UniTask PreloadByLabelAsync(string label, IProgress<float> p, CancellationToken ct)
        => UniTask.CompletedTask;
}
```

### 11.2 Test show/hide

```csharp
[Test]
public async Task ShowAsync_WhenValidId_ReturnsHandle()
{
    // Arrange
    var mockProvider = new MockUIAssetProvider();
    mockProvider.Prefabs[UIId.Inventory] = CreateInventoryPrefab();

    var manager = SetupTestUIManager(mockProvider);

    // Act
    var handle = await manager.ShowAsync(UIId.Inventory);

    // Assert
    Assert.IsTrue(handle.IsValid);
    Assert.IsTrue(manager.IsVisible(UIId.Inventory));
}
```

### 11.3 Test popup queue tuần tự

```csharp
[Test]
public async Task Queue_ShowsPopupsSequentially()
{
    var manager = SetupTestUIManager();
    var order = new List<UIId>();

    manager.OnShown += view => order.Add(view.Id);

    manager.EnqueuePopupAsync(UIId.Attendance).Forget();
    manager.EnqueuePopupAsync(UIId.DailyBonus).Forget();
    manager.EnqueuePopupAsync(UIId.EventOffer, priority: 100).Forget();

    // Chờ queue xong
    await UniTask.WaitUntil(() => !manager.PopupQueue.IsProcessing && manager.PopupQueue.PendingCount == 0);

    // EventOffer priority cao → chạy trước
    CollectionAssert.AreEqual(
        new[] { UIId.EventOffer, UIId.Attendance, UIId.DailyBonus },
        order);
}
```

---

## 12. Cách thêm loại UI mới

Bước:

1. **Thêm vào enum `UIId`** (theo quy ước số):
   ```csharp
   public enum UIId
   {
       // ...
       GuildHall = 2010, // Popup range
   }
   ```

2. **Tạo prefab** + mark Addressable (`ui/popup/guild_hall`, label `ui_guild`).

3. **Tạo class** `GuildHallUI : UIBase<GuildHallData>`.

4. **Thêm entry vào `UIRegistrySO`** asset.

5. **Run validator**: Tools → NinjaUI → Validate All Registries.

6. **Build Addressables**.

7. **Gọi**: `UIManager.Instance.ShowAsync<GuildHallUI>(UIId.GuildHall, ctx)`.

---

## 13. Patterns & Anti-patterns

### ✓ DO

- **Pass CancellationToken** từ scope phù hợp.
- **Subscribe events** trong `OnBeforeShowAsync`, **unsubscribe** trong `OnHiddenAsync`.
- **Setup refs + listeners** trong `OnCreateAsync` (1 lần).
- **Dùng `UIBase<TData>`** cho UI có data → type-safe.
- **Preload theo group** qua label, không preload từng cái.
- **PushBlock với reason rõ ràng** để debug.
- **Return UniHandle** từ method để caller hide được đúng instance.
- **Dùng object pool bên trong UI** cho child element bật tắt liên tục.

### ✗ DON'T

- **Đừng** `Resources.Load` trong UIBase.
- **Đừng** gọi `UIManager.Instance` trong `Awake` của UI (UIManager chưa chắc đã init).
- **Đừng** nhét business logic (server call, save data) vào UIBase — để ở service layer.
- **Đừng** gọi `ShowAsync` trong `Update`/`OnGUI` — lag + race condition.
- **Đừng** subscribe global event trong `OnCreateAsync` — sẽ leak khi hide (vì OnCreate chỉ 1 lần).
- **Đừng** dùng Coroutine trong UIBase — dùng UniTask.
- **Đừng** `Destroy(uiView.gameObject)` thủ công — để UIManager xử lý qua CachePolicy.
- **Đừng** nhét tutorial step list, scenario flow vào UIManager — đó là feature controller.
- **Đừng** 1 enum UIId chung cho 100+ UI nếu dev không maintain được — split thành nhiều registry.

### Khi nào ở dạng `UIBase` vs `UIBase<TData>`?

| Case | Chọn |
|---|---|
| UI không có data động (HUD, spinner, MainMenu button bar) | `UIBase` |
| UI cần data mỗi lần show (Inventory, Shop, Confirm) | `UIBase<TData>` |
| UI có data static load 1 lần từ service | `UIBase` + gọi service trong `OnBeforeShowAsync` |
| UI dùng cho server push (string JSON) | `UIBase<string>` rồi parse JSON bên trong |

---

## Tham chiếu nhanh

- Phân tích 3 project nguồn: [`01-ui-manager-analysis.md`](./01-ui-manager-analysis.md)
- Thiết kế kiến trúc: [`02-ui-manager-design.md`](./02-ui-manager-design.md)
- README với API reference: [`../NinjaUI/README.md`](../NinjaUI/README.md)
- Sample code: [`../NinjaUI/Samples/InventoryUI.Example.cs`](../NinjaUI/Samples/InventoryUI.Example.cs)

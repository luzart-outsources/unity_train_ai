// EXAMPLE — KHÔNG build vào runtime. Minh họa cách dùng base.
// Di chuyển vào Scripts/UI/ nếu muốn dùng, hoặc tham khảo pattern rồi xóa file này.

using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    /// <summary>Data model truyền vào Inventory UI khi mở.</summary>
    public class InventoryData
    {
        public int Gold;
        public int Diamond;
        public System.Collections.Generic.List<ItemInfo> Items;
    }

    public class ItemInfo
    {
        public int Id;
        public string Name;
        public int Quantity;
    }

    /// <summary>
    /// Example UI kế thừa UIBase&lt;TData&gt; — type-safe data binding.
    /// </summary>
    public class InventoryUI : UIBase<InventoryData>
    {
        [Header("Refs")]
        [SerializeField] private TextMeshProUGUI txtGold;
        [SerializeField] private TextMeshProUGUI txtDiamond;
        [SerializeField] private Transform itemRoot;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnShop;

        public override UniTask OnCreateAsync(UIContext ctx, CancellationToken ct)
        {
            // Chỉ chạy 1 lần cả vòng đời. Setup non-data refs.
            btnClose.onClick.AddListener(OnCloseButtonClicked);
            btnShop.onClick.AddListener(OnShopClicked);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnBeforeShowAsync(InventoryData data, CancellationToken ct)
        {
            // Chạy mỗi lần mở. Bind data.
            if (data == null) return UniTask.CompletedTask;

            txtGold.text = data.Gold.ToString("N0");
            txtDiamond.text = data.Diamond.ToString("N0");
            // BindItems(data.Items);
            return UniTask.CompletedTask;
        }

        protected override UniTask OnShownAsync(InventoryData data, CancellationToken ct)
        {
            // Animation đã xong, user có thể click.
            return UniTask.CompletedTask;
        }

        private void OnShopClicked()
        {
            // Mở popup khác. Inventory sẽ tự pause nhờ Lane=Popup + PausableWhenOverlaid=true.
            UIManager.Instance.ShowAsync<ShopUI>(UIId.Shop).Forget();
        }

        // HandleEscape: mặc định đã close nếu Config.DismissByEscape=true.
        // Override nếu muốn hỏi confirm:
        // public override bool HandleEscape() {
        //     ShowConfirmExit();
        //     return true;
        // }
    }

    /// <summary>UI không cần data → kế thừa UIBase trực tiếp.</summary>
    public class ShopUI : UIBase
    {
        // Không override OnBeforeShowAsync(TData, ct).
        public override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }

    // =======================================================================
    // Cách dùng từ gameplay code
    // =======================================================================

    public class GameplayCallerExample : MonoBehaviour
    {
        public async void OnClickOpenInventory()
        {
            var data = new InventoryData
            {
                Gold = 12345,
                Diamond = 100,
                Items = new System.Collections.Generic.List<ItemInfo>()
            };

            // Cách 1: show + lấy typed view.
            var inv = await UIManager.Instance.ShowAsync<InventoryUI>(
                UIId.Inventory,
                new UIContext(data));
            Debug.Log($"Inventory opened, state={inv.State}");

            // Cách 2: fire-and-forget (không care kết quả).
            UIManager.Instance.ShowAsync(UIId.QuestBoard).Forget();

            // Cách 3: server-driven qua string ID.
            // UIManager.Instance.ShowByStringIdAsync("quest_reward", new UIContext(serverData)).Forget();
        }

        public async void OnClickCloseAll()
        {
            await UIManager.Instance.CloseAllPopupsAsync();
        }

        public async void OnPreloadCriticalAssets()
        {
            // Preload popup hay dùng lúc boot / lobby.
            await UIManager.Instance.PreloadAsync(UIId.Inventory);
            await UIManager.Instance.PreloadAsync(UIId.Shop);
        }
    }
}

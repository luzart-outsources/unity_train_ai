using System.Threading;
using Cysharp.Threading.Tasks;

namespace Luzart
{
    /// <summary>
    /// Generic base cho UI có data. Type-safe, học từ Lumber MVCViewPopup&lt;TViewData&gt;
    /// nhưng không cần serialize View component riêng.
    ///
    /// Usage:
    /// <code>
    /// public class InventoryUI : UIBase&lt;InventoryData&gt; {
    ///     protected override UniTask OnBeforeShowAsync(InventoryData data, CancellationToken ct) {
    ///         txtGold.text = data.Gold.ToString();
    ///         return BindItemsAsync(data.Items, ct);
    ///     }
    /// }
    /// </code>
    /// </summary>
    public abstract class UIBase<TData> : UIBase where TData : class
    {
        protected TData Data { get; private set; }

        public sealed override UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct)
        {
            Data = ctx.Data as TData;
            if (Data == null && ctx.Data != null)
            {
                UnityEngine.Debug.LogError(
                    $"[NinjaUI] {GetType().Name} expects data type {typeof(TData).Name} " +
                    $"but got {ctx.Data.GetType().Name}. UI will show with null data.");
            }
            return OnBeforeShowAsync(Data, ct);
        }

        public sealed override UniTask OnShownAsync(UIContext ctx, CancellationToken ct)
            => OnShownAsync(Data, ct);

        public sealed override UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct)
        {
            var result = OnHiddenAsync(Data, reason, ct);
            // Clear data để tránh leak giữa các lần show.
            Data = null;
            return result;
        }

        protected virtual UniTask OnBeforeShowAsync(TData data, CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnShownAsync(TData data, CancellationToken ct) => UniTask.CompletedTask;
        protected virtual UniTask OnHiddenAsync(TData data, UIHideReason reason, CancellationToken ct) => UniTask.CompletedTask;
    }
}

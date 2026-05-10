using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Base class cho mọi UI. Mỏng, async-first.
    /// Lifecycle:
    ///   OnCreateAsync (1 lần) → OnBeforeShowAsync → animation → OnShownAsync
    ///   → (OnPauseAsync → OnResumeAsync)*
    ///   → OnBeforeHideAsync → animation → OnHiddenAsync → (OnRelease)
    ///
    /// Dev bình thường nên kế thừa UIBase&lt;TData&gt; để type-safe. UIBase này cho UI
    /// không cần data (ví dụ HUD, Loading spinner).
    /// </summary>
    public abstract class UIBase : MonoBehaviour, IEscapeResponder
    {
        // Được UIManager set khi instantiate.
        public UIId Id { get; internal set; }
        public UILayer Lane { get; internal set; }
        public UIState State { get; internal set; } = UIState.None;
        public UIConfig Config { get; internal set; }

        public bool IsVisible => State == UIState.Visible || State == UIState.Paused;

        #region Lifecycle hooks

        /// <summary>Gọi 1 lần sau khi instantiate. Setup references, subscribe global events.</summary>
        public virtual UniTask OnCreateAsync(UIContext ctx, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Gọi trước mỗi lần show. Bind data, load sub-asset nếu cần.</summary>
        public virtual UniTask OnBeforeShowAsync(UIContext ctx, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Gọi sau khi animation show xong. Bắt đầu nhận input.</summary>
        public virtual UniTask OnShownAsync(UIContext ctx, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Bị popup khác đè lên. Unsubscribe timer, pause animation, KHÔNG destroy.</summary>
        public virtual UniTask OnPauseAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Popup đè lên đã đóng. Restore state.</summary>
        public virtual UniTask OnResumeAsync(CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Gọi trước khi animation hide chạy. Save state nếu cần.</summary>
        public virtual UniTask OnBeforeHideAsync(UIHideReason reason, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Gọi sau animation hide. Unsubscribe, cleanup.</summary>
        public virtual UniTask OnHiddenAsync(UIHideReason reason, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Instance sắp destroy. Release handle, dispose token.</summary>
        public virtual void OnRelease() { }

        #endregion

        #region Animation hooks (override nếu cần custom)

        /// <summary>Animation mở. Mặc định no-op — override để custom (DOTween scale, fade, slide...).</summary>
        public virtual UniTask AnimateShowAsync(bool instant, CancellationToken ct) => UniTask.CompletedTask;

        /// <summary>Animation đóng. Mặc định no-op — override để custom.</summary>
        public virtual UniTask AnimateHideAsync(bool instant, CancellationToken ct) => UniTask.CompletedTask;

        #endregion

        #region Escape / Back button

        /// <summary>
        /// Xử lý ESC/back. Return true = đã xử lý (stop propagation).
        /// Mặc định: nếu Config.DismissByEscape = true, request close.
        /// Override nếu cần hành vi đặc biệt (mở submenu, confirm...).
        /// </summary>
        public virtual bool HandleEscape()
        {
            if (Config == null || !Config.DismissByEscape) return false;
            if (State != UIState.Visible) return false;

            OnCloseButtonClicked();
            return true;
        }

        /// <summary>
        /// Hook cho close button. Override để hỏi confirm, save, ...
        /// Mặc định: request UIManager hide.
        /// </summary>
        public virtual void OnCloseButtonClicked()
        {
            // Sẽ được UIManager subscribe (tránh tight coupling với singleton).
            OnCloseRequested?.Invoke(this);
        }

        internal System.Action<UIBase> OnCloseRequested;

        /// <summary>Callback fire khi popup đã hide xong. Set bởi UIManager từ UIContext.OnClosedCallback.</summary>
        internal System.Action OnClosedCallback;

        #endregion
    }
}

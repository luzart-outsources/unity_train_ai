using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Luzart
{
    /// <summary>
    /// Facade cho gameplay code. Inject interface thay vì gọi UIManager.Instance trực tiếp
    /// để code testable (mock được) và tránh tight coupling.
    ///
    /// Nếu không dùng DI, có thể dùng UIManager.Instance trực tiếp (cũng implement interface này).
    /// </summary>
    public interface IUIService
    {
        // ---- Show ----
        UniTask<UIHandle> ShowAsync(UIId id, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default);
        UniTask<T> ShowAsync<T>(UIId id, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default) where T : UIBase;
        UniTask<UIHandle> ShowByStringIdAsync(string stringId, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default);

        /// <summary>Enqueue popup (đi qua UIPopupQueue). Show tuần tự, không chồng popup khác trong queue.</summary>
        UniTask<UIHandle> EnqueuePopupAsync(UIId id, UIContext ctx = default, UIShowOptions opts = default, int priority = 0, CancellationToken ct = default);

        // ---- Hide ----
        UniTask HideAsync(UIHandle handle, UIHideOptions opts = default, CancellationToken ct = default);
        UniTask HideAsync(UIId id, UIHideOptions opts = default, CancellationToken ct = default);
        UniTask CloseTopPopupAsync(UIHideOptions opts = default, CancellationToken ct = default);
        UniTask CloseAllPopupsAsync(UIHideOptions opts = default, CancellationToken ct = default);

        // ---- Preload / Release ----
        UniTask PreloadAsync(UIId id, IProgress<float> progress = null, CancellationToken ct = default);
        UniTask PreloadByLabelAsync(string label, IProgress<float> progress = null, CancellationToken ct = default);
        void Release(UIId id);

        // ---- Query ----
        bool IsVisible(UIId id);
        bool TryGetVisible(UIId id, out UIBase view);

        // ---- Convenience ----
        UniTask<UIHandle> ShowToastAsync(string message, ToastStyle style = ToastStyle.Info, float duration = 2f);
        IDisposable PushBlock(string reason);

        // ---- Events (diagnostic) ----
        event Action<UIId> OnLoadStarted;
        event Action<UIId> OnLoadCompleted;
        event Action<UIId, Exception> OnLoadFailed;
        event Action<UIBase> OnShown;
        event Action<UIBase, UIHideReason> OnHidden;
        event Action<UIId> OnReleased;
    }
}

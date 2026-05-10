using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Trung tâm điều phối UI. Async-first, lane-based, reference-counted.
    ///
    /// Setup:
    ///   1. Tạo Canvas trong scene, gắn UIManager.
    ///   2. Tạo 6 empty child cho 6 lane (WorldOverlay, Screen, Hud, Popup, System, Toast).
    ///   3. Tạo UIRegistrySO asset, fill entries, gán vào Registry field.
    ///   4. Optional: gán custom IUIAssetProvider (default = DirectPrefabUIAssetProvider).
    ///
    /// Gameplay code gọi qua:
    ///   - UIManager.Instance.ShowAsync&lt;InventoryUI&gt;(UIId.Inventory, ctx)
    ///   - Hoặc inject IUIService để testable.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIService
    {
        #region Singleton

        private static UIManager instance;
        public static UIManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindObjectOfType<UIManager>();
                return instance;
            }
        }

        #endregion

        #region Inspector

        [Header("Config")]
        [SerializeField] private UIRegistrySO registry;

        [Header("Layer roots (gán 6 RectTransform tương ứng UILayer enum)")]
        [SerializeField] private RectTransform worldOverlayRoot;
        [SerializeField] private RectTransform screenRoot;
        [SerializeField] private RectTransform hudRoot;
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private RectTransform systemRoot;
        [SerializeField] private RectTransform toastRoot;

        [Header("Optional")]
        [SerializeField] private UIInputRouter inputRouter;
        [SerializeField] private UIBlockService blockService;
        [SerializeField] private bool preloadOnStart = true;
        [SerializeField] private bool logVerbose = false;

        [Header("Boot preload labels (legacy)")]
        [Tooltip("Khong dung khi provider la DirectPrefabUIAssetProvider (no-op). " +
                 "Giu lai field de tuong thich neu sau nay swap sang Addressable provider.")]
        [SerializeField] private string[] bootPreloadLabels;

        #endregion

        #region Internal state

        private IUIAssetProvider assetProvider;
        private readonly Dictionary<UILayer, UILayerStack> stacks = new Dictionary<UILayer, UILayerStack>();

        /// <summary>Instance đã load nhưng đang ở Hidden state (pool theo policy).</summary>
        private readonly Dictionary<UIId, UIBase> pooledInstances = new Dictionary<UIId, UIBase>();

        /// <summary>ShowAsync đang chạy, để coalesce nếu gọi lại cùng UIId.</summary>
        private readonly Dictionary<UIId, UniTask<UIHandle>> pendingShows = new Dictionary<UIId, UniTask<UIHandle>>();

        /// <summary>Cancellation token per active UI, để hide async giữa chừng không leak.</summary>
        private readonly Dictionary<UIBase, CancellationTokenSource> activeCts = new Dictionary<UIBase, CancellationTokenSource>();

        private UIPopupQueue popupQueue;
        public UIPopupQueue PopupQueue => popupQueue;

        public event Action<UIId> OnLoadStarted;
        public event Action<UIId> OnLoadCompleted;
        public event Action<UIId, Exception> OnLoadFailed;
        public event Action<UIBase> OnShown;
        public event Action<UIBase, UIHideReason> OnHidden;
        public event Action<UIId> OnReleased;

        #endregion

        #region Unity lifecycle

        protected virtual void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            // Persist qua scene loads (FirstSceneNew → Gameplay → ...). Tránh UIManager bị destroy
            // khi switch scene → Instance trả null cho NoticeService.ShowLoading + các caller khác.
            // Phải call sau `instance = this` (set singleton trước) và TRƯỚC bất kỳ Destroy duplicate.
            // Yêu cầu: UIManager phải nằm trên ROOT GameObject (scene parent là null) để DDOL hợp lệ.
            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);
            else
                Debug.LogWarning("[NinjaUI] UIManager không phải root GameObject → DontDestroyOnLoad bị skip. " +
                                 "Move UIManager ra root scene để persist qua scene loads.");

            if (registry == null)
            {
                Debug.LogError("[NinjaUI] UIRegistrySO chưa được gán vào UIManager.");
                enabled = false;
                return;
            }

            registry.BuildLookup();
            BuildStacks();

            assetProvider ??= new DirectPrefabUIAssetProvider();
            popupQueue = new UIPopupQueue(this);
        }

        protected virtual async void Start()
        {
            var ct = this.GetCancellationTokenOnDestroy();

            if (preloadOnStart)
                await PreloadAllMarkedAsync(ct);

            if (bootPreloadLabels != null)
            {
                foreach (var label in bootPreloadLabels)
                {
                    if (string.IsNullOrEmpty(label)) continue;
                    try { await assetProvider.PreloadByLabelAsync(label, null, ct); }
                    catch (Exception e) { Debug.LogWarning($"[NinjaUI] Preload label '{label}' failed: {e.Message}"); }
                }
            }
        }

        protected virtual void OnDestroy()
        {
            foreach (var cts in activeCts.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            activeCts.Clear();

            if (assetProvider is DirectPrefabUIAssetProvider direct)
                direct.ReleaseAll();

            if (instance == this) instance = null;
        }

        private void BuildStacks()
        {
            stacks[UILayer.WorldOverlay] = new UILayerStack(UILayer.WorldOverlay, worldOverlayRoot);
            stacks[UILayer.Screen]       = new UILayerStack(UILayer.Screen, screenRoot);
            stacks[UILayer.Hud]          = new UILayerStack(UILayer.Hud, hudRoot);
            stacks[UILayer.Popup]        = new UILayerStack(UILayer.Popup, popupRoot);
            stacks[UILayer.System]       = new UILayerStack(UILayer.System, systemRoot);
            stacks[UILayer.Toast]        = new UILayerStack(UILayer.Toast, toastRoot);
        }

        #endregion

        #region Public API: Show

        public UniTask<UIHandle> ShowAsync(UIId id, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default)
        {
            // Coalesce: nếu đang show cùng UIId → reuse task.
            if (pendingShows.TryGetValue(id, out var pending))
                return pending;

            if (!registry.TryGet(id, out var config))
                throw new KeyNotFoundException($"[NinjaUI] No config for UIId={id}. Check UIRegistrySO.");

            // Dùng UniTaskCompletionSource để register pendingShows TRƯỚC khi work chạy.
            // Tránh race khi ShowInternalAsync hoàn tất đồng bộ (cache hit + instant animation):
            // không có Source → finally pendingShows.Remove chạy trước pendingShows[id] = task,
            // dẫn đến pendingShows giữ task stale → Show lần sau hit cache trả về UIHandle cũ
            // mà popup không thực sự hiện ra.
            var tcs = new UniTaskCompletionSource<UIHandle>();
            var task = tcs.Task.Preserve(); // Preserve để multi-await an toàn (2+ caller đồng thời).
            pendingShows[id] = task;

            ShowAndCompleteAsync(id, config, ctx, opts, ct, tcs).Forget();

            return task;
        }

        private async UniTaskVoid ShowAndCompleteAsync(UIId id, UIConfig config, UIContext ctx,
            UIShowOptions opts, CancellationToken ct, UniTaskCompletionSource<UIHandle> tcs)
        {
            try
            {
                var handle = await ShowInternalAsync(config, ctx, opts, ct);
                tcs.TrySetResult(handle);
            }
            catch (OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                pendingShows.Remove(id);
            }
        }

        public async UniTask<T> ShowAsync<T>(UIId id, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default)
            where T : UIBase
        {
            var handle = await ShowAsync(id, ctx, opts, ct);
            return handle.View as T;
        }

        public UniTask<UIHandle> ShowByStringIdAsync(string stringId, UIContext ctx = default, UIShowOptions opts = default, CancellationToken ct = default)
        {
            if (!registry.TryGet(stringId, out var config))
                throw new KeyNotFoundException($"[NinjaUI] No config for stringId='{stringId}'.");
            return ShowAsync(config.Id, ctx, opts, ct);
        }

        private async UniTask<UIHandle> ShowInternalAsync(UIConfig config, UIContext ctx, UIShowOptions opts, CancellationToken externalCt)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, this.GetCancellationTokenOnDestroy());
            var ct = linkedCts.Token;

            UIBase view = null;
            bool forceHid = false; // true sau khi SafeForceHideAsync chạy → skip lưu vào activeCts.
            try
            {
                // Nếu đã visible + không cho multi-instance → bring to top + return.
                if (!config.AllowMultiInstance)
                {
                    var existing = FindVisible(config.Id);
                    if (existing != null && !opts.ForceReload)
                    {
                        stacks[config.Lane].BringToTop(existing);
                        // += để không clobber callback của lần show trước (multicast cả hai).
                        if (ctx.OnClosedCallback != null)
                            existing.OnClosedCallback += ctx.OnClosedCallback;
                        return new UIHandle(config.Id, existing);
                    }
                }

                // Lấy instance từ pool hoặc instantiate mới.
                view = await AcquireInstanceAsync(config, ct);
                view.Id = config.Id;
                view.Lane = config.Lane;
                view.Config = config;
                if (ctx.OnClosedCallback != null)
                    view.OnClosedCallback += ctx.OnClosedCallback;

                // Đặt vào đúng lane root.
                var laneRoot = stacks[config.Lane].Root;
                if (view.transform.parent != laneRoot)
                    view.transform.SetParent(laneRoot, false);

                view.gameObject.SetActive(true);
                view.OnCloseRequested = HandleCloseRequest;

                // Lane push + pause others if needed.
                var toPause = stacks[config.Lane].Push(view);
                foreach (var p in toPause)
                    _ = PauseViewAsync(p, ct); // fire-and-forget, song song với show.

                // Lifecycle
                if (view.State == UIState.None || view.State == UIState.Loaded || view.State == UIState.Hidden)
                {
                    if (view.State == UIState.None)
                    {
                        view.State = UIState.Loaded;
                        await view.OnCreateAsync(ctx, ct);
                    }

                    view.State = UIState.Showing;
                    await view.OnBeforeShowAsync(ctx, ct);
                    await view.AnimateShowAsync(opts.Instant, ct);
                    view.State = UIState.Visible;
                    await view.OnShownAsync(ctx, ct);

                    if (inputRouter != null) inputRouter.Register(view);
                    OnShown?.Invoke(view);
                    LogVerbose($"Shown {view.Id} in lane {view.Lane}");
                }

                return new UIHandle(config.Id, view);
            }
            catch (OperationCanceledException)
            {
                if (view != null)
                {
                    await SafeForceHideAsync(view, UIHideReason.Cancelled);
                    forceHid = true;
                }
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NinjaUI] ShowAsync failed for {config.Id}: {e}");
                OnLoadFailed?.Invoke(config.Id, e);
                if (view != null)
                {
                    await SafeForceHideAsync(view, UIHideReason.Error);
                    forceHid = true;
                }
                throw;
            }
            finally
            {
                // pendingShows.Remove được xử lý ở ShowAndCompleteAsync.finally để tránh
                // race với caller chưa kịp gán pendingShows[id] (xem ShowAsync).
                // Khi force-hide đã chạy, view đã ở state Hidden/Released — HideInternalAsync
                // sẽ early-return không dispose CTS → tự dispose tại đây để khỏi leak.
                if (view != null && !forceHid)
                    activeCts[view] = linkedCts;
                else
                    linkedCts.Dispose();
            }
        }

        private async UniTask<UIBase> AcquireInstanceAsync(UIConfig config, CancellationToken ct)
        {
            // 1. Check pool.
            if (pooledInstances.TryGetValue(config.Id, out var pooled) && pooled != null)
            {
                pooledInstances.Remove(config.Id);
                return pooled;
            }

            // 2. Load asset.
            OnLoadStarted?.Invoke(config.Id);
            try
            {
                var prefab = await assetProvider.LoadAsync(config, ct);
                var go = Instantiate(prefab, stacks[config.Lane].Root);
                var view = go.GetComponent<UIBase>();
                if (view == null)
                {
                    Destroy(go);
                    assetProvider.Release(config);
                    throw new InvalidOperationException(
                        $"[NinjaUI] Prefab for {config.Id} has no UIBase component.");
                }
                OnLoadCompleted?.Invoke(config.Id);
                return view;
            }
            catch
            {
                // OnLoadFailed được raise ở tầng ShowInternalAsync để có context exception đầy đủ.
                throw;
            }
        }

        #endregion

        #region Public API: Hide

        public async UniTask HideAsync(UIHandle handle, UIHideOptions opts = default, CancellationToken ct = default)
        {
            if (handle == null || !handle.IsValid) return;
            await HideInternalAsync(handle.View, opts, ct);
        }

        public async UniTask HideAsync(UIId id, UIHideOptions opts = default, CancellationToken ct = default)
        {
            var view = FindVisible(id);
            if (view == null) return;
            await HideInternalAsync(view, opts, ct);
        }

        public async UniTask CloseTopPopupAsync(UIHideOptions opts = default, CancellationToken ct = default)
        {
            var top = stacks[UILayer.Popup].Top;
            if (top == null) return;
            await HideInternalAsync(top, opts, ct);
        }

        public async UniTask CloseAllPopupsAsync(UIHideOptions opts = default, CancellationToken ct = default)
        {
            var all = new List<UIBase>(stacks[UILayer.Popup].All);
            // Đóng từ top xuống.
            for (int i = all.Count - 1; i >= 0; i--)
            {
                if (ct.IsCancellationRequested) break;
                await HideInternalAsync(all[i], opts, ct);
            }
        }

        /// <summary>
        /// Hide all visible UIs trừ System lane. Dùng TRƯỚC khi <c>SceneManager.LoadScene</c>
        /// để UI cũ (Login, MainMenu, popup, ...) không persist qua scene transition vì
        /// UIManager là DontDestroyOnLoad. System lane (Notice loading/alert) giữ lại vì
        /// thường cần xuyên transition (vd "Đang vào game..." loading).
        /// </summary>
        public async UniTask HideAllExceptSystemAsync(UIHideOptions opts = default, CancellationToken ct = default)
        {
            foreach (var kv in stacks)
            {
                if (kv.Key == UILayer.System) continue;
                var allInLane = new List<UIBase>(kv.Value.All);
                for (int i = allInLane.Count - 1; i >= 0; i--)
                {
                    if (ct.IsCancellationRequested) return;
                    try
                    {
                        await HideInternalAsync(allInLane[i], opts, ct);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[NinjaUI] HideAllExceptSystemAsync: {kv.Key}/{allInLane[i]?.Id} failed: {e.Message}");
                    }
                }
            }
        }

        private async UniTask HideInternalAsync(UIBase view, UIHideOptions opts, CancellationToken externalCt)
        {
            if (view == null) return;
            if (view.State == UIState.Hiding || view.State == UIState.Hidden || view.State == UIState.Released)
                return;

            var reason = opts.Reason == default ? UIHideReason.Programmatic : opts.Reason;
            // Giữ reference tới CTS để dispose ở finally — tránh leak registration trên
            // OnDestroy token (sống cả đời MonoBehaviour, tích lũy theo số lần hide).
            using var hideLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                externalCt, this.GetCancellationTokenOnDestroy());
            var linkedCt = hideLinkedCts.Token;

            try
            {
                view.State = UIState.Hiding;
                await view.OnBeforeHideAsync(reason, linkedCt);
                await view.AnimateHideAsync(opts.Instant, linkedCt);
                view.State = UIState.Hidden;
                await view.OnHiddenAsync(reason, linkedCt);

                if (inputRouter != null) inputRouter.Unregister(view);

                // Lane pop + resume UI dưới.
                var toResume = stacks[view.Lane].Pop(view);
                foreach (var r in toResume)
                    _ = ResumeViewAsync(r, linkedCt);

                OnHidden?.Invoke(view, reason);
                LogVerbose($"Hidden {view.Id} (reason={reason})");
                InvokeClosedCallback(view);

                await ApplyCachePolicyAsync(view);
            }
            catch (OperationCanceledException)
            {
                await SafeForceHideAsync(view, UIHideReason.Cancelled);
            }
            finally
            {
                if (activeCts.TryGetValue(view, out var cts))
                {
                    cts.Dispose();
                    activeCts.Remove(view);
                }
            }
        }

        private async UniTask ApplyCachePolicyAsync(UIBase view)
        {
            var config = view.Config;
            switch (config.CachePolicy)
            {
                case UICachePolicy.ReleaseOnClose:
                    view.OnRelease();
                    view.State = UIState.Releasing;
                    Destroy(view.gameObject);
                    assetProvider.Release(config);
                    view.State = UIState.Released;
                    break;

                case UICachePolicy.PoolOnClose:
                    view.OnRelease();
                    view.State = UIState.Releasing;
                    Destroy(view.gameObject);
                    // Giữ asset, lần sau instantiate lại nhanh.
                    // (không add vào pool vì instance đã destroy)
                    view.State = UIState.Released;
                    break;

                case UICachePolicy.KeepLoaded:
                    view.gameObject.SetActive(false);
                    pooledInstances[config.Id] = view;
                    break;
            }
            await UniTask.CompletedTask;
        }

        private async UniTask SafeForceHideAsync(UIBase view, UIHideReason reason)
        {
            try
            {
                view.State = UIState.Hidden;
                if (view.gameObject != null)
                {
                    if (inputRouter != null) inputRouter.Unregister(view);
                    stacks[view.Lane].Pop(view);
                    OnHidden?.Invoke(view, reason);
                    await ApplyCachePolicyAsync(view);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NinjaUI] SafeForceHide failed: {e}");
            }
            finally
            {
                // Luôn fire callback dù cleanup partial fail — caller cần biết "popup gone".
                InvokeClosedCallback(view);
            }
        }

        private void InvokeClosedCallback(UIBase view)
        {
            if (view == null) return;
            var cb = view.OnClosedCallback;
            view.OnClosedCallback = null;
            if (cb == null) return;
            try { cb.Invoke(); }
            catch (Exception e) { Debug.LogError($"[NinjaUI] OnClosedCallback failed: {e}"); }
        }

        #endregion

        #region Pause / Resume

        private async UniTask PauseViewAsync(UIBase view, CancellationToken ct)
        {
            if (view == null || view.State != UIState.Visible) return;
            try
            {
                view.State = UIState.Paused;
                // Không SetActive(false) để giữ animation/Canvas state.
                // UI khác sẽ render đè lên nhờ SetAsLastSibling.
                // Subclass tự xử lý disable input trong OnPauseAsync (CanvasGroup, EventSystem, ...).
                await view.OnPauseAsync(ct);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NinjaUI] PauseView failed for {view.Id}: {e}");
            }
        }

        private async UniTask ResumeViewAsync(UIBase view, CancellationToken ct)
        {
            if (view == null || view.State != UIState.Paused) return;
            try
            {
                view.State = UIState.Visible;
                await view.OnResumeAsync(ct);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NinjaUI] ResumeView failed for {view.Id}: {e}");
            }
        }

        #endregion

        #region Preload / Release

        public async UniTask PreloadAsync(UIId id, IProgress<float> progress = null, CancellationToken ct = default)
        {
            if (!registry.TryGet(id, out var config)) return;
            await assetProvider.PreloadAsync(config, progress, ct);
        }

        public async UniTask PreloadByLabelAsync(string label, IProgress<float> progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(label)) return;
            await assetProvider.PreloadByLabelAsync(label, progress, ct);
        }

        public void Release(UIId id)
        {
            if (!registry.TryGet(id, out var config)) return;

            // Nếu có instance trong pool → destroy.
            if (pooledInstances.TryGetValue(id, out var pooled) && pooled != null)
            {
                pooled.OnRelease();
                Destroy(pooled.gameObject);
                pooledInstances.Remove(id);
            }
            assetProvider.Release(config);
            OnReleased?.Invoke(id);
        }

        private async UniTask PreloadAllMarkedAsync(CancellationToken ct)
        {
            foreach (var cfg in registry.GetPreloadEntries())
            {
                if (ct.IsCancellationRequested) return;
                try
                {
                    await assetProvider.PreloadAsync(cfg, null, ct);
                    LogVerbose($"Preloaded {cfg.Id}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[NinjaUI] Preload failed for {cfg.Id}: {e.Message}");
                }
            }
        }

        #endregion

        #region Query helpers

        public bool IsVisible(UIId id) => FindVisible(id) != null;

        public bool TryGetVisible(UIId id, out UIBase view)
        {
            view = FindVisible(id);
            return view != null;
        }

        private UIBase FindVisible(UIId id)
        {
            foreach (var kv in stacks)
            {
                var found = kv.Value.FindById(id);
                if (found != null && found.IsVisible)
                    return found;
            }
            return null;
        }

        #endregion

        #region Convenience API

        /// <summary>Enqueue popup vào UIPopupQueue. Show tuần tự, không chồng popup khác trong queue.</summary>
        public UniTask<UIHandle> EnqueuePopupAsync(UIId id, UIContext ctx = default, UIShowOptions opts = default,
            int priority = 0, CancellationToken ct = default)
        {
            return popupQueue.EnqueueAsync(id, ctx, opts, priority, ct);
        }

        /// <summary>Shortcut show toast. Tự tạo ToastData + ShowAsync.</summary>
        public UniTask<UIHandle> ShowToastAsync(string message, ToastStyle style = ToastStyle.Info, float duration = 2f)
        {
            var data = new ToastData(message, style, duration);
            return ShowAsync(UIId.Toast, new UIContext(data));
        }

        /// <summary>Push input blocker. Return IDisposable — dispose để release.</summary>
        public IDisposable PushBlock(string reason)
        {
            if (blockService == null)
            {
                Debug.LogWarning("[NinjaUI] UIBlockService chưa được gán. PushBlock trả về no-op.");
                return new NoOpDisposable();
            }
            return blockService.PushBlock(reason);
        }

        private class NoOpDisposable : IDisposable { public void Dispose() { } }

        #endregion

        #region Event handlers

        private void HandleCloseRequest(UIBase view)
        {
            _ = HideInternalAsync(view, new UIHideOptions { Reason = UIHideReason.UserRequested }, default);
        }

        #endregion

        #region Logging

        private void LogVerbose(string msg)
        {
            if (logVerbose) Debug.Log($"[NinjaUI] {msg}");
        }

        #endregion
    }
}

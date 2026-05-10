using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Queue show popup tuần tự. Học từ Lumber PopupQueuer.
    ///
    /// Dùng khi:
    ///   - Tutorial chain: show popup bước 1 → đóng → show bước 2.
    ///   - Daily reward combo: login bonus → quest reward → event offer (không chồng nhau).
    ///   - Server push: reconnect → maintenance → banner (tuần tự).
    ///
    /// Quy tắc:
    ///   - Chỉ 1 popup trong queue đang visible tại 1 thời điểm.
    ///   - Popup tiếp theo chỉ show khi popup trước đã close hoàn toàn.
    ///   - Priority cao hơn = chèn lên đầu queue.
    ///   - Không ảnh hưởng popup show trực tiếp qua UIManager.ShowAsync — queue chỉ áp dụng cho popup gọi qua EnqueuePopupAsync.
    /// </summary>
    public class UIPopupQueue
    {
        private class QueueItem
        {
            public UIId Id;
            public UIContext Context;
            public UIShowOptions Options;
            public int Priority;
            public UniTaskCompletionSource<UIHandle> Tcs;
            public CancellationToken Ct;
        }

        private readonly IUIService uiService;
        private readonly List<QueueItem> queue = new List<QueueItem>(8);
        private bool isProcessing;

        public int PendingCount => queue.Count;
        public bool IsProcessing => isProcessing;

        /// <summary>Event fired khi queue trống (xử lý xong item cuối).</summary>
        public event Action OnQueueEmpty;

        public UIPopupQueue(IUIService uiService)
        {
            this.uiService = uiService ?? throw new ArgumentNullException(nameof(uiService));
        }

        /// <summary>
        /// Enqueue popup để show tuần tự. Trả về task hoàn thành khi popup này được close.
        /// </summary>
        public UniTask<UIHandle> EnqueueAsync(UIId id, UIContext ctx = default, UIShowOptions opts = default,
            int priority = 0, CancellationToken ct = default)
        {
            var item = new QueueItem
            {
                Id = id,
                Context = ctx,
                Options = opts,
                Priority = priority,
                Tcs = new UniTaskCompletionSource<UIHandle>(),
                Ct = ct,
            };

            // Chèn theo priority giảm dần (priority cao lên trước).
            int insertIndex = queue.Count;
            for (int i = 0; i < queue.Count; i++)
            {
                if (queue[i].Priority < priority)
                {
                    insertIndex = i;
                    break;
                }
            }
            queue.Insert(insertIndex, item);

            if (!isProcessing)
                ProcessLoopAsync().Forget();

            return item.Tcs.Task;
        }

        /// <summary>Xóa queue, cancel tất cả pending (không đóng popup đang visible).</summary>
        public void Clear()
        {
            foreach (var item in queue)
                item.Tcs.TrySetCanceled();
            queue.Clear();
        }

        /// <summary>Xóa item có UIId cụ thể khỏi queue (popup đang visible không bị ảnh hưởng).</summary>
        public void Remove(UIId id)
        {
            for (int i = queue.Count - 1; i >= 0; i--)
            {
                if (queue[i].Id == id)
                {
                    queue[i].Tcs.TrySetCanceled();
                    queue.RemoveAt(i);
                }
            }
        }

        private async UniTaskVoid ProcessLoopAsync()
        {
            isProcessing = true;
            try
            {
                while (queue.Count > 0)
                {
                    var item = queue[0];
                    queue.RemoveAt(0);

                    if (item.Ct.IsCancellationRequested)
                    {
                        item.Tcs.TrySetCanceled();
                        continue;
                    }

                    try
                    {
                        var handle = await uiService.ShowAsync(item.Id, item.Context, item.Options, item.Ct);
                        item.Tcs.TrySetResult(handle);

                        // KHÔNG truyền item.Ct vào WaitUntilHiddenAsync: caller cancel sau khi popup
                        // hiện sẽ làm wait throw OCE → loop tiến sang item kế tiếp dù popup hiện tại
                        // còn visible, phá contract "chỉ 1 popup queue visible". Wait sẽ thoát tự
                        // nhiên khi popup hide thật sự (handle.View == null hoặc state Hidden).
                        await WaitUntilHiddenAsync(handle, CancellationToken.None);
                    }
                    catch (OperationCanceledException)
                    {
                        item.Tcs.TrySetCanceled();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[NinjaUI][Queue] Failed to show {item.Id}: {e}");
                        item.Tcs.TrySetException(e);
                    }
                }

                OnQueueEmpty?.Invoke();
            }
            finally
            {
                isProcessing = false;
            }
        }

        private async UniTask WaitUntilHiddenAsync(UIHandle handle, CancellationToken ct)
        {
            if (handle == null || !handle.IsValid) return;

            // Poll state. Đơn giản, dùng UniTask.WaitWhile.
            await UniTask.WaitWhile(
                () => handle.IsValid && handle.View != null &&
                      (handle.View.State == UIState.Showing ||
                       handle.View.State == UIState.Visible ||
                       handle.View.State == UIState.Paused),
                PlayerLoopTiming.Update,
                ct);
        }
    }
}

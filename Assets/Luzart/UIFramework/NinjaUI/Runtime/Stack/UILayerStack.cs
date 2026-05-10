using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Stack UI trong 1 lane. Mỗi lane có 1 instance.
    ///
    /// Luật:
    ///   - Popup lane: LIFO, push pause top, pop resume.
    ///   - Screen lane: exclusive, push replace (pause previous, có back stack optional).
    ///   - HUD/Toast/WorldOverlay lane: flat list, không pause nhau.
    ///
    /// Học từ Lumber PopupContainer + thêm async pause/resume.
    /// </summary>
    public class UILayerStack
    {
        public UILayer Lane { get; }
        public RectTransform Root { get; }

        private readonly List<UIBase> stack = new List<UIBase>(8);

        public UILayerStack(UILayer lane, RectTransform root)
        {
            Lane = lane;
            Root = root;
        }

        public UIBase Top => stack.Count > 0 ? stack[stack.Count - 1] : null;
        public int Count => stack.Count;
        public IReadOnlyList<UIBase> All => stack;

        /// <summary>Push UI lên top. Trả về list UI cần pause (caller tự pause async).</summary>
        public List<UIBase> Push(UIBase ui)
        {
            var toPause = new List<UIBase>();

            // Lane Popup/Screen mới có khái niệm pause khi bị đè. Lane khác bỏ qua.
            // PausableWhenOverlaid là property của UI đang nằm DƯỚI (UI sắp bị đè),
            // KHÔNG phải của UI mới đè lên — đọc từ existing.Config, không phải ui.Config.
            if (Lane == UILayer.Popup || Lane == UILayer.Screen)
            {
                for (int i = 0; i < stack.Count; i++)
                {
                    var existing = stack[i];
                    if (existing == null || existing.State != UIState.Visible) continue;
                    if (existing.Config?.PausableWhenOverlaid ?? true)
                        toPause.Add(existing);
                }
            }

            stack.Add(ui);
            ui.transform.SetAsLastSibling();
            return toPause;
        }

        /// <summary>Pop UI. Trả về list UI cần resume (caller tự resume async).</summary>
        public List<UIBase> Pop(UIBase ui)
        {
            var toResume = new List<UIBase>();
            stack.Remove(ui);

            // Nếu popup/screen, resume top mới (nếu đang paused).
            if ((Lane == UILayer.Popup || Lane == UILayer.Screen) && stack.Count > 0)
            {
                var newTop = stack[stack.Count - 1];
                if (newTop != null && newTop.State == UIState.Paused)
                    toResume.Add(newTop);
            }

            return toResume;
        }

        public bool Contains(UIBase ui) => stack.Contains(ui);

        public UIBase FindById(UIId id)
        {
            for (int i = 0; i < stack.Count; i++)
                if (stack[i] != null && stack[i].Id == id)
                    return stack[i];
            return null;
        }

        public void BringToTop(UIBase ui)
        {
            if (!stack.Contains(ui)) return;
            stack.Remove(ui);
            stack.Add(ui);
            ui.transform.SetAsLastSibling();
        }

        /// <summary>Clear stack không chạy animation. Dùng cho scene change.</summary>
        public List<UIBase> Clear()
        {
            var all = new List<UIBase>(stack);
            stack.Clear();
            return all;
        }
    }
}

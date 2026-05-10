using System;

namespace Luzart
{
    /// <summary>
    /// Context truyền vào UI khi show. Gồm data payload + metadata.
    /// Học từ Lumber MVCViewPopup&lt;TData&gt; nhưng không constrain kiểu data cụ thể.
    /// </summary>
    public readonly struct UIContext
    {
        public readonly object Data;
        public readonly Action OnClosedCallback;

        public UIContext(object data = null, Action onClosed = null)
        {
            Data = data;
            OnClosedCallback = onClosed;
        }

        public T GetData<T>()
        {
            if (Data is T typed) return typed;
            return default;
        }

        public bool TryGetData<T>(out T result)
        {
            if (Data is T typed)
            {
                result = typed;
                return true;
            }
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Option khi show UI. Mọi field đều optional.
    /// </summary>
    public struct UIShowOptions
    {
        /// <summary>Bỏ qua animation, hiện tức thì.</summary>
        public bool Instant;

        /// <summary>Force reload asset dù đã có trong cache.</summary>
        public bool ForceReload;

        public static UIShowOptions Default => default;
    }

    /// <summary>
    /// Option khi hide UI.
    /// </summary>
    public struct UIHideOptions
    {
        public bool Instant;
        public UIHideReason Reason;

        public static UIHideOptions Default => default;
    }
}

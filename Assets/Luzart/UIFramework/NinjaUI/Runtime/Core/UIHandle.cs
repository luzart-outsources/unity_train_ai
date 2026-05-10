namespace Luzart
{
    /// <summary>
    /// Handle trả về từ ShowAsync. Dùng để hide/cancel chính xác instance cụ thể
    /// (quan trọng khi allowMultiInstance = true).
    /// </summary>
    public class UIHandle
    {
        public UIId Id { get; }
        public UIBase View { get; internal set; }
        public bool IsValid => View != null;

        internal UIHandle(UIId id, UIBase view)
        {
            Id = id;
            View = view;
        }

        internal void Invalidate() { View = null; }
    }
}

namespace Luzart
{
    /// <summary>
    /// Interface cho UI nhận ESC/back button. Chain of responsibility.
    /// Học từ Lumber EscapePropagator.
    /// </summary>
    public interface IEscapeResponder
    {
        /// <summary>
        /// Return true = đã xử lý, stop propagation.
        /// Return false = pass xuống responder dưới.
        /// </summary>
        bool HandleEscape();
    }
}

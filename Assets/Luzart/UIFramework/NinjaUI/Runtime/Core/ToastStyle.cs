namespace Luzart
{
    public enum ToastStyle
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,
    }

    /// <summary>
    /// Data truyền vào UIToast. UI Toast concrete kế thừa UIBase&lt;ToastData&gt;.
    /// </summary>
    public class ToastData
    {
        public string Message;
        public ToastStyle Style;
        public float DurationSeconds;

        public ToastData(string message, ToastStyle style = ToastStyle.Info, float duration = 2f)
        {
            Message = message;
            Style = style;
            DurationSeconds = duration;
        }
    }
}

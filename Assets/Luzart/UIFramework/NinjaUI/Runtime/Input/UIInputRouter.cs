using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Router cho ESC / Android back button. Chain of responsibility.
    /// Duyệt từ top (mới nhất) xuống, responder nào return true thì stop.
    ///
    /// Đăng ký: UIManager tự gọi Register/Unregister khi show/hide.
    /// </summary>
    public class UIInputRouter : MonoBehaviour
    {
        private readonly List<IEscapeResponder> responders = new List<IEscapeResponder>(16);

        [Tooltip("Bật xử lý phím ESC (PC).")]
        public bool HandleEscapeKey = true;

        [Tooltip("Bật xử lý nút back Android.")]
        public bool HandleAndroidBack = true;

        public void Register(IEscapeResponder responder)
        {
            if (responder == null) return;
            // Remove nếu đã có để đảm bảo vào top stack.
            responders.Remove(responder);
            responders.Add(responder);
        }

        public void Unregister(IEscapeResponder responder)
        {
            if (responder == null) return;
            responders.Remove(responder);
        }

        public bool DispatchEscape()
        {
            // Duyệt từ cuối (topmost) về đầu.
            for (int i = responders.Count - 1; i >= 0; i--)
            {
                var r = responders[i];
                if (r == null) continue;
                if (r.HandleEscape()) return true;
            }
            return false;
        }

        private void Update()
        {
            bool pressed = false;
#if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            if (HandleEscapeKey && Input.GetKeyDown(KeyCode.Escape))
                pressed = true;
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (HandleAndroidBack && Input.GetKeyDown(KeyCode.Escape))
                pressed = true;
#endif
            if (pressed)
            {
                DispatchEscape();
            }
        }
    }
}

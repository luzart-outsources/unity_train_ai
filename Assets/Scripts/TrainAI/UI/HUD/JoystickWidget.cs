using UnityEngine;
using UnityEngine.EventSystems;

namespace TrainAI.UI.HUD
{
    // Floating joystick simple - drag handle quanh background.
    public class JoystickWidget : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float radius = 80f;

        public Vector2 Direction { get; private set; }
        public bool IsActive { get; private set; }

        private Vector2 _startPos;
        private Camera _cam;

        private void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                _cam = canvas.worldCamera;
            else
                _cam = null;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (background == null) return;
            IsActive = true;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background.parent as RectTransform, e.position, _cam, out _startPos);
            background.anchoredPosition = _startPos;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
        }

        public void OnDrag(PointerEventData e)
        {
            if (background == null || handle == null) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background, e.position, _cam, out local);
            Vector2 clamped = Vector2.ClampMagnitude(local, radius);
            handle.anchoredPosition = clamped;
            Direction = clamped / radius;
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsActive = false;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIJoystickController : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField] float handleRange = 50f;

        Vector2 _value;
        public Vector2 Value => _value;

        public void OnPointerDown(PointerEventData e) => OnDrag(e);

        public void OnDrag(PointerEventData e)
        {
            if (background == null || handle == null) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(background, e.position, e.pressEventCamera, out var local);
            Vector2 size = background.rect.size;
            local.x = Mathf.Clamp(local.x, -size.x * 0.5f, size.x * 0.5f);
            local.y = Mathf.Clamp(local.y, -size.y * 0.5f, size.y * 0.5f);
            handle.anchoredPosition = Vector2.ClampMagnitude(local, handleRange);
            _value = handle.anchoredPosition / handleRange;
        }

        public void OnPointerUp(PointerEventData _)
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            _value = Vector2.zero;
        }
    }
}

using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI
{
    public class UIMiniMapController : MonoBehaviour
    {
        [SerializeField] RectTransform mapRect;
        [SerializeField] RectTransform playerDot;
        [SerializeField] PlayerStateRSO playerState;
        [SerializeField] Vector2 worldMin = new(-100, -70);
        [SerializeField] Vector2 worldMax = new(100, 70);

        // When set, replaces the RawImage's RenderTexture with a static HOLA
        // poster image. Lets us swap the live top-down RT for a designer-
        // authored map without ripping out the RawImage pipeline — keeps
        // the same SciFi frame + PlayerDot logic, only the underlying texture
        // changes.
        [SerializeField] RawImage mapImage;
        [SerializeField] Texture holaMapTexture;

        void OnEnable()
        {
            if (mapImage != null && holaMapTexture != null)
            {
                mapImage.texture = holaMapTexture;
                mapImage.uvRect = new Rect(0, 0, 1, 1);
            }
        }

        void Update()
        {
            if (mapRect == null || playerDot == null || playerState == null) return;
            Vector3 p = playerState.lastWorldPos;
            float u = Mathf.InverseLerp(worldMin.x, worldMax.x, p.x);
            float v = Mathf.InverseLerp(worldMin.y, worldMax.y, p.z);
            Vector2 size = mapRect.rect.size;
            playerDot.anchoredPosition = new Vector2(
                (u - 0.5f) * size.x,
                (v - 0.5f) * size.y);
        }
    }
}

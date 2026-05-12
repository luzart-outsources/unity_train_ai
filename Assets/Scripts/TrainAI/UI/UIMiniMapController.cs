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
        [SerializeField] Vector2 worldMin = new(-25, -25);
        [SerializeField] Vector2 worldMax = new(25, 25);

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

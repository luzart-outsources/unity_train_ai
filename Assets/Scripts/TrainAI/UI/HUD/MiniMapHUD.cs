using UnityEngine;
using UnityEngine.UI;

namespace TrainAI.UI.HUD
{
    // Simple top-down mini-map: 1 RawImage hien camera render.
    // Camera tu setup ngoai (1 Camera ortho top-down render texture).
    public class MiniMapHUD : MonoBehaviour
    {
        [SerializeField] private RawImage rawImage;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private Camera mapCamera;
        [SerializeField] private RectTransform playerDot;
        [SerializeField] private Transform playerWorld;
        [SerializeField] private float worldToMapScale = 5f;

        private float _playerFindCooldown;

        private void Awake()
        {
            if (rawImage != null && renderTexture != null) rawImage.texture = renderTexture;
            if (mapCamera != null && renderTexture != null) mapCamera.targetTexture = renderTexture;
        }

        private void LateUpdate()
        {
            if (playerWorld == null)
            {
                _playerFindCooldown -= UnityEngine.Time.deltaTime;
                if (_playerFindCooldown <= 0f)
                {
                    _playerFindCooldown = 0.5f;
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) playerWorld = p.transform;
                }
            }
            if (playerDot != null && playerWorld != null && mapCamera != null)
            {
                Vector3 wp = playerWorld.position;
                Vector3 cp = mapCamera.transform.position;
                Vector2 offset = new Vector2(wp.x - cp.x, wp.z - cp.z) * worldToMapScale;
                playerDot.anchoredPosition = offset;
            }
        }
    }
}

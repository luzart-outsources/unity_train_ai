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

        // Optional reference to the player's actual world Transform so the
        // arrow on the minimap rotates to match facing direction. Without
        // this, only player position updates and the arrow stays pointing
        // north — survival-game minimaps typically rotate the arrow with
        // the player so the player can read direction at a glance.
        [SerializeField] Transform playerWorldTransform;

        void OnEnable()
        {
            if (mapImage != null && holaMapTexture != null)
            {
                mapImage.texture = holaMapTexture;
                mapImage.uvRect = new Rect(0, 0, 1, 1);
            }
            // Resolve player transform lazily — Player is in 10_World scene,
            // not in Bootstrap, so it isn't available at component Awake.
            if (playerWorldTransform == null)
            {
                var p = GameObject.Find("Player");
                if (p != null) playerWorldTransform = p.transform;
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

            // Rotate the player arrow to match the player's heading. Unity
            // world Y rotation maps directly to a UI Z rotation (top-down
            // view) but inverted because UI rotates counter-clockwise
            // while compass/world rotates clockwise.
            if (playerWorldTransform == null)
            {
                var pgo = GameObject.Find("Player");
                if (pgo != null) playerWorldTransform = pgo.transform;
            }
            if (playerWorldTransform != null)
            {
                float yaw = playerWorldTransform.eulerAngles.y;
                playerDot.localEulerAngles = new Vector3(0, 0, -yaw);
            }
        }
    }
}

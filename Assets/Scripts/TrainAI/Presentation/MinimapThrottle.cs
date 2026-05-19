using TrainAI.Services;
using UnityEngine;

namespace TrainAI.Presentation
{
    // Disables Camera's auto-render in Update and manually triggers Render()
    // at a fixed low rate, so the minimap doesn't redraw 966+ renderers
    // every frame alongside the main camera. With both cameras rendering
    // every frame on D3D12, the editor was running out of command-list slots
    // after ~30 seconds of camera rotation and crashing the GPU.
    //
    // Attach to the MinimapCamera GameObject. The Camera component is left
    // assigned (RawImage in HUD still binds to its targetTexture); we just
    // toggle the auto-render path and pump it at our own rate.
    [RequireComponent(typeof(Camera))]
    public class MinimapThrottle : MonoBehaviour
    {
        [SerializeField] float intervalSec = 0.2f; // 5 Hz
        Camera _cam;
        float _accum;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            // Auto-render off; we'll drive Render() manually at intervalSec.
            _cam.enabled = false;
        }

        void LateUpdate()
        {
            if (_cam == null || _cam.targetTexture == null) return;
            // Skip rendering while a scene transition is in flight. The
            // MinimapCamera lives in 10_World — when LoadReplacing unloads
            // 10_World to enter a sub-scene, calling Render() right at the
            // unload boundary leaves command-buffer state dangling because
            // the renderers being drawn are about to be destroyed. The
            // 426× "m_CmdState == kEmpty" assertion spam observed in the
            // 2026-05-19 08:26 crash dump matches that signature exactly.
            // Same pattern as MovementService.Tick gating on IsTransitioning.
            if (SceneRouter.IsTransitioning) return;
            _accum += Time.unscaledDeltaTime;
            if (_accum < intervalSec) return;
            _accum = 0f;
            _cam.Render();
        }
    }
}

using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TrainAI.Presentation
{
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        // Singleton-by-survival: only one rig may exist at a time. The first
        // instance that wakes up survives (and DDOLs itself); any later copy
        // — typically from re-loading 10_World after a sub-scene visit —
        // self-destructs. Pattern identical to InteractionRouterBridge and
        // PlayerPersistence so the camera also survives every LoadReplacing.
        //
        // Why this matters: without DDOL the rig dies with 10_World during a
        // world→sub-scene swap, and the sub-scene's own static MainCamera
        // takes over (no follow logic). User reported "mất camera focus" on
        // every transition except the very first world load.
        static ThirdPersonCameraRig _instance;
        const float DEF_DIST = 6f;
        const float DEF_HEIGHT = 2.2f;
        const float DEF_YAW_SPEED = 140f;
        const float DEF_PITCH_MIN = -10f;
        const float DEF_PITCH_MAX = 60f;
        const float DEF_SMOOTH = 0.08f;

        [SerializeField] Transform target;
        [SerializeField] Camera cam;
        [SerializeField] ThirdPersonCameraConfigSO config;
        [SerializeField] float mouseSensitivity = 0.25f;
        [SerializeField] bool requireRightMouseToOrbit = true;
        [SerializeField] float initialYaw = 0f;
        [SerializeField] float initialPitch = 25f;

        float _yaw, _pitch;
        Vector3 _vel;
        bool _snapped;

        float D => config != null ? config.distance : DEF_DIST;
        float H => config != null ? config.height : DEF_HEIGHT;
        float YawSpeed => config != null ? config.yawSpeed : DEF_YAW_SPEED;
        float PitchMin => config != null ? config.pitchMin : DEF_PITCH_MIN;
        float PitchMax => config != null ? config.pitchMax : DEF_PITCH_MAX;
        float Smooth => config != null ? config.smoothTime : DEF_SMOOTH;
        bool InvertY => config != null && config.invertY;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            // Reparent to scene root so DontDestroyOnLoad accepts it.
            if (transform.parent != null) transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (cam == null) cam = GetComponentInChildren<Camera>();
            RefreshTarget();
            _yaw = initialYaw;
            _pitch = initialPitch;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Player is DDOL too, but its tag-by-find may not have been ready
            // when our Awake ran (different scene load order). Re-find on
            // every load to recover from any race. Also snap on next frame so
            // the camera doesn't smoothly lerp from the old world position to
            // the player's new sub-scene position (jarring).
            RefreshTarget();
            _snapped = false;
        }

        void RefreshTarget()
        {
            if (target != null && target.gameObject != null) return;
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        void LateUpdate()
        {
            if (target == null || cam == null) return;

            Vector2 look = ReadLook();
            float dx = look.x * YawSpeed * mouseSensitivity * Time.deltaTime;
            float dy = look.y * YawSpeed * mouseSensitivity * Time.deltaTime * (InvertY ? 1f : -1f);
            _yaw += dx;
            _pitch = Mathf.Clamp(_pitch + dy, PitchMin, PitchMax);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = target.position + Vector3.up * H
                              - transform.rotation * Vector3.forward * D;
            if (!_snapped)
            {
                transform.position = desired;
                _vel = Vector3.zero;
                _snapped = true;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, Smooth);
            }
            cam.transform.LookAt(target.position + Vector3.up * (H * 0.6f));
        }

        Vector2 ReadLook()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (requireRightMouseToOrbit && !mouse.rightButton.isPressed) return Vector2.zero;
                return mouse.delta.ReadValue();
            }
            var gp = Gamepad.current;
            if (gp != null) return gp.rightStick.ReadValue() * 10f;
            return Vector2.zero;
        }
    }
}

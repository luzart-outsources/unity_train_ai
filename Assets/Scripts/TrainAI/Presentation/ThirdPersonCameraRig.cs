using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Presentation
{
    public class ThirdPersonCameraRig : MonoBehaviour
    {
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
            if (cam == null) cam = GetComponentInChildren<Camera>();
            if (target == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) target = p.transform;
            }
            _yaw = initialYaw;
            _pitch = initialPitch;
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

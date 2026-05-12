using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Presentation
{
    public class ThirdPersonCameraRig : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Camera cam;
        [SerializeField] ThirdPersonCameraConfigSO config;
        [SerializeField] float mouseSensitivity = 0.2f;
        [SerializeField] bool requireRightMouseToOrbit = true;

        float _yaw, _pitch = 15f;
        Vector3 _vel;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
        }

        void LateUpdate()
        {
            if (target == null || config == null || cam == null) return;

            Vector2 look = ReadLook();
            float dx = look.x * config.yawSpeed * mouseSensitivity * Time.deltaTime;
            float dy = look.y * config.yawSpeed * mouseSensitivity * Time.deltaTime * (config.invertY ? 1f : -1f);
            _yaw += dx;
            _pitch = Mathf.Clamp(_pitch + dy, config.pitchMin, config.pitchMax);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = target.position + Vector3.up * config.height
                              - transform.rotation * Vector3.forward * config.distance;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, config.smoothTime);
            cam.transform.LookAt(target.position + Vector3.up * (config.height * 0.6f));
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

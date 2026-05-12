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
        [SerializeField] InputActionReference lookAction;

        float _yaw, _pitch = 15f;
        Vector3 _vel;

        void Awake()
        {
            if (cam == null) cam = GetComponentInChildren<Camera>();
        }

        void OnEnable() { lookAction?.action?.Enable(); }
        void OnDisable() { lookAction?.action?.Disable(); }

        void LateUpdate()
        {
            if (target == null || config == null || cam == null) return;

            Vector2 look = lookAction != null && lookAction.action != null
                ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
            float dx = look.x * config.yawSpeed * Time.deltaTime;
            float dy = look.y * config.yawSpeed * Time.deltaTime * (config.invertY ? 1f : -1f);
            _yaw += dx;
            _pitch = Mathf.Clamp(_pitch + dy, config.pitchMin, config.pitchMax);

            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = target.position + Vector3.up * config.height
                              - transform.rotation * Vector3.forward * config.distance;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, config.smoothTime);
            cam.transform.LookAt(target.position + Vector3.up * (config.height * 0.6f));
        }
    }
}

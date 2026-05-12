using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Presentation
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 4f;
        [SerializeField] float gravity = -12f;
        [SerializeField] PlayerStateRSO playerState;

        [Header("Input (Input System)")]
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference lookAction;

        [Header("Camera")]
        [SerializeField] Transform cameraRig;

        CharacterController _cc;
        Vector3 _velocity;

        void Awake() { _cc = GetComponent<CharacterController>(); }

        void OnEnable()
        {
            moveAction?.action?.Enable();
            lookAction?.action?.Enable();
        }

        void OnDisable()
        {
            moveAction?.action?.Disable();
            lookAction?.action?.Disable();
        }

        void Update()
        {
            Vector2 input = moveAction != null && moveAction.action != null
                ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

            Vector3 forward = cameraRig != null
                ? Vector3.ProjectOnPlane(cameraRig.forward, Vector3.up).normalized
                : transform.forward;
            Vector3 right = cameraRig != null
                ? Vector3.ProjectOnPlane(cameraRig.right, Vector3.up).normalized
                : transform.right;

            Vector3 wish = (forward * input.y + right * input.x) * moveSpeed;
            if (wish.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(wish), 0.2f);

            _velocity.x = wish.x;
            _velocity.z = wish.z;
            _velocity.y = _cc.isGrounded ? -1f : _velocity.y + gravity * Time.deltaTime;

            _cc.Move(_velocity * Time.deltaTime);

            if (playerState != null) playerState.lastWorldPos = transform.position;
        }
    }
}

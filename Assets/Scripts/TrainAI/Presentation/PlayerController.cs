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

        [Header("Camera")]
        [SerializeField] Transform cameraRig;

        [Header("Mobile")]
        [SerializeField] Joystick joystick;

        CharacterController _cc;
        Vector3 _velocity;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (cameraRig == null)
            {
                var rig = GameObject.Find("CameraRig");
                if (rig != null) cameraRig = rig.transform;
            }
        }

        void Start()
        {
            // Joystick lives on the persistent UICanvas (DontDestroyOnLoad scene),
            // so it isn't available at Awake before this scene's Start. Resolve here.
            if (joystick == null)
                joystick = Object.FindFirstObjectByType<Joystick>(FindObjectsInactive.Include);
        }

        void Update()
        {
            Vector2 input = ReadMove();

            Vector3 forward, right;
            if (cameraRig != null)
            {
                forward = Vector3.ProjectOnPlane(cameraRig.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(cameraRig.right, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right;
            }
            else
            {
                forward = Vector3.forward;
                right = Vector3.right;
            }

            Vector3 wish = (forward * input.y + right * input.x) * moveSpeed;
            if (wish.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(wish), 0.2f);

            _velocity.x = wish.x;
            _velocity.z = wish.z;
            _velocity.y = _cc.isGrounded ? -2f : _velocity.y + gravity * Time.deltaTime;
            _cc.Move(_velocity * Time.deltaTime);

            if (playerState != null) playerState.lastWorldPos = transform.position;
        }

        Vector2 ReadMove()
        {
            Vector2 v = Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  v.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            }
            var gp = Gamepad.current;
            if (gp != null && v == Vector2.zero) v = gp.leftStick.ReadValue();
            if (joystick != null && v == Vector2.zero) v = joystick.Direction;
            return Vector2.ClampMagnitude(v, 1f);
        }
    }
}

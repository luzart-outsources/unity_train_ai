using TrainAI.UI.HUD;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrainAI.Player
{
    // Top-down 3D move with joystick (drives via JoystickWidget) + camera follow.
    // Khong xai DATN PlayerController cu (Stardew template) - day la code moi don gian.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private JoystickWidget joystick;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private float gravity = -9.81f;

        private CharacterController _cc;
        private Vector3 _velocity;
        private Camera _cam;

        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _cam = Camera.main;
        }

        private void Update()
        {
            Vector2 input = joystick != null ? joystick.Direction : ReadKeyboardFallback();

            Vector3 camForward = _cam != null ? _cam.transform.forward : Vector3.forward;
            Vector3 camRight = _cam != null ? _cam.transform.right : Vector3.right;
            camForward.y = 0; camRight.y = 0;
            camForward.Normalize(); camRight.Normalize();

            Vector3 move = camForward * input.y + camRight * input.x;
            move *= moveSpeed;

            // Gravity simple.
            if (_cc.isGrounded && _velocity.y < 0) _velocity.y = -2f;
            _velocity.y += gravity * UnityEngine.Time.deltaTime;

            Vector3 step = move * UnityEngine.Time.deltaTime + new Vector3(0, _velocity.y, 0) * UnityEngine.Time.deltaTime;
            _cc.Move(step);

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion target = Quaternion.LookRotation(move);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * UnityEngine.Time.deltaTime);
            }
        }

        private static Vector2 ReadKeyboardFallback()
        {
            // Input System (new) - KHONG dung UnityEngine.Input legacy.
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float h = 0f, v = 0f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
            return new Vector2(h, v).normalized;
        }
    }
}

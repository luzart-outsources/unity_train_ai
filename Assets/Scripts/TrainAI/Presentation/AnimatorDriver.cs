using UnityEngine;

namespace TrainAI.Presentation
{
    // Drives a humanoid Animator's Speed/Grounded parameters from the host
    // CharacterController's actual movement, so the visible character matches
    // input. The Animator itself lives on the child visual (e.g. the
    // CustomizableCharacter prefab from AdvancedPeopleSystem2).
    //
    // Looks for an Animator in children if one isn't assigned. Parameter names
    // match common humanoid controllers (Speed, Grounded). If a parameter is
    // missing, that call is a silent no-op — works with any controller.
    [RequireComponent(typeof(CharacterController))]
    public class AnimatorDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Float param name (e.g. 'Speed') — for blend trees / locomotion controllers.")]
        [SerializeField] string speedParam = "Speed";
        [Tooltip("Bool param name (e.g. 'walk') — for simple two-state Idle↔Walk controllers like AdvancedPeopleSystem2's MaleAnimator.")]
        [SerializeField] string walkBoolParam = "walk";
        [Tooltip("Speed threshold (m/s) above which walkBool is set true. Ignored if controller has no walk-style bool.")]
        [SerializeField] float walkThreshold = 0.1f;
        [SerializeField] string groundedParam = "Grounded";
        [SerializeField] float speedSmoothing = 8f;

        CharacterController _cc;
        float _smoothedSpeed;
        int _speedHash, _walkHash, _groundedHash;
        bool _hasSpeed, _hasWalk, _hasGrounded;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            ResolveParams();
        }

        void ResolveParams()
        {
            _hasSpeed = _hasWalk = _hasGrounded = false;
            if (animator == null) return;
            _speedHash = Animator.StringToHash(speedParam);
            _walkHash = Animator.StringToHash(walkBoolParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            // Resolve whichever subset of params the controller exposes. The
            // AdvancedPeopleSystem2 MaleAnimator only has a `walk` bool, so
            // older code that only set Speed/Grounded left it idle forever.
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == _speedHash && p.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                else if (p.nameHash == _walkHash && p.type == AnimatorControllerParameterType.Bool) _hasWalk = true;
                else if (p.nameHash == _groundedHash && p.type == AnimatorControllerParameterType.Bool) _hasGrounded = true;
            }
        }

        void Update()
        {
            if (animator == null) return;
            // Use horizontal velocity only — vertical (gravity) shouldn't trigger walk anim.
            Vector3 v = _cc.velocity; v.y = 0f;
            float target = v.magnitude;
            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, target, Time.deltaTime * speedSmoothing);
            if (_hasSpeed) animator.SetFloat(_speedHash, _smoothedSpeed);
            if (_hasWalk) animator.SetBool(_walkHash, _smoothedSpeed > walkThreshold);
            if (_hasGrounded) animator.SetBool(_groundedHash, _cc.isGrounded);
        }
    }
}

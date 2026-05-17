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
        [SerializeField] string speedParam = "Speed";
        [SerializeField] string groundedParam = "Grounded";
        [SerializeField] float speedSmoothing = 8f;

        CharacterController _cc;
        float _smoothedSpeed;
        int _speedHash, _groundedHash;
        bool _hasSpeed, _hasGrounded;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            ResolveParams();
        }

        void ResolveParams()
        {
            _hasSpeed = _hasGrounded = false;
            if (animator == null) return;
            _speedHash = Animator.StringToHash(speedParam);
            _groundedHash = Animator.StringToHash(groundedParam);
            foreach (var p in animator.parameters)
            {
                if (p.nameHash == _speedHash) _hasSpeed = true;
                if (p.nameHash == _groundedHash) _hasGrounded = true;
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
            if (_hasGrounded) animator.SetBool(_groundedHash, _cc.isGrounded);
        }
    }
}

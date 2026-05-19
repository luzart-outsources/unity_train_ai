using UnityEngine;

namespace TrainAI.Presentation
{
    // Animator driver for NPCs whose locomotion is transform-only (no
    // CharacterController, no NavMeshAgent.velocity). Reads the host
    // transform's per-frame displacement to derive a movement speed and
    // forwards it to the child Animator's walk-style parameters.
    //
    // Mirrors AnimatorDriver (Player) but without RequireComponent on
    // CharacterController — NPCs use a passive CapsuleCollider trigger
    // for interaction proximity, and their motion comes from
    // SimpleWalkMovementSO/NavMesh/Sentis strategies pushing position
    // directly. The Player-side driver can't be reused because it asks
    // its CharacterController.velocity which NPCs don't have.
    //
    // The parameter resolution logic copies AnimatorDriver's pattern so
    // both Idle↔Walk bool controllers (AdvancedPeopleSystem2 style) and
    // float-Speed blendtrees keep working without per-NPC re-configuration.
    public class NpcAnimatorDriver : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [Tooltip("Float param (e.g. 'Speed') for locomotion blendtrees.")]
        [SerializeField] string speedParam = "Speed";
        [Tooltip("Bool param (e.g. 'walk') for AdvancedPeopleSystem2-style controllers.")]
        [SerializeField] string walkBoolParam = "walk";
        [Tooltip("Bool param (e.g. 'Grounded').")]
        [SerializeField] string groundedParam = "Grounded";
        [Tooltip("Speed threshold (m/s) above which walk bool flips true.")]
        [SerializeField] float walkThreshold = 0.1f;
        [SerializeField] float speedSmoothing = 8f;

        Vector3 _lastPos;
        float _smoothedSpeed;
        int _speedHash, _walkHash, _groundedHash;
        bool _hasSpeed, _hasWalk, _hasGrounded;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _lastPos = transform.position;
            ResolveParams();
        }

        void ResolveParams()
        {
            _hasSpeed = _hasWalk = _hasGrounded = false;
            if (animator == null) return;
            _speedHash = Animator.StringToHash(speedParam);
            _walkHash = Animator.StringToHash(walkBoolParam);
            _groundedHash = Animator.StringToHash(groundedParam);
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
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            Vector3 cur = transform.position;
            Vector3 delta = cur - _lastPos;
            delta.y = 0f;
            float instantSpeed = delta.magnitude / dt;
            _lastPos = cur;

            _smoothedSpeed = Mathf.Lerp(_smoothedSpeed, instantSpeed, dt * speedSmoothing);
            if (_hasSpeed) animator.SetFloat(_speedHash, _smoothedSpeed);
            if (_hasWalk) animator.SetBool(_walkHash, _smoothedSpeed > walkThreshold);
            // NPCs in this project don't physics-simulate, so assume grounded.
            if (_hasGrounded) animator.SetBool(_groundedHash, true);
        }
    }
}

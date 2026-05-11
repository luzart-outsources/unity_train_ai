using UnityEngine;

namespace TrainAI.Player
{
    // Camera top-down theo player.
    public class PlayerCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0, 12f, -8f);
        [SerializeField] private float smooth = 5f;

        public void SetTarget(Transform t) => target = t;

        private float _findCooldown;

        private void LateUpdate()
        {
            if (target == null)
            {
                _findCooldown -= UnityEngine.Time.deltaTime;
                if (_findCooldown <= 0f)
                {
                    _findCooldown = 0.5f;
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null) target = p.transform;
                }
                if (target == null) return;
            }
            Vector3 wanted = target.position + offset;
            transform.position = Vector3.Lerp(transform.position, wanted, smooth * UnityEngine.Time.deltaTime);
            transform.LookAt(target.position);
        }
    }
}

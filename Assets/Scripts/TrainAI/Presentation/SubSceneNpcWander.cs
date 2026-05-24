using UnityEngine;

namespace TrainAI.Presentation
{
    // Cheap ambient walk-cycle for decorative NPCs in sub-scenes (LopHoc /
    // NhaAn / KyTucXa). PingPongs a small lateral offset around the NPC's
    // original spawn position so they look alive without pathfinding,
    // colliders, or AI inference.
    //
    // Why so dumb-simple:
    //   - Sub-scene NPCs are visual filler — "không tương tác làm màu cho
    //     vui" per spec. No interaction, no quest gating, no NPCDirector
    //     registration. So we skip the whole MovementStrategy/Sentis stack
    //     and just lerp a position offset directly.
    //   - 12 ambient NPCs (4+5+3 across 3 scenes) × MovementStrategy.Tick
    //     overhead would be wasted on something the player never engages.
    //     This component costs ~3 ops per frame per NPC.
    //
    // Range, speed, and phase offset are randomised on Start so 12
    // copies aren't visually synchronised — gives natural variety.
    public class SubSceneNpcWander : MonoBehaviour
    {
        [Tooltip("Max lateral distance the NPC drifts from its spawn pos (m).")]
        [SerializeField] float range = 1.5f;
        [Tooltip("Cycles per second.")]
        [SerializeField] float speed = 0.3f;

        Vector3 _origin;
        Vector3 _axis;
        float _phase;

        void Start()
        {
            _origin = transform.position;
            // Random horizontal axis per NPC + per-instance phase so the
            // 12 ambient bodies don't synch into a single tide.
            float a = Random.value * Mathf.PI * 2f;
            _axis = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            _phase = Random.value * Mathf.PI * 2f;
            // Slight per-instance speed variance avoids cycle lock-step.
            speed *= Random.Range(0.7f, 1.4f);
        }

        void Update()
        {
            // Sine wave is gentler than PingPong — no instant direction
            // flip at endpoints which would look robotic.
            float t = Mathf.Sin(Time.time * speed * Mathf.PI * 2f + _phase);
            transform.position = _origin + _axis * (t * range);
        }
    }
}

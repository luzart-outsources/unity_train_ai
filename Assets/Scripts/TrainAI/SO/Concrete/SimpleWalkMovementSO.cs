using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    // Lightweight transform-only NPC mover. Walks toward Target at `speed`
    // m/s, snaps to the ground via a downward raycast each tick, and yaws
    // to face the heading. Designed for a campus with no baked NavMesh
    // (10_World currently has m_NavMeshData=0) so the NavMesh strategy
    // would silently no-op. SimpleWalk doesn't pathfind around obstacles,
    // but for the GDD's free-roam student NPCs ("học sinh đi lại theo
    // thời gian biểu"), straight-line locomotion between scheduled
    // AreaSO waypoints reads correctly from the player's perspective and
    // avoids the cost of baking a 200×140 NavMesh.
    //
    // Update cadence: MovementService.Tick fires at 0.2s intervals, so
    // each Tick gets ~0.2s of dt. Speed scales accordingly — 2.5 m/s ⇒
    // 0.5m per tick, which is visible motion but doesn't teleport.
    [CreateAssetMenu(fileName = "Movement_SimpleWalk", menuName = "TrainAI/Movement/Simple Walk")]
    public class SimpleWalkMovementSO : MovementStrategySO
    {
        [Tooltip("Walking speed in metres per second.")]
        public float speed = 2.0f;
        [Tooltip("Distance under which the agent is considered 'arrived'. Prevents oscillation.")]
        public float arriveRadius = 0.5f;
        [Tooltip("Degrees per second the agent yaws toward heading. 360 = snap.")]
        public float turnSpeed = 240f;

        public override IMovementAgent Bind(Transform npc)
            => new SimpleWalkAgent(npc, speed, arriveRadius, turnSpeed);
    }

    internal class SimpleWalkAgent : IMovementAgent
    {
        readonly Transform _t;
        readonly float _speed;
        readonly float _arriveRadius;
        readonly float _turnSpeed;

        // Cached so NpcAnimatorDriver can read a smoothed "is moving" signal
        // without doing its own velocity estimation. The animator-side driver
        // reads transform position deltas anyway, but exposing this here also
        // helps headless tests verify the agent is actually walking.
        public Vector3 Target { get; set; }

        public SimpleWalkAgent(Transform t, float speed, float arriveRadius, float turnSpeed)
        {
            _t = t;
            _speed = speed;
            _arriveRadius = arriveRadius;
            _turnSpeed = turnSpeed;
        }

        public void Tick(object _, float dt)
        {
            if (_t == null) return;
            Vector3 cur = _t.position;
            Vector3 to = Target - cur;
            to.y = 0f; // ignore vertical separation; ground-snap handles y
            float dist = to.magnitude;
            if (dist < _arriveRadius) return; // arrived, hold still

            Vector3 dir = to / dist;
            float step = Mathf.Min(_speed * dt, dist);
            Vector3 next = cur + dir * step;

            // Ground-snap: cast down from above the agent's head. Without
            // this, NPCs that spawn on a slope or near the static "Polish"
            // primitive walls slide off the ground into y < 0.
            if (Physics.Raycast(next + Vector3.up * 2f, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore))
                next.y = hit.point.y;
            else
                next.y = 0f;

            _t.position = next;

            // Yaw toward heading. Quaternion.RotateTowards keeps it bounded
            // so an NPC turning into a sharp corner doesn't pop visually.
            Quaternion want = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z), Vector3.up);
            _t.rotation = Quaternion.RotateTowards(_t.rotation, want, _turnSpeed * dt);
        }
    }
}

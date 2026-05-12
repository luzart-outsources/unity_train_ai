using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.AI;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Movement_NavMesh", menuName = "TrainAI/Movement/NavMesh")]
    public class NavMeshMovementSO : MovementStrategySO
    {
        public float speed = 2.5f;
        public float angularSpeed = 180f;
        public float stoppingDistance = 0.3f;

        public override IMovementAgent Bind(Transform npc)
            => new NavMeshMovementAgent(npc, speed, angularSpeed, stoppingDistance);
    }

    internal class NavMeshMovementAgent : IMovementAgent
    {
        readonly NavMeshAgent _agent;
        public Vector3 Target { get; set; }

        public NavMeshMovementAgent(Transform npc, float speed, float angularSpeed, float stoppingDistance)
        {
            _agent = npc != null ? npc.GetComponent<NavMeshAgent>() : null;
            if (_agent == null && npc != null) _agent = npc.gameObject.AddComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.speed = speed;
                _agent.angularSpeed = angularSpeed;
                _agent.stoppingDistance = stoppingDistance;
            }
        }

        public void Tick(object _, float __)
        {
            if (_agent == null) return;
            if ((_agent.destination - Target).sqrMagnitude > 0.01f)
                _agent.SetDestination(Target);
        }
    }
}

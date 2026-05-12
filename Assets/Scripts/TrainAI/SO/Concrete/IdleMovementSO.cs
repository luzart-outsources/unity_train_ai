using UnityEngine;
using TrainAI.Core;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Movement_Idle", menuName = "TrainAI/Movement/Idle")]
    public class IdleMovementSO : MovementStrategySO
    {
        public override IMovementAgent Bind(Transform npc) => new IdleMovementAgent(npc);
    }

    internal class IdleMovementAgent : IMovementAgent
    {
        readonly Transform _t;
        public Vector3 Target { get; set; }

        public IdleMovementAgent(Transform t) { _t = t; }
        public void Tick(object workerOrAgent, float dt) { }
    }
}

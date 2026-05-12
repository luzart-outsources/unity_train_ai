using TrainAI.Core;
using TrainAI.SO.Base;
using TrainAI.Sentis;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Movement_Onnx", menuName = "TrainAI/Movement/Onnx")]
    public class OnnxMovementSO : MovementStrategySO
    {
        [Header("Tuning - must match soldier.onnx training env")]
        public float maxSpeed = 3.5f;
        public float maxTurnRadPerSec = Mathf.PI;
        public float rayMaxDist = 10f;
        public int numRays = 8;
        public float arenaDiagonal = 35.36f;
        public float agentRadius = 0.5f;

        [Header("Layer masks")]
        public LayerMask obstacleMask = ~0;
        public LayerMask targetMask = 0;

        public override IMovementAgent Bind(Transform npc)
            => new OnnxMovementAgent(npc, maxSpeed, maxTurnRadPerSec, rayMaxDist,
                                    numRays, arenaDiagonal, agentRadius,
                                    obstacleMask, targetMask);
    }
}

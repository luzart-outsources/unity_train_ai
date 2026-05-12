using TrainAI.Core;
using UnityEngine;

namespace TrainAI.SO.Base
{
    public abstract class MovementStrategySO : ScriptableObject
    {
        public abstract IMovementAgent Bind(Transform npc);
    }
}

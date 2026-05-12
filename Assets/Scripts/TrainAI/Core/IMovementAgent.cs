using UnityEngine;

namespace TrainAI.Core
{
    public interface IMovementAgent
    {
        Vector3 Target { get; set; }
        void Tick(object workerOrAgent, float dt);
    }
}

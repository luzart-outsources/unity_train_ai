using UnityEngine;

namespace TrainAI.SO.Base
{
    public interface IMovementAgent
    {
        Vector3 Target { get; set; }
        void Tick(object workerOrAgent, float dt);
    }
}

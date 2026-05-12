using UnityEngine;

namespace TrainAI.Core
{
    public abstract class RuntimeSO : ScriptableObject
    {
        void OnEnable() => Reset();
        public abstract void Reset();
    }
}

using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TrainAI.SO.Base
{
    public abstract class DialogueStrategySO : ScriptableObject
    {
        public abstract UniTask<string> Reply(string userInput, NpcContext ctx);
    }
}

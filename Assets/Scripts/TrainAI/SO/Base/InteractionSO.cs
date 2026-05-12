using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TrainAI.SO.Base
{
    public abstract class InteractionSO : ScriptableObject
    {
        public string promptText = "Bam E de tuong tac";
        public Sprite icon;

        public abstract UniTask Execute(InteractionContext ctx);
    }
}

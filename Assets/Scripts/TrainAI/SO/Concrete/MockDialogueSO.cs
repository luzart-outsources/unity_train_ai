using UnityEngine;
using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Dialogue_Mock", menuName = "TrainAI/Dialogue/Mock")]
    public class MockDialogueSO : DialogueStrategySO
    {
        public override UniTask<string> Reply(string input, NpcContext ctx)
            => UniTask.FromResult($"[mock-reply to: {input}]");
    }
}

using Cysharp.Threading.Tasks;
using TrainAI.Core;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Dialogue_Onnx", menuName = "TrainAI/Dialogue/Onnx")]
    public class OnnxDialogueSO : DialogueStrategySO
    {
        [Range(0f, 1f)] public float confidenceThreshold = 0.5f;
        public IntentId fallback = IntentId.OUT_OF_SCOPE;

        public override UniTask<string> Reply(string input, NpcContext ctx)
        {
            if (ctx == null || ctx.tokenizer == null || ctx.intentClassifier == null || ctx.responseFiller == null)
                return UniTask.FromResult("[onnx khong san sang]");

            var tokens = ctx.tokenizer.Encode(input);
            var result = ctx.intentClassifier.Predict(tokens);
            var intent = result.score < confidenceThreshold ? fallback : result.intent;
            var filled = ctx.responseFiller.Fill(intent, ctx.fillTemplate);
            return UniTask.FromResult(filled);
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using TrainAI.Configs;
using UnityEngine;

namespace TrainAI.Systems.Dialogue
{
    // Stub fallback - tra ve fallback response random tu NPCProfile.
    // Quyen wire Sentis ONNX sau bang cach implement IPhaseAChatService.
    public class PhaseAChatStub : IPhaseAChatService
    {
        public async UniTask<string> RespondAsync(string playerInput, NPCProfileSO npc, string playerName, CancellationToken ct)
        {
            // Simulate think time.
            await UniTask.Delay(400, cancellationToken: ct);

            if (npc == null || npc.fallbackResponses == null || npc.fallbackResponses.Count == 0)
                return "...";

            int idx = Random.Range(0, npc.fallbackResponses.Count);
            return npc.fallbackResponses[idx];
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using TrainAI.Configs;

namespace TrainAI.Systems.Dialogue
{
    // Interface cho Sentis chat NPC. DialogueManager dung interface, khong biet implementation.
    // Default: PhaseAChatStub - rule-based fallback khi chua wire ONNX.
    public interface IPhaseAChatService
    {
        UniTask<string> RespondAsync(string playerInput, NPCProfileSO npc, string playerName, CancellationToken ct);
    }
}

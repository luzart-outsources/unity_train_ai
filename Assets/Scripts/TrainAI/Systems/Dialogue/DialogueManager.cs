using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.UI.Components;
using UnityEngine;

namespace TrainAI.Systems.Dialogue
{
    // Don gian: chi mo UIDialogue + ket noi NPC. Logic chat AI nam THANG trong DialogueScreen
    // theo yeu cau "Chat gan vao UIDialogue".
    public class DialogueManager
    {
        public async UniTask OpenDialogueAsync(NPCProfileSO npc, CancellationToken ct = default)
        {
            if (npc == null) return;
            var data = new DialogueData { Npc = npc };
            await UIManager.Instance.ShowAsync(UIIdGame.Dialogue, new UIContext(data), default, ct);
        }

        // Pick 1 reply theo behavior GDD-defined (fallback responses tu NPCProfile).
        // Quyen co the wire Sentis ONNX o day sau khi muon.
        public string GetReply(string playerText, NPCProfileSO npc)
        {
            if (npc == null || npc.fallbackResponses == null || npc.fallbackResponses.Count == 0)
                return "...";
            int idx = Random.Range(0, npc.fallbackResponses.Count);
            return npc.fallbackResponses[idx];
        }
    }
}

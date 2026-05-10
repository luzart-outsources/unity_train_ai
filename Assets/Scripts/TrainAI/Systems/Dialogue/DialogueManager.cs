using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.UI.Components;

namespace TrainAI.Systems.Dialogue
{
    public class DialogueManager
    {
        private readonly IPhaseAChatService _chat;

        public IPhaseAChatService ChatService => _chat;

        public DialogueManager(IPhaseAChatService chat)
        {
            _chat = chat ?? new PhaseAChatStub();
        }

        public async UniTask OpenDialogueAsync(NPCProfileSO npc, CancellationToken ct = default)
        {
            if (npc == null) return;
            var data = new DialogueData { Npc = npc };
            await UIManager.Instance.ShowAsync(UIIdGame.Dialogue, new UIContext(data), default, ct);
            // Caller co the chosse await data.ResultTcs neu can.
        }

        public async UniTask<string> GetReplyAsync(string playerText, NPCProfileSO npc, CancellationToken ct)
        {
            string playerName = GameServices.Player != null ? GameServices.Player.Name : "ban";
            return await _chat.RespondAsync(playerText ?? "", npc, playerName, ct);
        }
    }
}

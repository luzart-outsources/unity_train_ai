using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;

namespace TrainAI.Services
{
    public class DialogueService : IDialogueService
    {
        readonly IQuestRouter _quests;
        readonly PlayerStateRSO _player;
        readonly ResponseTemplatesSO _templates;
        readonly ISentisRuntime _sentis;

        public DialogueService(IQuestRouter quests, PlayerStateRSO player,
                               ResponseTemplatesSO templates, ISentisRuntime sentis)
        {
            _quests = quests;
            _player = player;
            _templates = templates;
            _sentis = sentis;
        }

        public UniTask<string> Reply(NPCSO npc, string userInput)
        {
            if (npc == null || npc.dialogue == null)
                return UniTask.FromResult("[npc khong co dialogue]");
            var ctx = new NpcContext
            {
                playerName = _player != null ? _player.playerName : "",
                todaySummary = _quests != null ? _quests.GetTodaySummary() : "",
                hocTap = _player != null ? _player.hocTap : 0,
                renLuyen = _player != null ? _player.renLuyen : 0,
                fillTemplate = s => s
            };
            return npc.dialogue.Reply(userInput, ctx);
        }
    }
}

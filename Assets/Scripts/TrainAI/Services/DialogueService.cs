using Cysharp.Threading.Tasks;
using TrainAI.Core;
using TrainAI.SO.Base;

namespace TrainAI.Services
{
    public class DialogueService : IDialogueService
    {
        readonly IQuestRouter _quests;
        readonly PlayerStateRSO _player;
        readonly ResponseTemplatesSO _templates;
        readonly ISentisRuntime _sentis;
        readonly AreaDB _areaDB;

        public DialogueService(IQuestRouter quests, PlayerStateRSO player,
                               ResponseTemplatesSO templates, ISentisRuntime sentis,
                               AreaDB areaDB = null)
        {
            _quests = quests;
            _player = player;
            _templates = templates;
            _sentis = sentis;
            _areaDB = areaDB;
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
                tokenizer = _sentis?.Tokenizer,
                intentClassifier = _sentis?.IntentClassifier,
                responseFiller = _sentis?.ResponseFiller,
            };
            ctx.fillTemplate = key => LookupTag(key, ctx);
            return npc.dialogue.Reply(userInput, ctx);
        }

        string LookupTag(string key, NpcContext ctx)
        {
            return key switch
            {
                "scheduled_today" => ctx.todaySummary,
                "playerName" => ctx.playerName,
                "hocTap" => ctx.hocTap.ToString(),
                "renLuyen" => ctx.renLuyen.ToString(),
                "meal_time" => "11h30 trua va 18h toi",
                "minutes_to_meal" => "30",
                "place" => _areaDB != null && _player != null
                    ? (_areaDB.NearestTo(_player.lastWorldPos)?.id ?? "khu doanh trai")
                    : "khu doanh trai",
                "direction" => "dong",
                "distance" => "100",
                "block" => "B5",
                "topic" => "dieu do",
                "chapter" => "3",
                "summary" => "thao, lap, bao duong",
                _ => null
            };
        }
    }
}

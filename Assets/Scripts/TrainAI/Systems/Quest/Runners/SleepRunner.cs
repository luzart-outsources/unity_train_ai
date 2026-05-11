using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.UI.Components;

namespace TrainAI.Systems.Quest.Runners
{
    // Sleep: UIConfirm "Di ngu?" -> save -> next day.
    public class SleepRunner : IQuestRunner
    {
        public async UniTask<int> RunAsync(QuestDefSO quest, CancellationToken ct)
        {
            if (quest == null) return 0;
            var data = new ConfirmData
            {
                Title = quest.title,
                Message = quest.confirmText,
                OkLabel = quest.okButtonText,
            };
            await Luzart.UIManager.Instance.ShowAsync(UIIdGame.Confirm, new UIContext(data), default, ct);
            await data.ResultTcs.Task.AttachExternalCancellation(ct);

            // Save game.
            GameServices.Save?.Save();

            // Next day - QuestManager will react via GameClock.OnNewDay.
            if (GameServices.Time != null)
            {
                int firstHour = 5;
                int firstMin = 0;
                if (GameServices.Database != null && GameServices.Database.timeConfig != null)
                {
                    firstHour = GameServices.Database.timeConfig.defaultDayStartHour;
                    firstMin = GameServices.Database.timeConfig.defaultDayStartMinute;
                }
                GameServices.Time.NextDay(firstHour, firstMin);
            }
            return 0;
        }
    }
}

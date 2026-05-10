using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.UI.Components;

namespace TrainAI.Systems.Quest.Runners
{
    // Base cho cac quest type chi can show UIConfirm va return 0 (Exercise/Cleaning/Eat).
    public abstract class ConfirmRunnerBase : IQuestRunner
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
            await UIManager.Instance.ShowAsync(UIIdGame.Confirm, new UIContext(data), default, ct);
            await data.ResultTcs.Task.AttachExternalCancellation(ct);
            return 0;
        }
    }

    public class ExerciseRunner : ConfirmRunnerBase { }
    public class CleaningRunner : ConfirmRunnerBase { }
    public class EatRunner : ConfirmRunnerBase { }
    public class FreeRoamRunner : ConfirmRunnerBase { }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using TrainAI.Configs;

namespace TrainAI.Systems.Quest
{
    // Strategy interface - 1 runner / QuestType.
    // Return: int = score earned (chi co Study* return > 0).
    public interface IQuestRunner
    {
        UniTask<int> RunAsync(QuestDefSO quest, CancellationToken ct);
    }
}

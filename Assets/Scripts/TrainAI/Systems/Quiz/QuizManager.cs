using System.Threading;
using Cysharp.Threading.Tasks;
using Luzart;
using TrainAI.Configs;
using TrainAI.UI.Components;

namespace TrainAI.Systems.Quiz
{
    public class QuizManager
    {
        // Show QuizScreen va return correctCount.
        // Caller (StudyRunner) tinh score = correctCount * pointsPerQuizQuestion.
        public async UniTask<int> RunAsync(QuizSetSO set, CancellationToken ct)
        {
            if (set == null || set.Count == 0) return 0;

            var data = new QuizData { Set = set };
            // UIId.Quiz - dat trong UIIdGame extension hoac tao entry trong UIRegistry.
            // Cast int -> UIId neu can.
            await UIManager.Instance.ShowAsync((UIId)2010, new UIContext(data), default, ct);
            int correct = await data.ResultTcs.Task.AttachExternalCancellation(ct);
            return correct;
        }
    }
}

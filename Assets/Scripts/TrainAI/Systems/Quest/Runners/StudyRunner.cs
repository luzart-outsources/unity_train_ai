using System.Threading;
using Cysharp.Threading.Tasks;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using UnityEngine;

namespace TrainAI.Systems.Quest.Runners
{
    // Run quiz va tra ve so diem dat duoc.
    // Score = correctCount * pointsPerQuizQuestion (lam tron), tu ScoreConfigSO.
    public class StudyRunner : IQuestRunner
    {
        public async UniTask<int> RunAsync(QuestDefSO quest, CancellationToken ct)
        {
            if (quest == null || quest.quizSet == null)
            {
                Debug.LogWarning($"[StudyRunner] Quest {quest?.id} has no quizSet, skipping with 0 score.");
                return 0;
            }

            var qm = GameServices.Quiz;
            if (qm == null)
            {
                Debug.LogError("[StudyRunner] QuizManager not initialized.");
                return 0;
            }

            int correct = await qm.RunAsync(quest.quizSet, ct);

            float ppq = GameServices.Score != null && GameServices.Score.Config != null
                ? GameServices.Score.Config.pointsPerQuizQuestion
                : 1f;
            int score = Mathf.RoundToInt(correct * ppq);
            return score;
        }
    }
}

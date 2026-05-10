using System.Threading;
using Cysharp.Threading.Tasks;
using TrainAI.Configs;
using TrainAI.Core.Bootstrap;
using TrainAI.Core.Events;
using UnityEngine;

namespace TrainAI.Systems.Quest
{
    // Host wire QuestManager <-> Runners. Subscribe QuestStarted -> chay runner async.
    // Gan vao GameBootstrap GO.
    public class QuestRunnerHost : MonoBehaviour
    {
        [SerializeField] private bool autoTravelToTargetScene = true;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            GameEvents.QuestStarted += OnQuestStarted;
        }

        private void OnDisable()
        {
            GameEvents.QuestStarted -= OnQuestStarted;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private void OnQuestStarted(QuestDefSO quest)
        {
            _cts?.Cancel();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            RunQuestAsync(quest, _cts.Token).Forget();
        }

        private async UniTaskVoid RunQuestAsync(QuestDefSO quest, CancellationToken ct)
        {
            if (quest == null) return;
            var registry = QuestRunnerProvider.Registry;
            var runner = registry?.Get(quest.type);
            if (runner == null)
            {
                Debug.LogWarning($"[QuestRunnerHost] No runner for {quest.type}, completing 0.");
                GameServices.Quest?.CompleteCurrentQuest(0);
                return;
            }

            // Travel to target scene if needed.
            if (autoTravelToTargetScene && quest.targetScene != null)
            {
                if (GameServices.SceneFlow != null)
                    await GameServices.SceneFlow.TravelAsync(quest.targetScene, ct);
            }

            int score = 0;
            try
            {
                score = await runner.RunAsync(quest, ct);
            }
            catch (System.OperationCanceledException) { return; }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestRunnerHost] Runner failed: {e}");
            }

            GameServices.Quest?.CompleteCurrentQuest(score);

            // Travel back to World scene neu da o scene khac.
            if (autoTravelToTargetScene && quest.targetScene != null && GameServices.SceneFlow != null)
            {
                var worldRoute = FindWorldRoute();
                if (worldRoute != null)
                    await GameServices.SceneFlow.TravelAsync(worldRoute, ct);
            }
        }

        private static SceneRouteSO FindWorldRoute()
        {
            var db = GameServices.Database;
            if (db == null || db.scenes == null) return null;
            for (int i = 0; i < db.scenes.Count; i++)
            {
                var r = db.scenes[i];
                if (r != null && (r.id == "R_World" || r.sceneName == "World")) return r;
            }
            return null;
        }
    }
}

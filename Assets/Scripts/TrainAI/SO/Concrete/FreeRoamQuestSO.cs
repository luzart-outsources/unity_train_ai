using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.SO.Concrete
{
    [CreateAssetMenu(fileName = "Quest_FreeRoam_New", menuName = "TrainAI/Quest/Free Roam")]
    public class FreeRoamQuestSO : QuestSO
    {
        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new FreeRoamQuestRuntime();
    }

    internal class FreeRoamQuestRuntime : IQuestRuntime
    {
        public void Begin() { }
        public void Tick(float dt) { }
        public void OnComplete(bool success) { }
    }
}

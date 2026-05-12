using NUnit.Framework;
using UnityEngine;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;

namespace TrainAI.EditMode.Tests
{
    public class GotoConfirmQuestSOTests
    {
        [Test]
        public void CreateRuntime_ReturnsNonNull()
        {
            var q = ScriptableObject.CreateInstance<GotoConfirmQuestSO>();
            q.id = "test"; q.title = "Test";
            q.area = ScriptableObject.CreateInstance<AreaSO>();
            var rt = q.CreateRuntime(new QuestContext());
            Assert.NotNull(rt);
            var area = q.area;
            Object.DestroyImmediate(q);
            Object.DestroyImmediate(area);
        }
    }
}

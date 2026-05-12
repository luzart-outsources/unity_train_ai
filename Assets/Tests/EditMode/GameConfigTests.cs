using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;
using TrainAI.SO.Base;

namespace TrainAI.EditMode.Tests
{
    public class GameConfigTests
    {
        [Test]
        public void Defaults_MatchSpec()
        {
            var c = ScriptableObject.CreateInstance<GameConfigSO>();
            Assert.AreEqual(30, c.totalDays);
            Assert.IsTrue(c.skipWeekend);
            Assert.AreEqual(100, c.startRenLuyen);
            Assert.AreEqual(480, c.maxHocTap);
            Assert.AreEqual(BackendKind.GPUCompute, c.preferredBackend);
            Object.DestroyImmediate(c);
        }
    }
}

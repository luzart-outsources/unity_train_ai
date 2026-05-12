using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests
{
    public class RuntimeSOTests
    {
        public class CounterRSO : RuntimeSO
        {
            public int counter;
            public override void Reset() => counter = 0;
        }

        [Test]
        public void OnEnable_ResetsState()
        {
            var so = ScriptableObject.CreateInstance<CounterRSO>();
            so.counter = 99;
            ScriptableObject.DestroyImmediate(so);
            so = ScriptableObject.CreateInstance<CounterRSO>();
            Assert.AreEqual(0, so.counter, "OnEnable should call Reset()");
            ScriptableObject.DestroyImmediate(so);
        }
    }
}

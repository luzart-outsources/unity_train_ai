using NUnit.Framework;
using UnityEngine;
using TrainAI.Services;

namespace TrainAI.EditMode.Tests
{
    public class SkeletonSmokeTests
    {
        [Test]
        public void ServiceLocator_CanInstantiate()
        {
            var so = ScriptableObject.CreateInstance<ServiceLocatorSO>();
            Assert.NotNull(so, "ServiceLocatorSO should be instantiable");
            so.Bootstrap();
            ScriptableObject.DestroyImmediate(so);
        }
    }
}

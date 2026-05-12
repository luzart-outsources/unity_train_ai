using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;
using TrainAI.SO.Base;

namespace TrainAI.EditMode.Tests
{
    public class DayDBTests
    {
        [Test]
        public void ByIndex_ReturnsMatchingDay()
        {
            var db = ScriptableObject.CreateInstance<DayDB>();
            var d1 = ScriptableObject.CreateInstance<DaySO>();
            var d2 = ScriptableObject.CreateInstance<DaySO>();
            d1.dayIndex = 1; d1.weekday = Weekday.Monday;
            d2.dayIndex = 2; d2.weekday = Weekday.Tuesday;
            db.all.Add(d1); db.all.Add(d2);

            Assert.AreSame(d1, db.ByIndex(1));
            Assert.AreSame(d2, db.ByIndex(2));
            Assert.IsNull(db.ByIndex(99));

            Object.DestroyImmediate(db);
            Object.DestroyImmediate(d1);
            Object.DestroyImmediate(d2);
        }
    }
}

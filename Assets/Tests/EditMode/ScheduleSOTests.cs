using NUnit.Framework;
using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.EditMode.Tests
{
    public class ScheduleSOTests
    {
        [Test]
        public void GetEntryAt_ReturnsLastEntryAtOrBefore()
        {
            var s = ScriptableObject.CreateInstance<ScheduleSO>();
            s.entries.Add(new ScheduleEntry { hour = 5, minute = 0 });
            s.entries.Add(new ScheduleEntry { hour = 7, minute = 30 });
            s.entries.Add(new ScheduleEntry { hour = 11, minute = 30 });

            Assert.AreEqual(5, s.GetEntryAt(1, 6, 0).hour);
            Assert.AreEqual(7, s.GetEntryAt(1, 7, 30).hour);
            Assert.AreEqual(7, s.GetEntryAt(1, 10, 0).hour);
            Assert.AreEqual(11, s.GetEntryAt(1, 12, 0).hour);

            Object.DestroyImmediate(s);
        }

        [Test]
        public void GetEntryAt_BeforeFirst_ReturnsDefault()
        {
            var s = ScriptableObject.CreateInstance<ScheduleSO>();
            s.entries.Add(new ScheduleEntry { hour = 7, minute = 0 });

            var e = s.GetEntryAt(1, 5, 0);
            Assert.AreEqual(0, e.hour);
            Assert.AreEqual(0, e.minute);

            Object.DestroyImmediate(s);
        }
    }
}

using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests
{
    public class TimeRangeTests
    {
        [Test]
        public void Contains_HourInRange_ReturnsTrue()
        {
            var r = new TimeRange(5, 0, 5, 15);
            Assert.IsTrue(r.Contains(5, 10));
        }

        [Test]
        public void Contains_HourOutOfRange_ReturnsFalse()
        {
            var r = new TimeRange(5, 0, 5, 15);
            Assert.IsFalse(r.Contains(5, 20));
            Assert.IsFalse(r.Contains(4, 59));
        }

        [Test]
        public void TotalMinutes_Compute()
        {
            var r = new TimeRange(7, 30, 11, 30);
            Assert.AreEqual(240, r.TotalMinutes);
        }
    }
}

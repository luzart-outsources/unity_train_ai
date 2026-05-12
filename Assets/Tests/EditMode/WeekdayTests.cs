using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests
{
    public class WeekdayTests
    {
        [Test] public void Next_FromFriday_SkipsWeekend_ToMonday()
            => Assert.AreEqual(Weekday.Monday, WeekdayHelper.Next(Weekday.Friday, skipWeekend: true));

        [Test] public void Next_FromMonday_NoSkip_GoesToTuesday()
            => Assert.AreEqual(Weekday.Tuesday, WeekdayHelper.Next(Weekday.Monday, skipWeekend: false));

        [Test] public void Next_FromSaturday_SkipFalse_GoesToSunday()
            => Assert.AreEqual(Weekday.Sunday, WeekdayHelper.Next(Weekday.Saturday, skipWeekend: false));
    }
}

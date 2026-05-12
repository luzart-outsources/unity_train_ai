namespace TrainAI.Core
{
    public enum Weekday { Monday = 1, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

    public static class WeekdayHelper
    {
        public static Weekday Next(Weekday current, bool skipWeekend)
        {
            var next = (Weekday)(((int)current % 7) + 1);
            if (skipWeekend && (next == Weekday.Saturday || next == Weekday.Sunday))
                return Weekday.Monday;
            return next;
        }
    }
}

using System;

namespace TrainAI.Core
{
    [Serializable]
    public struct TimeRange
    {
        public int startHour, startMinute;
        public int endHour, endMinute;

        public TimeRange(int sh, int sm, int eh, int em)
        {
            startHour = sh; startMinute = sm; endHour = eh; endMinute = em;
        }

        public bool Contains(int hour, int minute)
        {
            int t = hour * 60 + minute;
            int s = startHour * 60 + startMinute;
            int e = endHour * 60 + endMinute;
            return t >= s && t < e;
        }

        public int TotalMinutes => (endHour * 60 + endMinute) - (startHour * 60 + startMinute);

        public override string ToString() => $"{startHour:D2}:{startMinute:D2}-{endHour:D2}:{endMinute:D2}";
    }
}

using System;

namespace TrainAI.Core.Time
{
    // Snapshot thoi gian in-game. Truyen vao moi tick.
    [Serializable]
    public struct GameTime
    {
        public int day;          // 1..30 (theo GDD: choi 30 ngay)
        public int hour;         // 0..23
        public int minute;       // 0..59
        public DayOfWeek weekday; // T2..CN

        public int TotalMinutes => hour * 60 + minute;

        public static GameTime FromTotalMinutes(int day, int totalMinutes, DayOfWeek weekday)
        {
            int safeMin = totalMinutes < 0 ? 0 : totalMinutes;
            int h = (safeMin / 60) % 24;
            int m = safeMin % 60;
            return new GameTime { day = day, hour = h, minute = m, weekday = weekday };
        }

        public override string ToString() => $"D{day} {weekday} {hour:D2}:{minute:D2}";
    }
}

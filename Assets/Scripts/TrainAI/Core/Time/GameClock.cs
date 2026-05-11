using System;
using TrainAI.Configs;

namespace TrainAI.Core.Time
{
    // Dong ho in-game thuan logic. KHONG MonoBehaviour - testable.
    // TimeTickDriver gan vao 1 GameObject va goi Tick(deltaTime) moi frame.
    public class GameClock
    {
        private readonly TimeConfigSO _cfg;
        private GameTime _now;
        private float _accumSeconds;
        private bool _frozen;

        public GameTime Now => _now;
        public bool IsFrozen => _frozen;
        public TimeConfigSO Config => _cfg;

        public event Action<GameTime> OnTick;
        public event Action<int> OnNewDay;       // arg = new day number
        public event Action<GameTime> OnHourChanged;

        public GameClock(TimeConfigSO cfg)
        {
            _cfg = cfg;
            Reset();
        }

        public void Reset()
        {
            _now = new GameTime
            {
                day = _cfg != null ? _cfg.firstDay : 1,
                hour = _cfg != null ? _cfg.defaultDayStartHour : 5,
                minute = _cfg != null ? _cfg.defaultDayStartMinute : 0,
                weekday = _cfg != null ? _cfg.weekStart : DayOfWeek.Monday,
            };
            _accumSeconds = 0f;
            _frozen = false;
        }

        public void Freeze() => _frozen = true;
        public void Resume() => _frozen = false;

        // Goi tu MonoBehaviour Update voi Time.deltaTime.
        public void Tick(float deltaRealSeconds)
        {
            if (_frozen || _cfg == null || _cfg.secondsPerGameHour <= 0f) return;

            float secsPerMin = _cfg.secondsPerGameHour / 60f;   // 180/60 = 3s = 1p game
            _accumSeconds += deltaRealSeconds;
            while (_accumSeconds >= secsPerMin)
            {
                _accumSeconds -= secsPerMin;
                AdvanceMinutes(_cfg.tickEveryGameMinutes);
            }
        }

        private void AdvanceMinutes(int minutes)
        {
            int prevHour = _now.hour;
            int totalMin = _now.hour * 60 + _now.minute + minutes;

            int dayCarry = 0;
            while (totalMin >= 24 * 60)
            {
                totalMin -= 24 * 60;
                dayCarry++;
            }

            _now.hour = totalMin / 60;
            _now.minute = totalMin % 60;

            if (dayCarry > 0)
            {
                for (int i = 0; i < dayCarry; i++) AdvanceDayInternal();
            }

            if (_now.hour != prevHour) OnHourChanged?.Invoke(_now);
            OnTick?.Invoke(_now);
        }

        // Public: dung khi quest skip thoi gian (vd 5h tap xong jump 6h).
        public void JumpTo(int hour, int minute)
        {
            int safeHour = ClampHour(hour);
            int safeMin = ClampMinute(minute);
            int targetTotal = safeHour * 60 + safeMin;
            int currentTotal = _now.hour * 60 + _now.minute;
            int delta = targetTotal - currentTotal;
            if (delta < 0) delta += 24 * 60; // wrap qua nua dem
            AdvanceMinutes(delta);
        }

        public void SetTime(int hour, int minute)
        {
            _now.hour = ClampHour(hour);
            _now.minute = ClampMinute(minute);
            OnTick?.Invoke(_now);
        }

        // Sang ngay moi - skip thu 7 + CN neu cau hinh.
        public void NextDay(int? resetHour = null, int? resetMinute = null)
        {
            AdvanceDayInternal();
            if (resetHour.HasValue) _now.hour = ClampHour(resetHour.Value);
            if (resetMinute.HasValue) _now.minute = ClampMinute(resetMinute.Value);
            OnTick?.Invoke(_now);
        }

        private void AdvanceDayInternal()
        {
            _now.day++;
            _now.weekday = NextWeekday(_now.weekday);

            if (_cfg != null && _cfg.skipWeekend)
            {
                // GDD: het ngay 6 (Sat) -> chuyen sang T2 luon. Nghia la skip Sat va Sun.
                // Logic: neu sau khi advance, weekday la Sat -> add 2 ngay (Sun, Mon).
                if (_now.weekday == DayOfWeek.Saturday)
                {
                    _now.day += 2;
                    _now.weekday = DayOfWeek.Monday;
                }
                else if (_now.weekday == DayOfWeek.Sunday)
                {
                    _now.day += 1;
                    _now.weekday = DayOfWeek.Monday;
                }
            }

            OnNewDay?.Invoke(_now.day);
        }

        private static DayOfWeek NextWeekday(DayOfWeek d)
        {
            int n = ((int)d + 1) % 7;
            return (DayOfWeek)n;
        }

        private static int ClampHour(int h) => h < 0 ? 0 : (h > 23 ? 23 : h);
        private static int ClampMinute(int m) => m < 0 ? 0 : (m > 59 ? 59 : m);
    }
}

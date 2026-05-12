using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;

namespace TrainAI.Services
{
    public class GameClockService : IGameClock
    {
        readonly TimeConfigSO _config;
        readonly GameClockRSO _state;
        readonly GameConfigSO _gameConfig;
        float _fractionalMinutes;

        public GameClockService(TimeConfigSO config, GameClockRSO state, GameConfigSO gameConfig)
        {
            _config = config;
            _state = state;
            _gameConfig = gameConfig;
        }

        public int Day => _state.day;
        public int Hour => _state.hour;
        public int Minute => _state.minute;
        public bool Frozen => _state.frozen;

        public void Tick(float realDeltaSec)
        {
            if (_state.frozen) return;
            float gameMinutesPerRealSec = (_config.gameHourPerRealMinute * 60f) / 60f;
            _fractionalMinutes += realDeltaSec * gameMinutesPerRealSec;

            while (_fractionalMinutes >= 1f)
            {
                _fractionalMinutes -= 1f;
                _state.minute += 1;
                if (_state.minute >= 60)
                {
                    _state.minute = 0;
                    _state.hour += 1;
                    if (_state.hour >= 24)
                    {
                        _state.hour = 0;
                        AdvanceDay();
                    }
                }
                BroadcastService.Send(new TimeTickMsg(_state.hour, _state.minute));
            }
        }

        public void Freeze() => _state.frozen = true;
        public void Resume() => _state.frozen = false;

        public void SkipTo(int hour, int minute)
        {
            _state.hour = hour;
            _state.minute = minute;
            _fractionalMinutes = 0f;
            BroadcastService.Send(new TimeTickMsg(_state.hour, _state.minute));
        }

        public void AdvanceDay()
        {
            BroadcastService.Send(new DayEndedMsg(_state.day));
            _state.day += 1;
            _state.weekday = WeekdayHelper.Next(_state.weekday, _gameConfig != null && _gameConfig.skipWeekend);
            BroadcastService.Send(new DayStartedMsg(_state.day, _state.weekday));
        }
    }
}

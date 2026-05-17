using System.Collections.Generic;
using TrainAI.Core;
using TrainAI.Core.Messages;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Services
{
    public class ScoreSystem : IScoreSystem
    {
        readonly PlayerStateRSO _state;
        readonly GameConfigSO _config;
        readonly List<ScoreRuleSO> _rules;

        public ScoreSystem(PlayerStateRSO state, GameConfigSO config, List<ScoreRuleSO> rules)
        {
            _state = state;
            _config = config;
            _rules = rules ?? new List<ScoreRuleSO>();
        }

        bool _expelFired;

        public void ApplyDelta(int hocTapDelta, int renLuyenDelta, string source)
        {
            int prevRen = _state.renLuyen;
            _state.hocTap = Mathf.Clamp(_state.hocTap + hocTapDelta, 0, _config != null ? _config.maxHocTap : 480);
            _state.renLuyen = Mathf.Clamp(_state.renLuyen + renLuyenDelta, 0, 100);

            BroadcastService.Send(new ScoreChangedMsg(_state.hocTap, _state.renLuyen));

            // Only fire Expel on the TRANSITION to zero. Without this guard,
            // every subsequent ApplyDelta after renLuyen hit 0 re-fired the
            // event each game-minute, spamming the UI and any subscribers.
            if (_state.renLuyen <= 0 && prevRen > 0 && !_expelFired)
            {
                _expelFired = true;
                BroadcastService.Send(new ExpelTriggeredMsg());
            }
            // Reset the latch if renLuyen recovers above zero, so a second
            // drop later in the run still triggers a fresh Expel.
            if (_state.renLuyen > 0) _expelFired = false;
        }

        public void OnQuizResult(int correctCount, int totalCount, SubjectSO subject)
        {
            if (totalCount <= 0) return;
            float pct = (float)correctCount / totalCount;
            int cap = subject != null ? subject.maxPointsPerLesson : 40;
            int gain = Mathf.RoundToInt(pct * cap);
            ApplyDelta(gain, 0, subject != null ? subject.id : "quiz");
        }
    }
}

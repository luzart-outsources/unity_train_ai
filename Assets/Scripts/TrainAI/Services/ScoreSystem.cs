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

        public void ApplyDelta(int hocTapDelta, int renLuyenDelta, string source)
        {
            _state.hocTap = Mathf.Clamp(_state.hocTap + hocTapDelta, 0, _config != null ? _config.maxHocTap : 480);
            _state.renLuyen = Mathf.Clamp(_state.renLuyen + renLuyenDelta, 0, 100);

            BroadcastService.Send(new ScoreChangedMsg(_state.hocTap, _state.renLuyen));

            if (_state.renLuyen <= 0)
                BroadcastService.Send(new ExpelTriggeredMsg());
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

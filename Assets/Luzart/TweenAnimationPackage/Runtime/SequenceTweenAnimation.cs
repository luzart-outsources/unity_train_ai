using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace Luzart
{
    public class SequenceTweenAnimation : TweenAnimationBase
    {
        [SerializeField] private List<TweenSequence> tweenSequences = new List<TweenSequence>();
        [SerializeField] private TweenSequenceSettings sequenceSettings;

        private Sequence _sequenceTweener;
        protected override void DoInitSetting(TweenAnimationSettings tweenAnimationSettings)
        {
            base.DoInitSetting(tweenAnimationSettings);
            for (int i = 0; i < tweenSequences.Count; i++)
            {
                var tS = tweenSequences[i];
                var tAB = tS.TweenAnimation;
                ITweenAnimation iTA = tAB;
                iTA.InitSetting(tweenAnimationSettings);

            }
        }
        protected override Tween DoShow()
        {
            _sequenceTweener = DOTween.Sequence();

            // Apply ignore time scale settings
            _sequenceTweener.SetUpdate(sequenceSettings.IsIgnoreTimeScale);

            // Apply timing settings
            if (sequenceSettings.Timing.DelayStart > 0)
                _sequenceTweener.AppendInterval(sequenceSettings.Timing.DelayStart);

            // Create the main sequence
            Sequence mainSequence = DOTween.Sequence();
            // Apply ignore time scale to main sequence as well
            mainSequence.SetUpdate(sequenceSettings.IsIgnoreTimeScale);

            for (int i = 0; i < tweenSequences.Count; i++)
            {
                var tweenSequence = tweenSequences[i];
                ITweenAnimation tweenAnimation = tweenSequence.TweenAnimation;
                var tween = tweenAnimation.Show();
                if (tween != null)
                {
                    if (tweenSequence.SequenceType == ESequenceType.Append)
                    {
                        mainSequence.Append(tween);
                    }
                    else if (tweenSequence.SequenceType == ESequenceType.Join)
                    {
                        mainSequence.Join(tween);
                    }
                    else if (tweenSequence.SequenceType == ESequenceType.Insert)
                    {
                        mainSequence.Insert(tweenSequence.InsertTime, tween);
                    }
                }
            }

            // Apply loop settings
            if (sequenceSettings.Loop.IsLoop)
            {
                Sequence loopSequence = DOTween.Sequence();
                // Apply ignore time scale to loop sequence
                loopSequence.SetUpdate(sequenceSettings.IsIgnoreTimeScale);

                if (sequenceSettings.Timing.TimeDelayPreLoop > 0)
                    loopSequence.AppendInterval(sequenceSettings.Timing.TimeDelayPreLoop);

                loopSequence.Append(mainSequence);

                if (sequenceSettings.Timing.TimeDelayAfterLoop > 0)
                    loopSequence.AppendInterval(sequenceSettings.Timing.TimeDelayAfterLoop);

                loopSequence.SetLoops(sequenceSettings.Loop.LoopCount, sequenceSettings.Loop.LoopType);
                _sequenceTweener.Append(loopSequence);
            }
            else
            {
                _sequenceTweener.Append(mainSequence);
            }

            return _sequenceTweener;
        }

        protected override void DoDispose()
        {
            _sequenceTweener?.Kill(true);
            _sequenceTweener = null;
        }

        private void OnValidate()
        {
            InitTweenSettings();
        }
        [ContextMenu("Init Tween Settings")]
        private void InitTweenSettings()
        {
            if (sequenceSettings == null)
            {
                sequenceSettings = new TweenSequenceSettings();
            }
            sequenceSettings.InitDuration(ComputeBaseDuration());
        }

        public override ITweenSettings GetTweenAnimationSettings()
        {
            TweenSequenceSettings sq = sequenceSettings.Clone();
            sq.InitDuration(ComputeBaseDuration());
            return sq;
        }

        private float ComputeBaseDuration()
        {
            float duration = 0;
            float lastAppendStart = 0;

            foreach (var entry in tweenSequences)
            {
                if (entry == null || entry.TweenAnimation == null)
                {
                    continue;
                }
                ITweenSettings tweenSettings = entry.TweenAnimation.GetTweenAnimationSettings();
                float dur = Mathf.Max(0f, tweenSettings.Duration);

                switch (entry.SequenceType)
                {
                    case ESequenceType.Append:
                        {
                            float start = duration;
                            lastAppendStart = start;
                            duration = start + dur;
                            break;
                        }

                    case ESequenceType.Join:
                        {
                            float end = lastAppendStart + dur;
                            duration = Mathf.Max(duration, end);
                            break;
                        }

                    case ESequenceType.Insert:
                        {
                            float start = Mathf.Max(0f, entry.InsertTime);
                            float end = start + dur;
                            if (end > duration)
                            {
                                duration = end;
                            }
                            lastAppendStart = start;
                            break;
                        }
                }
            }

            return duration;
        }

        [System.Serializable]
        class TweenSequence
        {
            public TweenAnimationBase TweenAnimation;
            public ESequenceType SequenceType;
            [ShowIf("SequenceType", ESequenceType.Insert)]
            public float InsertTime;
        }
        public enum ESequenceType
        {
            Append,
            Join,
            Insert
        }
    }

    // Minimal settings for sequence animations with IgnoreTimeScale option
    [System.Serializable]
    public class TweenSequenceSettings : ITweenSettings
    {
        [ReadOnly]
        [SerializeField]
        private float _duration = 0f;

        public bool IsIgnoreTimeScale = false;

        public TweenTimingSettings Timing;

        public TweenLoopSettings Loop;

        TweenTimingSettings ITweenSettings.Timing => Timing;
        TweenLoopSettings ITweenSettings.Loop => Loop;

        float ITweenSettings.Duration
        {
            get
            {
                if (Loop.IsLoop)
                {
                    if (Loop.LoopCount < 0)
                    {
                        return float.MaxValue;
                    }
                    return Timing.DelayStart + (_duration + Timing.TimeDelayPreLoop + Timing.TimeDelayAfterLoop) * Loop.LoopCount;
                }
                else
                {
                    return Timing.DelayStart + _duration;
                }
            }
        }

        bool ITweenSettings.IgnoreTimeScale => IsIgnoreTimeScale;

        public TweenSequenceSettings()
        {
            Timing = new TweenTimingSettings();
            Loop = new TweenLoopSettings();
        }
        public void InitDuration(float dur)
        {
            _duration = dur;
        }
        public TweenSequenceSettings Clone()
        {
            TweenSequenceSettings clone = new TweenSequenceSettings();
            clone._duration = this._duration;
            clone.IsIgnoreTimeScale = this.IsIgnoreTimeScale;
            clone.Timing = this.Timing;
            clone.Loop = this.Loop;
            return clone;
        }
    }
}

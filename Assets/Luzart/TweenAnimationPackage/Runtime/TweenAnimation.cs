using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Luzart
{
    public class TweenAnimation : TweenAnimationBase
    {
        [SerializeField] private EAnimation typeAnimation;
        [SerializeField] private TweenAnimationSettings tweenAnimationSettings = new TweenAnimationSettings();
        private ITweenAnimation _currentTweenAnimation;
        public bool IsAnimationVector3 => typeAnimation == EAnimation.Move ||
                                                typeAnimation == EAnimation.MoveLocal ||
                                                typeAnimation == EAnimation.MoveAnchors ||
                                                typeAnimation == EAnimation.Scale ||
                                                typeAnimation == EAnimation.Euler ||
                                                typeAnimation == EAnimation.SizeDelta ||
                                                typeAnimation == EAnimation.AnchorMin ||
                                                typeAnimation == EAnimation.AnchorMax;
        public bool IsAnimationFloat => typeAnimation == EAnimation.FadeByCanvasGroup ||
            typeAnimation == EAnimation.Float;

        /// <summary>
        /// True for animation types whose From/To values can be either user-provided (Override=true)
        /// or auto-pulled from the target's current value (Override=false).
        /// UnityEvent type does not use From/To at all and so does not support override.
        /// </summary>
        public bool SupportsValueOverride =>
            IsAnimationVector3
            || typeAnimation == EAnimation.FadeByCanvasGroup
            || typeAnimation == EAnimation.Float
            || typeAnimation == EAnimation.TextMeshProDOText;

        protected override Tween DoShow()
        {
            var tweenAnimation = GetTweenAnimation();
            if (tweenAnimation == null)
            {
                Debug.LogError("Tween Animation Type not found: " + typeAnimation.ToString());
                return null;
            }
            if (tweenAnimationSettings.General.Target == null)
            {
                tweenAnimationSettings.General.Target = this.gameObject;
            }

            tweenAnimation.InitSetting(tweenAnimationSettings);
            _currentTweenAnimation = tweenAnimation;
            return tweenAnimation.Show();
        }

        protected override void DoDispose()
        {
            _currentTweenAnimation?.Dispose();
            _currentTweenAnimation = null;
        }

        private ITweenAnimation GetTweenAnimation()
        {
            return typeAnimation switch
            {
                EAnimation.Move => new TweenAnimationMove(),
                EAnimation.MoveLocal => new TweenAnimationMoveLocal(),
                EAnimation.MoveAnchors => new TweenAnimationMoveAnchors(),
                EAnimation.Euler => new TweenAnimationEuler(),
                EAnimation.Scale => new TweenAnimationScale(),
                EAnimation.SizeDelta => new TweenAnimationSizeDelta(),
                EAnimation.AnchorMin => new TweenAnimationAnchorMin(),
                EAnimation.AnchorMax => new TweenAnimationAnchorMax(),
                EAnimation.FadeByCanvasGroup => new TweenAnimationFade(),
                EAnimation.TextMeshProDOText => new TweenAnimationTextMeshPro(),
                EAnimation.Float => new TweenAnimationFloat(),
                EAnimation.UnityEvent => new TweenAnimationUnityEvent(),
                _ => null
            };
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (tweenAnimationSettings == null || tweenAnimationSettings.General == null || tweenAnimationSettings.General.Target == null)
            {
                return;
            }
            // Defer structural changes: Unity forbids AddComponent/SendMessage inside OnValidate.
            EditorApplication.delayCall += DeferredEnsureCanvasGroup;
#endif
        }

#if UNITY_EDITOR
        private void DeferredEnsureCanvasGroup()
        {
            if (this == null)
            {
                return;
            }
            AddTweenAnimation();
        }
#endif

        private void Reset()
        {
            if (tweenAnimationSettings?.General != null)
            {
                tweenAnimationSettings.General.Target = gameObject;
            }
            AddTweenAnimation();
        }

        private void AddTweenAnimation()
        {
            if (typeAnimation != EAnimation.FadeByCanvasGroup)
            {
                return;
            }
            if (tweenAnimationSettings?.General == null || tweenAnimationSettings.General.Target == null)
            {
                return;
            }
            if (tweenAnimationSettings.General.Target is CanvasGroup)
            {
                return;
            }

            GameObject go = tweenAnimationSettings.General.Target as GameObject;
            if (go == null && tweenAnimationSettings.General.Target is Component comp)
            {
                go = comp.gameObject;
            }
            if (go == null)
            {
                Debug.LogError($"TweenAnimation: Target of {name} is not a GameObject or Component — cannot auto-add CanvasGroup.", this);
                return;
            }

            if (!go.TryGetComponent(out CanvasGroup canvas))
            {
                canvas = go.AddComponent<CanvasGroup>();
            }
            tweenAnimationSettings.General.Target = canvas;
        }

        public override ITweenSettings GetTweenAnimationSettings()
        {
            return tweenAnimationSettings;
        }
    }

    #region Data Structures

    // Interface for all settings types
    public interface ITweenSettings
    {
        public float Duration { get; }
        public bool IgnoreTimeScale { get; }
        TweenTimingSettings Timing { get; }
        TweenLoopSettings Loop { get; }
    }

    // Full settings for individual animations
    [System.Serializable]
    public class TweenAnimationSettings : ITweenSettings
    {
        public TweenGeneralSettings General;

        public TweenTimingSettings Timing;

        public TweenLoopSettings Loop;

        public TweenValueSettings Values;

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
                    return Timing.DelayStart + (General.Duration + Timing.TimeDelayPreLoop + Timing.TimeDelayAfterLoop) * Loop.LoopCount;
                }
                else
                {
                    return Timing.DelayStart + General.Duration;
                }
            }
        }

        public bool IgnoreTimeScale => General.IsIgnoreTimeScale;

        public TweenAnimationSettings()
        {
            General = new TweenGeneralSettings();
            Timing = new TweenTimingSettings();
            Loop = new TweenLoopSettings();
            Values = new TweenValueSettings();
        }
    }

    [System.Serializable]
    public class TweenGeneralSettings
    {
        public UnityEngine.Object Target;
        public float Duration = 1f;
        public Ease Easing = Ease.Linear;
        public bool IsIgnoreTimeScale = false;
    }

    [System.Serializable]
    public struct TweenTimingSettings
    {
        public float DelayStart;
        [ShowIf("../Loop.IsLoop", true)]
        public float TimeDelayPreLoop;
        [ShowIf("../Loop.IsLoop", true)]
        public float TimeDelayAfterLoop;
    }

    [System.Serializable]
    public struct TweenLoopSettings
    {
        public bool IsLoop;
        public LoopType LoopType;
        public int LoopCount;
    }

    [System.Serializable]
    public class TweenValueSettings
    {
        [Tooltip(
            "Eager: From is resolved and snapped immediately when Show() is called (before DelayStart). " +
            "Use this for standalone animations.\n\n" +
            "Lazy: resolution and snap are deferred until the tween actually starts playing " +
            "(inside the sequence callback, after DelayStart and after previous Sequence entries). " +
            "Use this for Sequence children that need 'From = my current position at my turn'.\n\n" +
            "Note: To value is ALWAYS resolved at Eager phase regardless of this setting, " +
            "because DOTween bakes the target value at tween-creation time.")]
        public EValueTiming Timing = EValueTiming.Eager;

        [Tooltip("If false, From is auto-pulled from the target's current value (resolved at the Timing phase). " +
                 "If true, use the explicit From value below.")]
        [ShowIf("../../SupportsValueOverride", true)]
        public bool OverrideFrom = false;

        [Tooltip("If false, To is auto-pulled from the target's current value (resolved at Eager phase). " +
                 "If true, use the explicit To value below.")]
        [ShowIf("../../SupportsValueOverride", true)]
        public bool OverrideTo = true;

        [ShowIfAll("../../IsAnimationVector3", true, "OverrideFrom", true)]
        public Vector3 Vector3From;

        [ShowIfAll("../../IsAnimationVector3", true, "OverrideTo", true)]
        public Vector3 Vector3To;

        [ShowIfAll("../../IsAnimationFloat", true, "OverrideFrom", true)]
        public float FloatFrom;

        [ShowIfAll("../../IsAnimationFloat", true, "OverrideTo", true)]
        public float FloatTo;

        [ShowIfAll("../../typeAnimation", EAnimation.TextMeshProDOText, "OverrideFrom", true)]
        public string StringFrom = "";

        [ShowIfAll("../../typeAnimation", EAnimation.TextMeshProDOText, "OverrideTo", true)]
        public string StringTo = "";

        [ShowIf("../../typeAnimation", EAnimation.Float)]
        public UnityEvent<float> OnFloatUnityEventInvoke;
        [ShowIf("../../typeAnimation", EAnimation.UnityEvent)]
        public UnityEvent OnUnityEventInvoke;

        // Helper methods to get type-specific values
        public Vector3 GetVector3From() => Vector3From;
        public Vector3 GetVector3To() => Vector3To;
        public float GetFloatFrom() => FloatFrom;
        public float GetFloatTo() => FloatTo;
        public string GetStringFrom() => StringFrom;
        public string GetStringTo() => StringTo;
    }

    /// <summary>
    /// When From value is resolved (if not overridden) and snapped onto the target.
    /// </summary>
    public enum EValueTiming
    {
        /// <summary>
        /// At the moment Show() is called (before DelayStart). Good for standalone animations.
        /// </summary>
        Eager = 0,

        /// <summary>
        /// When the tween actually starts playing (after DelayStart / after previous Sequence entries).
        /// Good for Sequence children that need "From = my position at my turn".
        /// </summary>
        Lazy = 1,
    }

    public enum ETypeShow
    {
        None = 0,
        Awake = 1,
        Start = 2,
        OnEnable = 3,
    }

    public enum EAnimation
    {
        Move = 0,
        MoveLocal = 1,
        MoveAnchors = 2,
        Float = 3,
        Euler = 4,
        Scale = 5,
        SizeDelta = 6,
        AnchorMin = 7,
        AnchorMax = 8,
        FadeByCanvasGroup = 9,
        TextMeshProDOText = 10,
        UnityEvent = 11,
    }

    #endregion

    #region Base Classes and Interface

    public interface ITweenAnimation : IDisposable
    {
        ITweenSettings Settings { get; }
        void InitSetting(TweenAnimationSettings settings);
        Tween Show();
    }

    #endregion
}

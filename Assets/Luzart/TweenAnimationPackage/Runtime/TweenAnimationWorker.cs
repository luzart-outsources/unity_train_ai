using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Luzart
{
    public abstract class TweenAnimationWorker : ITweenAnimation
    {
        protected Sequence _tweener;
        protected TweenAnimationSettings _settings;
        protected TweenAnimationSettings Settings => _settings;

        ITweenSettings ITweenAnimation.Settings => Settings;

        void ITweenAnimation.InitSetting(TweenAnimationSettings settings)
        {
            DoInitSetting(settings);
        }

        Tween ITweenAnimation.Show()
        {
            return DoShow();
        }

        void IDisposable.Dispose()
        {
            DoDispose();
        }

        protected virtual Tween DoShow()
        {
            return null;
        }

        protected virtual void DoInitSetting(TweenAnimationSettings settings)
        {
            this._settings = settings;
        }

        protected virtual void DoDispose()
        {
            _tweener?.Kill(true);
            _tweener = null;
        }

        protected Sequence CreateBaseTween()
        {
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(Settings.General.IsIgnoreTimeScale);
            if (Settings.Timing.DelayStart > 0)
                sequence.AppendInterval(Settings.Timing.DelayStart);
            return sequence;
        }

        protected void AppendTweenToSequence(Tween tween)
        {
            if (Settings.Loop.IsLoop)
            {
                Sequence loopTween = DOTween.Sequence();
                if (Settings.Timing.TimeDelayPreLoop > 0)
                    loopTween.AppendInterval(Settings.Timing.TimeDelayPreLoop);
                loopTween.Append(tween);
                if (Settings.Timing.TimeDelayAfterLoop > 0)
                    loopTween.AppendInterval(Settings.Timing.TimeDelayAfterLoop);
                loopTween.SetLoops(Settings.Loop.LoopCount, Settings.Loop.LoopType);
                _tweener.Append(loopTween);
            }
            else
            {
                _tweener.Append(tween);
            }
        }

        /// <summary>
        /// High-performance method to get target component with smart casting
        /// </summary>
        protected T GetTargetComponent<T>() where T : Component
        {
            var targetObject = Settings?.General?.Target;
            if (targetObject == null) return null;

            // Fast path: Direct cast if target is already the desired type
            if (targetObject is T directCast)
            {
                return directCast;
            }

            // Get GameObject reference efficiently
            GameObject gameObject = null;
            if (targetObject is GameObject go)
            {
                gameObject = go;
            }
            else if (targetObject is Component comp)
            {
                gameObject = comp.gameObject;
            }

            // Get component from GameObject
            return gameObject?.GetComponent<T>();
        }

        /// <summary>
        /// Get existing component or add it if not found (used for CanvasGroup)
        /// </summary>
        protected T GetOrAddTargetComponent<T>() where T : Component
        {
            var existingComponent = GetTargetComponent<T>();
            if (existingComponent != null) return existingComponent;

            // If component doesn't exist, try to add it
            var targetObject = Settings?.General?.Target;
            if (targetObject == null) return null;

            GameObject gameObject = null;
            if (targetObject is GameObject go)
            {
                gameObject = go;
            }
            else if (targetObject is Component comp)
            {
                gameObject = comp.gameObject;
            }
            if (gameObject == null)
            {
                Debug.LogError("GameObject in this is null");
                return null;
            }
            if (gameObject.TryGetComponent<T>(out T componentGet))
            {
                return componentGet;
            }
            else
            {
                var component = gameObject.AddComponent<T>();
                return component;
            }
        }
    }

    /// <summary>
    /// Generic base class for tween animations that reduces code duplication
    /// </summary>
    /// <typeparam name="T">The component type to animate</typeparam>
    public abstract class TweenAnimationWorker<T> : TweenAnimationWorker where T : Component
    {
        /// <summary>
        /// True when the current phase matches the configured Timing.
        /// Eager + isRuntime=false → true (init phase).
        /// Lazy + isRuntime=true → true (runtime callback phase).
        /// </summary>
        protected bool IsTimingPhase(bool isRuntime)
        {
            bool lazy = Settings.Values.Timing == EValueTiming.Lazy;
            return isRuntime == lazy;
        }

        private T _target;

        /// <summary>
        /// Cached target component with lazy initialization
        /// </summary>
        protected T Target
        {
            get
            {
                if (_target == null)
                {
                    _target = GetTargetComponent();
                }
                return _target;
            }
        }

        /// <summary>
        /// Get the target component. Override if you need GetOrAddTargetComponent behavior
        /// </summary>
        protected virtual T GetTargetComponent()
        {
            return base.GetTargetComponent<T>();
        }

        protected override void DoInitSetting(TweenAnimationSettings settings)
        {
            base.DoInitSetting(settings);
            SetDefaultValues(false);
            ApplyFromValue(false);
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            _target = null; // Clear cached target reference
        }

        protected override Tween DoShow()
        {
            if (Target == null)
            {
                Debug.LogWarning($"{GetType().Name}: Target {typeof(T).Name} is null");
                return null;
            }

            _tweener = CreateBaseTween();

            // Create the specific tween sequence
            Sequence tweenSequence = DOTween.Sequence();
            tweenSequence.AppendCallback(() =>
            {
                SetDefaultValues(true);
                ApplyFromValue(true);
            });

            // Add the specific animation tween
            var animationTween = CreateAnimationTween();
            if (animationTween != null)
            {
                tweenSequence.Append(animationTween);
            }

            AppendTweenToSequence(tweenSequence);
            _tweener.SetTarget(Target);
            return _tweener;
        }

        /// <summary>
        /// Create the specific animation tween (DOMove, DOScale, etc.)
        /// </summary>
        protected abstract Tween CreateAnimationTween();

        /// <summary>
        /// Set default values based on current component state
        /// </summary>
        protected abstract void SetDefaultValues(bool isRuntime);

        /// <summary>
        /// Apply the 'from' value to the component if needed
        /// </summary>
        protected abstract void ApplyFromValue(bool isRuntime);
    }

    /// <summary>
    /// Base class for Vector3-based animations (position, scale, rotation, etc.)
    /// </summary>
    /// <typeparam name="T">The component type to animate</typeparam>
    public abstract class Vector3TweenAnimationWorker<T> : TweenAnimationWorker<T> where T : Component
    {
        protected override void SetDefaultValues(bool isRuntime)
        {
            // From: resolved at the configured Timing phase
            if (!Settings.Values.OverrideFrom && IsTimingPhase(isRuntime))
            {
                Settings.Values.Vector3From = GetCurrentVector3Value();
            }
            // To: always resolved at init (isRuntime=false), because DOTween bakes targetValue
            // at tween-creation time in DoShow (which runs before the runtime callback fires).
            if (!Settings.Values.OverrideTo && !isRuntime)
            {
                Settings.Values.Vector3To = GetCurrentVector3Value();
            }
        }

        protected override void ApplyFromValue(bool isRuntime)
        {
            // No snap needed when From is not overridden — it equals the current value already.
            if (!Settings.Values.OverrideFrom) return;
            if (IsTimingPhase(isRuntime))
            {
                SetVector3Value(Settings.Values.GetVector3From());
            }
        }

        protected override Tween CreateAnimationTween()
        {
            return CreateVector3Tween(Settings.Values.GetVector3To(), Settings.General.Duration)
                .SetEase(Settings.General.Easing);
        }

        /// <summary>
        /// Get the current Vector3 value from the component (position, scale, etc.)
        /// </summary>
        protected abstract Vector3 GetCurrentVector3Value();

        /// <summary>
        /// Set the Vector3 value on the component
        /// </summary>
        protected abstract void SetVector3Value(Vector3 value);

        /// <summary>
        /// Create the specific Vector3 tween (DOMove, DOScale, etc.)
        /// </summary>
        protected abstract Tween CreateVector3Tween(Vector3 targetValue, float duration);
    }

    public static class TweenSettingsHelper
    {
        public static Sequence CreateBaseTween(ITweenSettings settings, bool ignoreTimeScale = false)
        {
            var sequence = DOTween.Sequence();
            sequence.SetUpdate(ignoreTimeScale);
            if (settings.Timing.DelayStart > 0)
                sequence.AppendInterval(settings.Timing.DelayStart);
            return sequence;
        }

        public static void AppendTweenToSequence(Sequence mainSequence, Tween tween, ITweenSettings settings)
        {
            if (settings.Loop.IsLoop)
            {
                Sequence loopTween = DOTween.Sequence();
                if (settings.Timing.TimeDelayPreLoop > 0)
                    loopTween.AppendInterval(settings.Timing.TimeDelayPreLoop);
                loopTween.Append(tween);
                if (settings.Timing.TimeDelayAfterLoop > 0)
                    loopTween.AppendInterval(settings.Timing.TimeDelayAfterLoop);
                loopTween.SetLoops(settings.Loop.LoopCount, settings.Loop.LoopType);
                mainSequence.Append(loopTween);
            }
            else
            {
                mainSequence.Append(tween);
            }
        }
    }

    #region Vector3 Based Animations

    public class TweenAnimationMove : Vector3TweenAnimationWorker<Transform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.position;
        protected override void SetVector3Value(Vector3 value) => Target.position = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOMove(targetValue, duration);
    }

    public class TweenAnimationMoveLocal : Vector3TweenAnimationWorker<Transform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.localPosition;
        protected override void SetVector3Value(Vector3 value) => Target.localPosition = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOLocalMove(targetValue, duration);
    }

    public class TweenAnimationMoveAnchors : Vector3TweenAnimationWorker<RectTransform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.anchoredPosition;
        protected override void SetVector3Value(Vector3 value) => Target.anchoredPosition = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOAnchorPos(targetValue, duration);
    }

    public class TweenAnimationScale : Vector3TweenAnimationWorker<Transform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.localScale;
        protected override void SetVector3Value(Vector3 value) => Target.localScale = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOScale(targetValue, duration);
    }

    public class TweenAnimationEuler : Vector3TweenAnimationWorker<Transform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.eulerAngles;
        protected override void SetVector3Value(Vector3 value) => Target.eulerAngles = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DORotate(targetValue, duration);
    }

    public class TweenAnimationSizeDelta : Vector3TweenAnimationWorker<RectTransform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.sizeDelta;
        protected override void SetVector3Value(Vector3 value) => Target.sizeDelta = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOSizeDelta(targetValue, duration);
    }

    public class TweenAnimationAnchorMin : Vector3TweenAnimationWorker<RectTransform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.anchorMin;
        protected override void SetVector3Value(Vector3 value) => Target.anchorMin = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOAnchorMin(targetValue, duration);
    }

    public class TweenAnimationAnchorMax : Vector3TweenAnimationWorker<RectTransform>
    {
        protected override Vector3 GetCurrentVector3Value() => Target.anchorMax;
        protected override void SetVector3Value(Vector3 value) => Target.anchorMax = value;
        protected override Tween CreateVector3Tween(Vector3 targetValue, float duration) =>
            Target.DOAnchorMax(targetValue, duration);
    }

    #endregion

    #region CanvasGroup Based Animations

    public class TweenAnimationFade : TweenAnimationWorker<CanvasGroup>
    {
        protected override CanvasGroup GetTargetComponent()
        {
            return GetOrAddTargetComponent<CanvasGroup>();
        }

        protected override void SetDefaultValues(bool isRuntime)
        {
            if (!Settings.Values.OverrideFrom && IsTimingPhase(isRuntime))
            {
                Settings.Values.FloatFrom = Target.alpha;
            }
            if (!Settings.Values.OverrideTo && !isRuntime)
            {
                Settings.Values.FloatTo = Target.alpha;
            }
        }

        protected override void ApplyFromValue(bool isRuntime)
        {
            if (!Settings.Values.OverrideFrom) return;
            if (IsTimingPhase(isRuntime))
            {
                Target.alpha = Settings.Values.GetFloatFrom();
            }
        }

        protected override Tween CreateAnimationTween()
        {
            return Target.DOFade(Settings.Values.GetFloatTo(), Settings.General.Duration)
                .SetEase(Settings.General.Easing);
        }
    }

    #endregion

    #region Text Based Animations

    public class TweenAnimationTextMeshPro : TweenAnimationWorker<TextMeshProUGUI>
    {
        protected override void SetDefaultValues(bool isRuntime)
        {
            if (!Settings.Values.OverrideFrom && IsTimingPhase(isRuntime))
            {
                Settings.Values.StringFrom = Target.text;
            }
            if (!Settings.Values.OverrideTo && !isRuntime)
            {
                Settings.Values.StringTo = Target.text;
            }
        }

        protected override void ApplyFromValue(bool isRuntime)
        {
            if (!Settings.Values.OverrideFrom) return;
            if (IsTimingPhase(isRuntime))
            {
                Target.text = Settings.Values.GetStringFrom();
            }
        }

        protected override Tween CreateAnimationTween()
        {
            return Target.DOText(Settings.Values.GetStringTo(), Settings.General.Duration)
                .SetEase(Settings.General.Easing);
        }
    }

    #endregion

    #region Float Based Animations

    public class TweenAnimationFloat : TweenAnimationWorker
    {
        protected override Tween DoShow()
        {
            _tweener = CreateBaseTween();

            var animationTween = DOVirtual.Float(
                Settings.Values.GetFloatFrom(),
                Settings.Values.GetFloatTo(),
                Settings.General.Duration,
                value => Settings.Values.OnFloatUnityEventInvoke?.Invoke(value))
                .SetEase(Settings.General.Easing);

            AppendTweenToSequence(animationTween);
            return _tweener;
        }
    }

    #endregion

    #region UnityEvent Based Animations

    public class TweenAnimationUnityEvent : TweenAnimationWorker
    {
        protected override Tween DoShow()
        {
            _tweener = CreateBaseTween();

            var animationTween = DOVirtual.DelayedCall(
                Settings.General.Duration,
                () => Settings.Values.OnUnityEventInvoke?.Invoke());

            AppendTweenToSequence(animationTween);
            return _tweener;
        }
    }

    #endregion
}

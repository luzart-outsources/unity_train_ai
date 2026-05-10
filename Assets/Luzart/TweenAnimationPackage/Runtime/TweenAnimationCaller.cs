using DG.Tweening;
using UnityEngine;

namespace Luzart
{
    /// <summary>
    /// Unified caller for all TweenAnimation types with auto-trigger functionality
    /// Use this instead of duplicating auto-trigger logic in TweenAnimationBase
    /// </summary>
    public class TweenAnimationCaller : MonoBehaviour
    {
        [Header("Animation Reference")]
        [SerializeField] private TweenAnimationBase tweenAnimation;

        [Header("Trigger Settings")]
        [SerializeField] private ETypeShow typeShow = ETypeShow.OnEnable;

        private Tween _currentTween;

        private void Awake()
        {
            if (typeShow == ETypeShow.Awake)
            {
                CallShow();
            }
        }

        private void Start()
        {
            if (typeShow == ETypeShow.Start)
            {
                CallShow();
            }
        }

        private void OnEnable()
        {
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }

            if (typeShow == ETypeShow.OnEnable)
            {
                CallShow();
            }
        }

        private void OnDisable()
        {
            if (_currentTween != null)
            {
                _currentTween.Kill();
                _currentTween = null;
            }
        }

        private void Reset()
        {
            if (tweenAnimation == null)
            {
                tweenAnimation = GetComponent<TweenAnimationBase>();
            }
        }

        /// <summary>
        /// Manually trigger the animation
        /// </summary>
        [ContextMenu("Call Show")]
        public void CallShow()
        {
            if (tweenAnimation == null)
            {
                Debug.LogWarning($"TweenAnimationCaller: TweenAnimation is null on {gameObject.name}", this);
                return;
            }

            try
            {
                // Stop current tween if running
                if (_currentTween != null && _currentTween.IsActive())
                {
                    _currentTween.Kill();
                }

                // Start new tween
                ITweenAnimation iTweenAnimation = tweenAnimation;
                _currentTween = iTweenAnimation.Show();

                if (_currentTween != null)
                {
                    // Optional: Add completion callback for cleanup
                    _currentTween.OnComplete(() => _currentTween = null);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"TweenAnimationCaller Error on {gameObject.name}: {e.Message}", this);
            }
        }

        /// <summary>
        /// Stop the current animation
        /// </summary>
        public void Stop()
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
                _currentTween = null;
            }
        }

        /// <summary>
        /// Check if animation is currently playing
        /// </summary>
        public bool IsPlaying()
        {
            return _currentTween != null && _currentTween.IsActive();
        }

        /// <summary>
        /// Set new animation target at runtime
        /// </summary>
        public void SetTweenAnimation(TweenAnimationBase newAnimation)
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
                _currentTween = null;
            }

            tweenAnimation = newAnimation;
        }
    }
}

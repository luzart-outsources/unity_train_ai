using DG.Tweening;
using System;
using UnityEngine;

namespace Luzart
{
    #region Base Classes and Interface
    // Implements ITweenAnimation and provides base functionality for tween animations
    public abstract class TweenAnimationBase : MonoBehaviour, ITweenAnimation
    {
        ITweenSettings ITweenAnimation.Settings => GetTweenAnimationSettings();
        public abstract ITweenSettings GetTweenAnimationSettings();
        Tween ITweenAnimation.Show()
        {
            return DoShow();
        }

        protected virtual Tween DoShow()
        {
            return null;
        }

        void ITweenAnimation.InitSetting(TweenAnimationSettings tweenAnimationSettings)
        {
            DoInitSetting(tweenAnimationSettings);
        }

        protected virtual void DoInitSetting(TweenAnimationSettings tweenAnimationSettings)
        {
        }

        void IDisposable.Dispose()
        {
            DoDispose();
        }

        protected virtual void DoDispose()
        {
        }

        private void OnDestroy()
        {
            ITweenAnimation tweenAnimation = this;
            tweenAnimation.Dispose();
        }

        // Public API for manual control
        public virtual Tween Show()
        {
            ITweenAnimation iTweenAnimation = this;
            return iTweenAnimation.Show();
        }

        public virtual void Stop()
        {
            DoDispose();
        }
    }

    #endregion
}

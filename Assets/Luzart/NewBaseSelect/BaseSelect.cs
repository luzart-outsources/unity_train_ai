using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Luzart.NewBase
{
    public interface IBaseSelect<T>
	{
		void Select(T value);
    }
    public interface IBaseToggle : IBaseSelect<bool> { }
    public interface IBaseSwitch : IBaseSelect<int> { }
    public interface ISelectBoolCache 
    {
        bool IsSelect {  get; }
        void SelectInvert();
    }

    public abstract class BaseSelect : MonoBehaviour, IBaseSwitch, IBaseToggle
    {
        void IBaseSelect<bool>.Select(bool value) => Select(value);
        void IBaseSelect<int>.Select(int value) => Select(value);
        [Button]
        public virtual void Select(bool value) {}
        [Button]
        public virtual void Select(int value) {}
    } 
    public abstract class BaseSelect<T> : BaseSelect, IBaseSelect<T>
    {
        void IBaseSelect<T>.Select(T value) => Select(value);
        public abstract void Select(T value);
    }
    public abstract class SelectToggle : BaseSelect<bool>, ISelectBoolCache
    {
        [SerializeField, ReadOnly]
        private bool _isSelect = false;
        bool ISelectBoolCache.IsSelect => _isSelect;

        void ISelectBoolCache.SelectInvert()
        {
            _isSelect = !_isSelect;
            Select(_isSelect);
        }
        public override void Select(bool value)
        {
            _isSelect = value;
        }
    }
    public abstract class SelectSwitch : BaseSelect<int> { }
}

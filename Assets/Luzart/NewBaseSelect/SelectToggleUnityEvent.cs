namespace Luzart.NewBase
{
    public class SelectToggleUnityEvent : SelectToggle
    {
        public UnityEngine.Events.UnityEvent onSelectTrue;
        public UnityEngine.Events.UnityEvent onSelectFalse;
        public override void Select(bool value)
        {
            base.Select(value);
            if (value)
            {
                onSelectTrue?.Invoke();
            }
            else
            {
                onSelectFalse?.Invoke();
            }
        }
    }
}

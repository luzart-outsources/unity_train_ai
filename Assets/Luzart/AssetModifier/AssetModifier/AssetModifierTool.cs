#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace AssetModifier
{
    /// <summary>
    /// Base class for every tool tab hosted inside <see cref="AssetModifierWindow"/>.
    /// </summary>
    public abstract class AssetModifierTool
    {
        protected AssetModifierWindow Window { get; private set; }

        /// <summary>Tab label shown in the top toolbar.</summary>
        public abstract string Label { get; }

        public bool IsActive { get; private set; }

        internal void BindWindow(AssetModifierWindow w) { Window = w; }

        internal void SetActive(bool v)
        {
            if (IsActive == v) return;
            IsActive = v;
            OnActivationChanged();
        }

        protected virtual void OnActivationChanged() { }

        /// <summary>Called once when the window is created (after BindWindow).</summary>
        public virtual void Setup() { }

        /// <summary>Called when the tool tab is drawn.</summary>
        public abstract void OnGUI();

        /// <summary>EditorPrefs key scoped to this window + tool.</summary>
        protected string EditorPrefKey(string name)
            => $"AssetModifier.{GetType().Name}.{name}";

        /// <summary>
        /// Draws a single selection entry. Tools can override to add per-entry controls.
        /// </summary>
        public virtual void OnSelectionDetailGUI(AssetModifierWindow.SelectionDetail sd)
        {
            EditorGUILayout.ObjectField(sd.obj, typeof(Object), false);
        }

        protected IEnumerable<Object> GetSelectedObjects()
        {
            foreach (var sd in Window.SelectionDetails)
                if (sd.obj) yield return sd.obj;
        }
    }
}
#endif

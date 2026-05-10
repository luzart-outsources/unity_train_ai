#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AssetModifier.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier.Tools
{
    /// <summary>
    /// Find string-typed SerializedProperties in the selection by a regex on propertyPath,
    /// then batch-modify (Set / Replace / Add before-after / Trim start-end) across
    /// the whole matched set with a staged manual-edit list before Apply.
    /// </summary>
    public class EditStringTool : AssetModifierTool
    {
        public override string Label => "Edit string";

        readonly List<SerializedObject> _serializedObjects = new List<SerializedObject>();
        readonly List<SerializedProperty> _serializedProperties = new List<SerializedProperty>();
        readonly Dictionary<SerializedProperty, string> _newValues = new Dictionary<SerializedProperty, string>();

        PrefString _pathRegex;
        PrefString _setTo, _replaceFrom, _replaceTo, _addText, _trimText;
        Vector2 _scroll;

        public override void Setup()
        {
            Cleanup();
            _pathRegex = new PrefString(EditorPrefKey(nameof(_pathRegex)));
            _setTo = new PrefString(EditorPrefKey(nameof(_setTo)));
            _replaceFrom = new PrefString(EditorPrefKey(nameof(_replaceFrom)));
            _replaceTo = new PrefString(EditorPrefKey(nameof(_replaceTo)));
            _addText = new PrefString(EditorPrefKey(nameof(_addText)));
            _trimText = new PrefString(EditorPrefKey(nameof(_trimText)));
        }

        protected override void OnActivationChanged()
        {
            if (!IsActive) Cleanup();
        }

        void Cleanup()
        {
            foreach (var so in _serializedObjects)
                if (so != null) so.Dispose();
            _serializedObjects.Clear();
            _serializedProperties.Clear();
            _newValues.Clear();
        }

        void LoadProperties()
        {
            Cleanup();
            Regex rx;
            try { rx = new Regex(_pathRegex.Value ?? ""); }
            catch (Exception e) { Debug.LogWarning($"Bad regex: {e.Message}"); return; }

            foreach (var obj in GetSelectedObjects().Distinct())
            {
                var so = new SerializedObject(obj);
                bool hasAny = false;
                var p = so.GetIterator();
                p.Next(true);
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.String && rx.IsMatch(p.propertyPath))
                    {
                        _serializedProperties.Add(p.Copy());
                        hasAny = true;
                    }
                }
                if (hasAny) _serializedObjects.Add(so);
                else so.Dispose();
            }
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
                GuiHelpers.GuideText("Modify serialized string properties across selected objects."));

            GuiHelpers.Separator();
            DrawToolSection();
            GuiHelpers.Separator();
            DrawPathSection();
            GuiHelpers.Separator();
            DrawModifySection();
        }

        void DrawPathSection()
        {
            GUILayout.Label("Properties", EditorStyles.boldLabel);
            _pathRegex.TrySetValue(EditorGUILayout.TextField("Path regex", _pathRegex.Value));
            if (GUILayout.Button("Reload properties")) LoadProperties();
        }

        void DrawToolSection()
        {
            var btn = GuiHelpers.MiniBtnStyle;
            const int btnW = 55;
            const int labelW = 60;

            GUILayout.Label("Tools", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Set to", GUILayout.Width(labelW));
                _setTo.TrySetValue(EditorGUILayout.TextField(_setTo.Value));
                if (GUILayout.Button("Set", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, _setTo.Value);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Replace", GUILayout.Width(labelW));
                _replaceFrom.TrySetValue(EditorGUILayout.TextField(_replaceFrom.Value));
                GUILayout.Label(GuiHelpers.ArrowSymbol.ToString(), GUILayout.ExpandWidth(false));
                _replaceTo.TrySetValue(EditorGUILayout.TextField(_replaceTo.Value));
                if (GUILayout.Button("Replace", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, GetMod(p).Replace(_replaceFrom.Value, _replaceTo.Value));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Add", GUILayout.Width(labelW));
                _addText.TrySetValue(EditorGUILayout.TextField(_addText.Value));
                if (GUILayout.Button("Before", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, _addText.Value + GetMod(p));
                if (GUILayout.Button("After", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, GetMod(p) + _addText.Value);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Trim", GUILayout.Width(labelW));
                _trimText.TrySetValue(EditorGUILayout.TextField(_trimText.Value));
                if (GUILayout.Button("Start", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, GetMod(p).TrimStart(_trimText.Value.ToCharArray()));
                if (GUILayout.Button("End", btn, GUILayout.Width(btnW)))
                    foreach (var p in _serializedProperties) SetMod(p, GetMod(p).TrimEnd(_trimText.Value.ToCharArray()));
            }
        }

        void DrawModifySection()
        {
            var btn = GuiHelpers.MiniBtnStyle;
            GUILayout.Label("Modify", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset all", btn)) _newValues.Clear();
                if (GUILayout.Button("Apply all", btn))
                    foreach (var p in _serializedProperties.ToList()) ApplyMod(p);
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.MinHeight(200), GUILayout.ExpandHeight(true)))
            {
                _scroll = sv.scrollPosition;
                bool shouldCleanup = false;
                for (int i = 0; i < _serializedProperties.Count; i++)
                {
                    var p = _serializedProperties[i];
                    Object obj;
                    try { obj = p.serializedObject.targetObject; }
                    catch { shouldCleanup = true; break; }

                    var newVal = GetMod(p);
                    var changed = newVal != p.stringValue;
                    using (new GuiHelpers.Colored(changed ? Color.cyan : Color.white))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GuiHelpers.MiniIconBtn("-")) { _serializedProperties.RemoveAt(i); _newValues.Remove(p); break; }
                        EditorGUILayout.ObjectField(obj, typeof(Object), false, GUILayout.Width(100));
                        EditorGUILayout.TextField(p.propertyPath, EditorStyles.wordWrappedLabel, GUILayout.Width(150));
                        var edited = EditorGUILayout.TextField(newVal);
                        if (edited != newVal) SetMod(p, edited);

                        using (new GuiHelpers.Disabled(!changed))
                        {
                            if (GuiHelpers.MiniIconBtn("x")) _newValues.Remove(p);
                            if (GuiHelpers.MiniIconBtn("✓")) ApplyMod(p);
                        }
                    }
                }
                if (shouldCleanup) Cleanup();
            }
        }

        string GetMod(SerializedProperty p) => _newValues.TryGetValue(p, out var v) ? v : p.stringValue;

        void SetMod(SerializedProperty p, string value)
        {
            if (value == p.stringValue) _newValues.Remove(p);
            else _newValues[p] = value;
        }

        void ApplyMod(SerializedProperty p)
        {
            if (!_newValues.TryGetValue(p, out var n)) return;
            if (p.stringValue == n) return;
            p.stringValue = n;
            p.serializedObject.ApplyModifiedProperties();
            _newValues.Remove(p);
        }
    }
}
#endif

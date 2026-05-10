#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using AssetModifier.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier.Tools
{
    /// <summary>
    /// Rename main-assets and sub-asset ScriptableObjects. Manual per-asset editing
    /// plus batch tools (Set / Replace / Add before/after / Trim / Beautify).
    /// </summary>
    public class RenameTool : AssetModifierTool
    {
        public override string Label => "Rename";

        readonly Dictionary<Object, string> _manualRenames = new Dictionary<Object, string>();

        PrefString _setTo, _replaceFrom, _replaceTo, _addText, _trimText;
        Vector2 _listScroll;

        public override void Setup()
        {
            _setTo = new PrefString(EditorPrefKey(nameof(_setTo)));
            _replaceFrom = new PrefString(EditorPrefKey(nameof(_replaceFrom)));
            _replaceTo = new PrefString(EditorPrefKey(nameof(_replaceTo)));
            _addText = new PrefString(EditorPrefKey(nameof(_addText)));
            _trimText = new PrefString(EditorPrefKey(nameof(_trimText)));
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
                GuiHelpers.GuideText("Rename selected assets (main or sub). Edits are staged locally then applied."));

            GuiHelpers.Separator();
            DrawToolSection();
            GuiHelpers.Separator();
            DrawListSection();
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
                    foreach (var o in GetSelectedObjects()) SetRename(o, _setTo.Value);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Replace", GUILayout.Width(labelW));
                _replaceFrom.TrySetValue(EditorGUILayout.TextField(_replaceFrom.Value));
                GUILayout.Label(GuiHelpers.ArrowSymbol.ToString(), GUILayout.ExpandWidth(false));
                _replaceTo.TrySetValue(EditorGUILayout.TextField(_replaceTo.Value));
                if (GUILayout.Button("Replace", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects())
                        SetRename(o, GetRename(o).Replace(_replaceFrom.Value, _replaceTo.Value));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Add", GUILayout.Width(labelW));
                _addText.TrySetValue(EditorGUILayout.TextField(_addText.Value));
                if (GUILayout.Button("Before", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects()) SetRename(o, _addText.Value + GetRename(o));
                if (GUILayout.Button("After", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects()) SetRename(o, GetRename(o) + _addText.Value);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Trim", GUILayout.Width(labelW));
                _trimText.TrySetValue(EditorGUILayout.TextField(_trimText.Value));
                if (GUILayout.Button("Start", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects()) SetRename(o, GetRename(o).TrimStart(_trimText.Value.ToCharArray()));
                if (GUILayout.Button("End", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects()) SetRename(o, GetRename(o).TrimEnd(_trimText.Value.ToCharArray()));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Beautify sub-asset\n" + GuiHelpers.Substring("|Name/Name"), new GUIStyle(EditorStyles.label) { richText = true });
                if (GUILayout.Button("Beautify", btn, GUILayout.Width(btnW)))
                    foreach (var o in GetSelectedObjects().Where(o => !AssetDatabase.IsMainAsset(o)))
                        SetRename(o, BeautifySubAssetName(GetRename(o)));
            }
        }

        void DrawListSection()
        {
            var btn = GuiHelpers.MiniBtnStyle;

            GUILayout.Label("Names", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset all", btn)) _manualRenames.Clear();
                if (GUILayout.Button("Apply all", btn))
                    foreach (var o in GetSelectedObjects()) ApplyRename(o);
            }

            using (var sv = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.MinHeight(200), GUILayout.ExpandHeight(true)))
            {
                _listScroll = sv.scrollPosition;
                foreach (var o in GetSelectedObjects())
                {
                    var changedName = GetRename(o);
                    var changed = changedName != o.name;
                    using (new GuiHelpers.Colored(changed ? Color.cyan : Color.white))
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(o, typeof(Object), false, GUILayout.MaxWidth(100));
                        var newVal = EditorGUILayout.TextField(changedName);
                        if (newVal != changedName) SetRename(o, newVal);

                        if (GuiHelpers.MiniIconBtn("...")) ShowContextMenu(o);
                        using (new GuiHelpers.Disabled(!changed))
                        {
                            if (GuiHelpers.MiniIconBtn("x")) _manualRenames.Remove(o);
                            if (GuiHelpers.MiniIconBtn("✓")) ApplyRename(o);
                        }
                    }
                }
            }
        }

        // --- state ---

        string GetRename(Object o) => _manualRenames.TryGetValue(o, out var v) ? v : o.name;

        void SetRename(Object o, string name)
        {
            if (name == o.name) _manualRenames.Remove(o);
            else _manualRenames[o] = name;
        }

        void ApplyRename(Object o)
        {
            if (!_manualRenames.TryGetValue(o, out var n)) return;
            if (o.name == n) return;
            var oldName = o.name;
            if (AssetDatabase.IsMainAsset(o))
            {
                o.name = n;
                AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(o), n);
            }
            else
            {
                o.name = n;
                EditorUtility.SetDirty(o);
                AssetDatabase.SaveAssetIfDirty(o);
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(o), ImportAssetOptions.ForceUpdate);
            }
            Debug.Log($"Renamed {AssetHelpers.ReadableId(o)}: {oldName} {GuiHelpers.ArrowSymbol} {n}");
            _manualRenames.Remove(o);
        }

        void ShowContextMenu(Object o)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Copy to others with same name"), false, () =>
            {
                foreach (var o2 in GetSelectedObjects())
                {
                    if (o2 != o && o2.name == o.name)
                    {
                        if (_manualRenames.ContainsKey(o)) _manualRenames[o2] = _manualRenames[o];
                        else _manualRenames.Remove(o2);
                    }
                }
            });
            menu.AddItem(new GUIContent("Copy to others with same type"), false, () =>
            {
                var type = o.GetType();
                var v = GetRename(o);
                foreach (var o2 in GetSelectedObjects())
                    if (o2 != o && o2.GetType() == type) _manualRenames[o2] = v;
            });
            menu.ShowAsContext();
        }

        static string BeautifySubAssetName(string name)
        {
            var split = new[] { '|', '/' };
            var parts = name.Split(split, System.StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                var head = p[0].ToString().ToUpper();
                if (p.Length > 1) head += p.Substring(1);
                parts[i] = head;
            }
            return "|" + string.Join("/", parts);
        }
    }
}
#endif

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
    /// Transform the current Unity selection via a staged choice list with
    /// Undo/Redo, filter-by-type, regex filter, parent/child/sibling navigation,
    /// and set operations (Apply / Add / Remove / Intersect / Difference).
    /// </summary>
    public class SelectTool : AssetModifierTool
    {
        public override string Label => "Select";

        readonly ObjectListState _list = new ObjectListState();

        PrefString _filterRegex;
        PrefInt _filterMode;

        enum FilterMode { ObjName = 0, ObjPath = 1, ClsName = 2, ClsFullName = 3 }

        public override void Setup()
        {
            _filterRegex = new PrefString(EditorPrefKey(nameof(_filterRegex)), "(?i)");
            _filterMode = new PrefInt(EditorPrefKey(nameof(_filterMode)), (int)FilterMode.ObjName);
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
            {
                GuiHelpers.GuideText("Modify the current selection. Press Begin, tweak choices, then Apply/Add/Remove...");
            });

            GuiHelpers.Separator();
            DrawMain();
            GuiHelpers.Separator();
            DrawTools();
            GuiHelpers.Separator();
            DrawChoices();
        }

        void DrawMain()
        {
            var rs = GuiHelpers.ReserveEqualRects(2, 20);
            if (GUI.Button(rs[0], "Begin", GuiHelpers.MiniBtnStyle))
                _list.Initialize(GetProjectSelection());
            if (GUI.Button(rs[1], "Reset", GuiHelpers.MiniBtnStyle))
                _list.ResetToInit();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Modify selection");
                using (new EditorGUILayout.VerticalScope())
                {
                    rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Apply", GuiHelpers.MiniBtnStyle)) Selection.objects = _list.Current.ToArray();
                    if (GUI.Button(rs[1], "Add", GuiHelpers.MiniBtnStyle)) Selection.objects = GetProjectSelection().Concat(_list.Current).Distinct().ToArray();

                    rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Remove", GuiHelpers.MiniBtnStyle)) Selection.objects = GetProjectSelection().Except(_list.Current).ToArray();
                    if (GUI.Button(rs[1], "Reverse remove", GuiHelpers.MiniBtnStyle)) Selection.objects = _list.Current.Except(GetProjectSelection()).ToArray();

                    rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Intersect", GuiHelpers.MiniBtnStyle)) Selection.objects = GetProjectSelection().Intersect(_list.Current).ToArray();
                    if (GUI.Button(rs[1], "Difference", GuiHelpers.MiniBtnStyle))
                    {
                        var sel = GetProjectSelection().ToArray();
                        var inter = sel.Intersect(_list.Current).ToArray();
                        Selection.objects = sel.Concat(_list.Current).Except(inter).ToArray();
                    }
                }
            }
        }

        // Unity's Two-Column Project browser keeps its highlight in Selection.assetGUIDs,
        // while Selection.objects can be empty/stale when focus is on the folder tree pane.
        // Union both to cover main assets (from assetGUIDs) AND sub-assets like Sprites
        // or AnimationClips inside an FBX (only present in Selection.objects).
        static IEnumerable<Object> GetProjectSelection()
        {
            var fromGuids = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .Where(o => o != null);

            var fromObjects = Selection.objects.Where(o => o != null && AssetDatabase.Contains(o));

            return fromObjects.Concat(fromGuids).Distinct();
        }

        void DrawTools()
        {
            GuiHelpers.SubSectionLabel("Choices tools");

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(" ");
                using (new EditorGUILayout.VerticalScope())
                {
                    var rs = GuiHelpers.ReserveEqualRects(2, 20);
                    using (new GuiHelpers.Disabled(!_list.CanUndo))
                        if (GUI.Button(rs[0], $"Undo ({_list.UndoCount})", GuiHelpers.MiniBtnStyle)) _list.Undo();
                    using (new GuiHelpers.Disabled(!_list.CanRedo))
                        if (GUI.Button(rs[1], $"Redo ({_list.RedoCount})", GuiHelpers.MiniBtnStyle)) _list.Redo();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Remove");
                using (new EditorGUILayout.VerticalScope())
                {
                    var rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Null", GuiHelpers.MiniBtnStyle)) _list.Remove(o => !o);
                    if (GUI.Button(rs[1], "Duplicates", GuiHelpers.MiniBtnStyle)) _list.RemoveDupes();
                    rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Main assets", GuiHelpers.MiniBtnStyle)) _list.Remove(AssetDatabase.IsMainAsset);
                    if (GUI.Button(rs[1], "Sub assets", GuiHelpers.MiniBtnStyle)) _list.Remove(o => !AssetDatabase.IsMainAsset(o));
                    rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Scriptables", GuiHelpers.MiniBtnStyle)) _list.Remove(o => o is ScriptableObject);
                    if (GUI.Button(rs[1], "Not Scriptables", GuiHelpers.MiniBtnStyle)) _list.Remove(o => !(o is ScriptableObject));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Go to");
                using (new EditorGUILayout.VerticalScope())
                {
                    var rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Parent", GuiHelpers.MiniBtnStyle)) _list.GoTo(GetParent);
                    if (GUI.Button(rs[1], "1st child", GuiHelpers.MiniBtnStyle)) _list.GoTo(GetFirstChild);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Transform");
                using (new EditorGUILayout.VerticalScope())
                {
                    var rs = GuiHelpers.ReserveEqualRects(1, 20);
                    if (GUI.Button(rs[0], "To main assets", GuiHelpers.MiniBtnStyle)) _list.Transform(AssetHelpers.GetMainAsset);
                    rs = GuiHelpers.ReserveEqualRects(1, 20);
                    if (GUI.Button(rs[0], "To sub assets", GuiHelpers.MiniBtnStyle))
                        _list.TransformMany(o => AssetHelpers.GetAllAssets(AssetHelpers.GetMainAsset(o)).Where(AssetHelpers.IsSubAsset));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Filter regex");
                using (new EditorGUILayout.VerticalScope())
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        var cur = (FilterMode)_filterMode.Value;
                        cur = (FilterMode)EditorGUILayout.EnumPopup(cur);
                        _filterMode.TrySetValue((int)cur);
                    }
                    _filterRegex.TrySetValue(EditorGUILayout.TextField(_filterRegex.Value));

                    var rs = GuiHelpers.ReserveEqualRects(2, 20);
                    if (GUI.Button(rs[0], "Remove", GuiHelpers.MiniBtnStyle)) FilterByRegex(false);
                    if (GUI.Button(rs[1], "Retain", GuiHelpers.MiniBtnStyle)) FilterByRegex(true);
                }
            }
        }

        void DrawChoices()
        {
            GuiHelpers.SubSectionLabel($"Choices ({_list.Current.Count}/{_list.OriginalCount})");
            foreach (var o in _list.Current)
                EditorGUILayout.ObjectField(o, typeof(Object), false);
        }

        void FilterByRegex(bool retainIfMatch)
        {
            var pattern = _filterRegex.Value ?? "";
            Regex rx;
            try { rx = new Regex(pattern); }
            catch (Exception e) { Debug.LogWarning($"Bad regex: {e.Message}"); return; }
            _list.Remove(o =>
            {
                var src = GetFilterString(o);
                var match = src != null && rx.IsMatch(src);
                return retainIfMatch ? !match : match;
            });
        }

        string GetFilterString(Object o)
        {
            if (!o) return null;
            return (FilterMode)_filterMode.Value switch
            {
                FilterMode.ObjName => o.name,
                FilterMode.ObjPath => AssetDatabase.GetAssetPath(o),
                FilterMode.ClsName => o.GetType().Name,
                FilterMode.ClsFullName => o.GetType().FullName,
                _ => ""
            };
        }

        static Object GetParent(Object o)
        {
            if (!o) return null;
            if (AssetHelpers.IsSubAsset(o)) return AssetHelpers.GetMainAsset(o);
            var path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) return null;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            return string.IsNullOrEmpty(parent) ? null : AssetDatabase.LoadAssetAtPath<Object>(parent);
        }

        static Object GetFirstChild(Object o)
        {
            if (!o) return null;
            if (o is ScriptableObject)
            {
                var all = AssetHelpers.GetAllAssets(o);
                foreach (var a in all) if (a != o && AssetHelpers.IsSubAsset(a)) return a;
                return null;
            }
            var path = AssetDatabase.GetAssetPath(o);
            if (!AssetDatabase.IsValidFolder(path)) return null;
            var children = AssetDatabase.FindAssets("", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.IO.Path.GetDirectoryName(p)?.Replace('\\', '/') == path)
                .Select(AssetDatabase.LoadMainAssetAtPath)
                .FirstOrDefault();
            return children;
        }

        // -------- inner state object --------

        class ObjectListState
        {
            readonly List<List<Object>> _undo = new List<List<Object>>();
            readonly List<List<Object>> _redo = new List<List<Object>>();
            List<Object> _original = new List<Object>();
            List<Object> _current = new List<Object>();

            public List<Object> Current => _current;
            public int OriginalCount => _original.Count;
            public int UndoCount => _undo.Count;
            public int RedoCount => _redo.Count;
            public bool CanUndo => _undo.Count > 0;
            public bool CanRedo => _redo.Count > 0;

            public void Initialize(IEnumerable<Object> src)
            {
                _undo.Clear(); _redo.Clear();
                _original = src.Where(o => o).ToList();
                _current = new List<Object>(_original);
            }

            public void ResetToInit()
            {
                Push();
                _current = new List<Object>(_original);
            }

            public void Remove(Func<Object, bool> predicate)
            {
                Push();
                _current.RemoveAll(o => predicate(o));
            }

            public void RemoveDupes()
            {
                Push();
                _current = _current.Distinct().ToList();
            }

            public void Transform(Func<Object, Object> map)
            {
                Push();
                _current = _current.Select(map).Where(o => o).Distinct().ToList();
            }

            public void TransformMany(Func<Object, IEnumerable<Object>> map)
            {
                Push();
                _current = _current.SelectMany(map).Where(o => o).Distinct().ToList();
            }

            public void GoTo(Func<Object, Object> nav) => Transform(nav);

            public void Undo()
            {
                if (_undo.Count == 0) return;
                _redo.Add(_current);
                _current = _undo[_undo.Count - 1];
                _undo.RemoveAt(_undo.Count - 1);
            }

            public void Redo()
            {
                if (_redo.Count == 0) return;
                _undo.Add(_current);
                _current = _redo[_redo.Count - 1];
                _redo.RemoveAt(_redo.Count - 1);
            }

            void Push()
            {
                _undo.Add(new List<Object>(_current));
                _redo.Clear();
                if (_undo.Count > 50) _undo.RemoveAt(0);
            }
        }
    }
}
#endif

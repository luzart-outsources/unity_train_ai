#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using AssetModifier.Internal;
using AssetModifier.Tools;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier
{
    /// <summary>
    /// Editor window with tabbed tools for creating, renaming, deleting, copying,
    /// selecting, and batch-editing ScriptableObject assets and sub-assets.
    ///
    /// Drop-in standalone tool. Pure Unity APIs only (AssetDatabase, Selection,
    /// SerializedObject, EditorPrefs, TypeCache, GlobalObjectId, ReorderableList).
    /// </summary>
    public class AssetModifierWindow : EditorWindow
    {
        [MenuItem("Window/Asset Modifier %&#j")] // Ctrl+Alt+Shift+J
        public static void Open()
        {
            var w = GetWindow<AssetModifierWindow>("Asset Modifier", true);
            w.Show();
        }

        // ---------- Selection viewer ----------

        public struct SelectionDetail
        {
            public Object obj;
            public string fullPath;
            public string childPrefix;

            public SelectionDetail(Object o)
            {
                obj = o;
                fullPath = o ? AssetDatabase.GetAssetPath(o) : "";
                childPrefix = "";
                if (o is ScriptableObject so)
                {
                    childPrefix = "|";
                    if (AssetHelpers.IsSubAsset(so)) childPrefix = so.name + "/";
                }
            }
        }

        public List<SelectionDetail> SelectionDetails { get; } = new List<SelectionDetail>();
        public SelectionDetail? MainSelection
        {
            get
            {
                if (Selection.activeObject == null) return null;
                foreach (var sd in SelectionDetails)
                    if (sd.obj == Selection.activeObject) return sd;
                return null;
            }
        }

        // ---------- Tools ----------

        readonly List<AssetModifierTool> _tools = new List<AssetModifierTool>();
        int _activeToolIndex;
        Vector2 _selectionScroll;
        Vector2 _toolScroll;
        bool _selectionFoldout = true;

        void OnEnable()
        {
            RegisterTools();
            foreach (var t in _tools)
            {
                t.BindWindow(this);
                t.Setup();
            }
            SetActiveTool(_activeToolIndex);
            Selection.selectionChanged += OnSelectionChanged;
            RefreshSelection();
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        void RegisterTools()
        {
            _tools.Clear();
            _tools.Add(new CreateTool());
            _tools.Add(new RenameTool());
            _tools.Add(new DeleteTool());
            _tools.Add(new CopyTool());
            _tools.Add(new SelectTool());
            _tools.Add(new EditStringTool());
            _tools.Add(new ModifyTool());
            _tools.Add(new SaveSelectTool());
        }

        void OnSelectionChanged()
        {
            RefreshSelection();
            Repaint();
        }

        void RefreshSelection()
        {
            SelectionDetails.Clear();
            foreach (var o in Selection.objects)
            {
                if (o == null) continue;
                if (!AssetDatabase.Contains(o)) continue;
                SelectionDetails.Add(new SelectionDetail(o));
            }
        }

        void SetActiveTool(int idx)
        {
            idx = Mathf.Clamp(idx, 0, _tools.Count - 1);
            for (int i = 0; i < _tools.Count; i++) _tools[i].SetActive(i == idx);
            _activeToolIndex = idx;
        }

        // ---------- Draw ----------

        void OnGUI()
        {
            DrawHeader();
            GuiHelpers.Separator();
            DrawSelectionSection();
            GuiHelpers.Separator();
            DrawToolsTabBar();
            GuiHelpers.Separator();
            DrawActiveTool();
        }

        void DrawHeader()
        {
            GUILayout.Label("Asset Modifier", EditorStyles.boldLabel);
            GUILayout.Label("(Ctrl + Alt + Shift + J)", EditorStyles.centeredGreyMiniLabel);
        }

        void DrawSelectionSection()
        {
            _selectionFoldout = EditorGUILayout.Foldout(_selectionFoldout, $"Selection ({SelectionDetails.Count})", true);
            if (!_selectionFoldout) return;

            if (SelectionDetails.Count == 0)
            {
                EditorGUILayout.HelpBox("Select one or more assets in the Project window.", MessageType.Info);
                return;
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(_selectionScroll, GUILayout.MaxHeight(160)))
            {
                _selectionScroll = scroll.scrollPosition;
                var tool = _tools.Count > 0 ? _tools[_activeToolIndex] : null;
                foreach (var sd in SelectionDetails)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (tool != null) tool.OnSelectionDetailGUI(sd);
                        else EditorGUILayout.ObjectField(sd.obj, typeof(Object), false);
                    }
                }
            }
        }

        void DrawToolsTabBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                for (int i = 0; i < _tools.Count; i++)
                {
                    var on = i == _activeToolIndex;
                    var style = on ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                    if (GUILayout.Toggle(on, _tools[i].Label, style) && !on)
                    {
                        SetActiveTool(i);
                    }
                }
            }
        }

        void DrawActiveTool()
        {
            if (_tools.Count == 0) return;
            var tool = _tools[_activeToolIndex];
            using (var scroll = new EditorGUILayout.ScrollViewScope(_toolScroll))
            {
                _toolScroll = scroll.scrollPosition;
                try { tool.OnGUI(); }
                catch (Exception e) { EditorGUILayout.HelpBox(e.ToString(), MessageType.Error); }
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using AssetModifier.Internal;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier.Tools
{
    /// <summary>
    /// Save / load named selection sets persisted in EditorPrefs via GlobalObjectId.
    /// </summary>
    public class SaveSelectTool : AssetModifierTool
    {
        public override string Label => "Saved Selects";

        const string NameSeparator = ";#@$";

        PrefString _saveNameText;
        PrefString _savedNames;
        ReorderableList _list;

        List<string> _names;
        bool _namesDirty;

        public override void Setup()
        {
            _saveNameText = new PrefString(EditorPrefKey(nameof(_saveNameText)));
            _savedNames = new PrefString(EditorPrefKey(nameof(_savedNames)));

            _names = LoadNames();
            _list = new ReorderableList(_names, typeof(string), true, false, false, false)
            {
                elementHeight = 35,
                drawElementCallback = DrawListElement,
                drawElementBackgroundCallback = DrawListBackground,
                onReorderCallback = _ => _namesDirty = true,
            };
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
                GuiHelpers.GuideText("Save and load named selection sets."));

            GuiHelpers.Separator();
            DrawSaveSection();
            GuiHelpers.Separator();
            DrawLoadSection();
        }

        void DrawSaveSection()
        {
            GuiHelpers.SubSectionLabel("Save");
            _saveNameText.TrySetValue(EditorGUILayout.TextField("Save name", _saveNameText.Value));

            var sel = GetSelectedObjects().ToList();
            EditorGUILayout.LabelField("Selections", sel.Count.ToString());

            var finalName = (_saveNameText.Value ?? "").Replace(NameSeparator, "");
            finalName = $"{finalName} (#{sel.Count})";
            EditorGUILayout.LabelField("Final name", finalName);

            bool saveable = !string.IsNullOrWhiteSpace(_saveNameText.Value) && sel.Count > 0;
            using (new GuiHelpers.Disabled(!saveable))
                if (GUILayout.Button("Save")) Save(sel, finalName);
        }

        void DrawLoadSection()
        {
            GuiHelpers.SubSectionLabel("Load");
            _names = LoadNames();
            _namesDirty = false;
            _list.list = _names;
            _list.DoLayoutList();
            if (_namesDirty) SaveNames(_names);
        }

        void DrawListBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index % 2 == 0) EditorGUI.DrawRect(rect, new Color(1, 1, 1, 0.08f));
            if (isFocused) EditorGUI.DrawRect(rect, new Color(0, 1, 1, 0.2f));
        }

        void DrawListElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || index >= _names.Count) return;
            var n = _names[index];
            rect.xMin += 2; rect.xMax -= 2;

            var xRect = new Rect(rect) { xMin = rect.xMax - 23 };
            var applyRect = new Rect(rect) { xMin = xRect.xMin - 50, xMax = xRect.xMin - 2 };
            var labelRect = new Rect(rect) { xMax = applyRect.xMin - 2 };

            GUI.Label(labelRect, n, EditorStyles.label);
            if (GUI.Button(applyRect, "Apply", GuiHelpers.MiniBtnStyle)) Load(n);
            if (GUI.Button(xRect, "x", GuiHelpers.MiniBtnStyle))
            {
                if (EditorUtility.DisplayDialog("Selection", $"Delete saved selection '{n}'?", "Yes", "No"))
                {
                    _names.RemoveAt(index);
                    _namesDirty = true;
                }
            }
        }

        void Save(List<Object> objs, string name)
        {
            var names = LoadNames();
            if (names.Contains(name)
                && !EditorUtility.DisplayDialog("Save selection", $"'{name}' already exists. Overwrite?", "Yes", "No"))
                return;
            if (!names.Contains(name)) names.Add(name);
            SaveNames(names);

            var payload = string.Join("#", objs.Select(o => GlobalObjectId.GetGlobalObjectIdSlow(o).ToString()));
            EditorPrefs.SetString(EditorPrefKey("saved_" + name), payload);
        }

        void Load(string name)
        {
            var s = EditorPrefs.GetString(EditorPrefKey("saved_" + name), "");
            var objs = new List<Object>();
            foreach (var tok in s.Split('#'))
            {
                if (GlobalObjectId.TryParse(tok, out var gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    if (obj) objs.Add(obj);
                }
            }
            Selection.objects = objs.ToArray();
        }

        List<string> LoadNames()
            => (_savedNames.Value ?? "")
                .Split(new[] { NameSeparator }, System.StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        void SaveNames(List<string> list)
            => _savedNames.TrySetValue(string.Join(NameSeparator, list));
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using AssetModifier.Internal;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier.Tools
{
    /// <summary>
    /// Create ScriptableObjects inside selected folders, or as sub-assets of selected ScriptableObjects.
    /// Type browser with filter/paging, name generation (auto / smart / custom / prefixed).
    /// </summary>
    public class CreateTool : AssetModifierTool
    {
        public override string Label => "Create";

        List<Type> _allTypes;
        readonly List<Type> _visibleTypes = new List<Type>();

        PrefBool _settingPaging;
        PrefBool _settingShowBaseClass;

        PrefBool _nameUsePrefix;
        PrefBool _nameAuto;
        PrefBool _nameAutoSmart;
        PrefString _nameCustom;

        PrefBool _typeIncludeNamespace;
        PrefString _typeFilterText;
        PrefInt _typePage;

        bool _tempIncludeNs;

        public override void Setup()
        {
            // Enumerate all creatable ScriptableObject types via TypeCache (pure Unity).
            _allTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
                .Where(t => !typeof(EditorWindow).IsAssignableFrom(t))
                .Where(t => !typeof(Editor).IsAssignableFrom(t))
                .Where(t => !t.IsAbstract)
                .Where(t => !t.IsGenericType && !t.ContainsGenericParameters)
                .Where(t => t.IsPublic || t.IsNestedPublic)
                .ToList();

            _settingPaging = new PrefBool(EditorPrefKey(nameof(_settingPaging)), false);
            _settingShowBaseClass = new PrefBool(EditorPrefKey(nameof(_settingShowBaseClass)), true);

            _nameUsePrefix = new PrefBool(EditorPrefKey(nameof(_nameUsePrefix)), true);
            _nameAuto = new PrefBool(EditorPrefKey(nameof(_nameAuto)), true);
            _nameAutoSmart = new PrefBool(EditorPrefKey(nameof(_nameAutoSmart)), true);
            _nameCustom = new PrefString(EditorPrefKey(nameof(_nameCustom)));

            _typeFilterText = new PrefString(EditorPrefKey(nameof(_typeFilterText)));
            _typeFilterText.OnValueChanged = Refresh;
            _typeIncludeNamespace = new PrefBool(EditorPrefKey(nameof(_typeIncludeNamespace)), true);
            _typeIncludeNamespace.OnValueChanged = Refresh;
            _typePage = new PrefInt(EditorPrefKey(nameof(_typePage)));

            Refresh();
        }

        public override void OnSelectionDetailGUI(AssetModifierWindow.SelectionDetail sd)
        {
            base.OnSelectionDetailGUI(sd);
            if (_nameUsePrefix.Value && !string.IsNullOrEmpty(sd.childPrefix))
            {
                GUILayout.Label($"{GuiHelpers.Substring("prefix:")} {sd.childPrefix}",
                    new GUIStyle(EditorStyles.miniLabel) { richText = true }, GUILayout.Width(150));
            }
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
            {
                GuiHelpers.GuideText("Creates ScriptableObjects in selected folders or as children of selected ScriptableObjects.");
                GuiHelpers.GuideText("To use: (1) select target folder/SO, (2) tweak name generation, (3) click a type button.");
            });

            GuiHelpers.Separator();
            DrawSettingsSection();
            GuiHelpers.Separator();
            DrawNameSection();
            GuiHelpers.Separator();
            DrawTypesSection();
        }

        void Refresh()
        {
            _tempIncludeNs = _typeIncludeNamespace.Value;
            var filter = (_typeFilterText.Value ?? "").ToLower();
            _visibleTypes.Clear();
            foreach (var t in _allTypes.OrderBy(TypeFilterName))
            {
                if (!string.IsNullOrWhiteSpace(filter) && !TypeFilterName(t).Contains(filter)) continue;
                _visibleTypes.Add(t);
            }
        }

        string TypeFilterName(Type t) => (_tempIncludeNs ? t.FullName : t.Name).ToLower();

        void DrawSettingsSection()
        {
            GuiHelpers.SubSectionLabel("Display settings");
            _settingPaging.TrySetValue(EditorGUILayout.ToggleLeft("Show as paging", _settingPaging.Value));
            _settingShowBaseClass.TrySetValue(EditorGUILayout.ToggleLeft("Show base class", _settingShowBaseClass.Value));
        }

        void DrawNameSection()
        {
            GuiHelpers.SubSectionLabel("Name generation");
            _nameUsePrefix.TrySetValue(EditorGUILayout.ToggleLeft("Use prefix (sub-asset)", _nameUsePrefix.Value));
            _nameAuto.TrySetValue(EditorGUILayout.ToggleLeft("Auto name", _nameAuto.Value));
            using (new GuiHelpers.Disabled(!_nameAuto.Value))
            using (new GuiHelpers.Indent(1))
            {
                _nameAutoSmart.TrySetValue(EditorGUILayout.ToggleLeft("Smart auto (trim _prefix / CSO suffix)", _nameAutoSmart.Value));
            }
            using (new GuiHelpers.Disabled(_nameAuto.Value))
            {
                _nameCustom.TrySetValue(EditorGUILayout.TextField("Custom name", _nameCustom.Value));
            }
        }

        void DrawTypesSection()
        {
            GuiHelpers.SubSectionLabel("Type select");

            _typeIncludeNamespace.TrySetValue(EditorGUILayout.ToggleLeft("Include namespace", _typeIncludeNamespace.Value));
            _typeFilterText.TrySetValue(EditorGUILayout.TextField("Type filter", _typeFilterText.Value));

            var btnStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, richText = true, wordWrap = true };
            bool includeNs = _typeIncludeNamespace.Value;
            bool showBase = _settingShowBaseClass.Value;

            if (_settingPaging.Value)
            {
                int page = _typePage.Value;
                page = GuiHelpers.Paginated(_visibleTypes, page, 20, (t, _) => DrawTypeButton(t, btnStyle, includeNs, showBase));
                _typePage.TrySetValue(page);
            }
            else
            {
                foreach (var t in _visibleTypes) DrawTypeButton(t, btnStyle, includeNs, showBase);
            }
        }

        void DrawTypeButton(Type t, GUIStyle btnStyle, bool includeNs, bool showBase)
        {
            string label = t.Name;
            if (includeNs && !string.IsNullOrWhiteSpace(t.Namespace))
                label = GuiHelpers.Substring(t.Namespace + ".") + label;
            if (showBase && t.BaseType != null)
                label += " " + GuiHelpers.Substring(": " + t.BaseType.Name);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(label, btnStyle))
                {
                    CreateInAllSelections(t);
                }
                GUILayout.Label(GuiHelpers.ArrowSymbol + GenerateName(t), EditorStyles.wordWrappedMiniLabel, GUILayout.Width(150));
            }
        }

        string GenerateName(Type type)
        {
            if (_nameAuto.Value)
                return _nameAutoSmart.Value ? SmartName(type) : type.Name;
            var custom = _nameCustom.Value;
            return string.IsNullOrWhiteSpace(custom) ? type.Name : custom;
        }

        static string SmartName(Type type)
        {
            var n = type.Name;
            if (n.Contains("_")) n = n.Substring(n.LastIndexOf('_') + 1);
            if (n.EndsWith("CSO")) n = n.Substring(0, n.Length - 3);
            return n;
        }

        void CreateInAllSelections(Type t)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var sd in Window.SelectionDetails)
                    CreateAt(t, sd);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            foreach (var sd in Window.SelectionDetails)
            {
                var p = AssetDatabase.GetAssetPath(sd.obj);
                if (!string.IsNullOrEmpty(p)) AssetDatabase.ImportAsset(p);
            }
        }

        void CreateAt(Type t, AssetModifierWindow.SelectionDetail sd)
        {
            var usePrefix = _nameUsePrefix.Value;
            var name = GenerateName(t);
            if (usePrefix) name = sd.childPrefix + name;

            var so = ScriptableObject.CreateInstance(t);

            if (AssetDatabase.IsValidFolder(sd.fullPath))
            {
                var path = System.IO.Path.Combine(sd.fullPath, name + ".asset").Replace('\\', '/');
                path = AssetDatabase.GenerateUniqueAssetPath(path);
                AssetDatabase.CreateAsset(so, path);
            }
            else if (sd.obj is ScriptableObject parentSO)
            {
                if (!name.StartsWith("|")) name = "|" + name;
                so.name = name;
                AssetDatabase.AddObjectToAsset(so, parentSO);
            }
            else
            {
                Object.DestroyImmediate(so);
                Debug.LogError($"CreateTool: target must be a folder or a ScriptableObject. Got: {sd.obj}");
            }
        }
    }
}
#endif

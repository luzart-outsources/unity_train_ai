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
    /// Copy structure / sub-asset references / serialized values from a source ScriptableObject
    /// to one or more destinations. Matching between source and dest sub-assets is done by name.
    /// </summary>
    public class CopyTool : AssetModifierTool
    {
        public override string Label => "Copy";

        PrefObject _sourceObject;

        Object _tempSource;
        bool _actionable;

        public override void Setup()
        {
            _sourceObject = new PrefObject(EditorPrefKey(nameof(_sourceObject)));
        }

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
            {
                GuiHelpers.GuideText("Copy structure, references, and serialized values from Source to selected Destinations.");
                GuiHelpers.GuideText("Sub-asset match is by name (SourceObj.|Foo " + GuiHelpers.ArrowSymbol + " DestObj.|Foo).");
                GuiHelpers.GuideText("Steps: (1) pick Source, (2) select Destinations, (3) press a Copy button.");
            });

            _tempSource = null; _actionable = false;

            GuiHelpers.Separator();
            DrawSourceSection();
            GuiHelpers.Separator();
            DrawDestinationsSection();
            GuiHelpers.Separator();
            DrawActionSection();
        }

        void DrawSourceSection()
        {
            GUILayout.Label("Source", EditorStyles.boldLabel);
            _sourceObject.TrySetValue(EditorGUILayout.ObjectField("Source", _sourceObject.Value, typeof(Object), false));

            var main = Window.MainSelection?.obj;
            if (main)
            {
                if (GUILayout.Button($"Choose main selection\n{GuiHelpers.Substring(main.name)}",
                    new GUIStyle(GUI.skin.button) { richText = true }))
                    _sourceObject.TrySetValue(main);
            }

            _tempSource = _sourceObject.Value;
            using (new GuiHelpers.Colored(Color.red))
            {
                if (!_tempSource) GUILayout.Label("No source object");
                else if (!AssetDatabase.Contains(_tempSource)) GUILayout.Label("Source must be an Asset");
                else if (!(_tempSource is ScriptableObject)) GUILayout.Label("Source must be a ScriptableObject");
                else _actionable = true;
            }
        }

        IEnumerable<Object> GetDestinations()
        {
            foreach (var sd in Window.SelectionDetails)
            {
                if (sd.obj == _tempSource) continue;
                if (sd.obj is ScriptableObject) yield return sd.obj;
            }
        }

        void DrawDestinationsSection()
        {
            GUILayout.Label("Destinations", EditorStyles.boldLabel);
            var list = GetDestinations().ToList();
            int total = list.Count;
            int mains = list.Count(AssetDatabase.IsMainAsset);
            int subs = total - mains;

            using (new GuiHelpers.Indent(1))
            {
                EditorGUILayout.LabelField("Total", total.ToString());
                EditorGUILayout.LabelField("Mains", mains.ToString());
                EditorGUILayout.LabelField("Subs", subs.ToString());
            }

            if (_tempSource && AssetDatabase.Contains(_tempSource))
            {
                if (AssetDatabase.IsMainAsset(_tempSource) && subs > 0)
                    EditorGUILayout.HelpBox("Source is main but destinations include sub-assets.", MessageType.Error);
                else if (!AssetDatabase.IsMainAsset(_tempSource) && mains > 0)
                    EditorGUILayout.HelpBox("Source is sub-asset but destinations include main assets.", MessageType.Error);
            }
        }

        void DrawActionSection()
        {
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            if (!_actionable) return;

            var list = GetDestinations().ToList();
            int total = list.Count;
            int mains = list.Count(AssetDatabase.IsMainAsset);
            int subs = total - mains;
            bool isSrcMain = AssetDatabase.IsMainAsset(_tempSource);

            using (new GuiHelpers.Disabled(total <= 0))
            {
                GUILayout.Label("Structure & References");
                using (new GuiHelpers.Disabled(!isSrcMain))
                {
                    if (GUILayout.Button($"Copy structure & references\nSource main {GuiHelpers.ArrowSymbol} {mains} Dest mains"))
                        CopyMainAsset(_tempSource, list.Where(AssetDatabase.IsMainAsset).ToList());
                }
                using (new GuiHelpers.Disabled(isSrcMain))
                {
                    if (GUILayout.Button($"Copy structure & references\nSource sub {GuiHelpers.ArrowSymbol} {subs} Dest subs"))
                        CopySubAsset(_tempSource, list.Where(o => !AssetDatabase.IsMainAsset(o)).ToList());
                }

                GUILayout.Label("Serialized values");
                if (GUILayout.Button($"Copy serialized values\nSource {GuiHelpers.ArrowSymbol} {total} destinations"))
                    CopySerializedValues(_tempSource, list);
            }
        }

        // --- copy strategies ---

        static void CopyMainAsset(Object src, List<Object> dsts)
        {
            var srcMap = MapSubAssetsByName(src);
            foreach (var dst in dsts)
            {
                var dstMap = MapSubAssetsByName(dst);
                var remap = new Dictionary<Object, Object>(); // srcObj -> dstObj
                foreach (var sk in srcMap)
                {
                    if (!sk.Value) continue;
                    if (!dstMap.TryGetValue(sk.Key, out var d))
                    {
                        d = Object.Instantiate(sk.Value);
                        d.name = sk.Value.name;
                        AssetDatabase.AddObjectToAsset(d, dst);
                        dstMap[sk.Key] = d;
                        Debug.Log($"{AssetHelpers.ReadableId(dst)}: Added {d}");
                    }
                    remap[sk.Value] = d;
                }
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(dst), ImportAssetOptions.ForceUpdate);
                RemapReferences(dstMap.Values, remap);
            }
        }

        static void CopySubAsset(Object src, List<Object> dsts)
        {
            var srcMain = AssetHelpers.GetMainAsset(src);
            var srcChildren = MapSubAssetsByName(srcMain)
                .Where(kv => kv.Value && kv.Value != src && IsSubOfSub(src, kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var dst in dsts)
            {
                var dstMain = AssetHelpers.GetMainAsset(dst);
                var dstMap = MapSubAssetsByName(dstMain);
                var remap = new Dictionary<Object, Object> { [src] = dst };

                foreach (var kv in srcChildren)
                {
                    var dstKey = kv.Key;
                    if (dstKey.StartsWith(src.name)) dstKey = dst.name + dstKey.Substring(src.name.Length);
                    if (!dstMap.TryGetValue(dstKey, out var d))
                    {
                        d = Object.Instantiate(kv.Value);
                        d.name = dstKey;
                        AssetDatabase.AddObjectToAsset(d, dstMain);
                        dstMap[kv.Key] = d;
                        Debug.Log($"{AssetHelpers.ReadableId(dstMain)}: Added {d}");
                    }
                    remap[kv.Value] = d;
                }
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(dstMain), ImportAssetOptions.ForceUpdate);
                RemapReferences(remap.Keys.Where(o => o != src).Select(k => remap[k]).Append(dst), remap);
            }
        }

        static void CopySerializedValues(Object src, IEnumerable<Object> dsts)
        {
            foreach (var d in dsts) CopySerializedValues(src, d);
        }

        static void CopySerializedValues(Object src, Object dst)
        {
            using var srcSO = new SerializedObject(src);
            using var dstSO = new SerializedObject(dst);

            var srcIt = srcSO.GetIterator();
            if (srcIt.Next(true))
            {
                do
                {
                    if (srcIt.name == "m_Name" || srcIt.name == "m_Script" || srcIt.name == "m_EditorClassIdentifier") continue;
                    var dstProp = dstSO.FindProperty(srcIt.propertyPath);
                    if (dstProp == null) continue;
                    dstSO.CopyFromSerializedProperty(srcIt);
                }
                while (srcIt.Next(false));
            }
            dstSO.ApplyModifiedProperties();
        }

        // --- helpers ---

        static Dictionary<string, Object> MapSubAssetsByName(Object mainAsset)
        {
            var dict = new Dictionary<string, Object>();
            if (!mainAsset) return dict;
            foreach (var o in AssetHelpers.GetAllAssets(mainAsset).OfType<ScriptableObject>())
            {
                var key = o == mainAsset ? "" : o.name;
                if (dict.ContainsKey(key))
                    Debug.LogError($"Duplicate sub-asset name '{key}' under {AssetHelpers.ReadableId(mainAsset)}", mainAsset);
                dict[key] = o;
            }
            return dict;
        }

        static bool IsSubOfSub(Object parent, Object child)
        {
            return child.name.StartsWith(parent.name + "/");
        }

        static void RemapReferences(IEnumerable<Object> targets, Dictionary<Object, Object> remap)
        {
            foreach (var t in targets)
            {
                if (!t) continue;
                using var so = new SerializedObject(t);
                var ite = so.GetIterator();
                while (ite.Next(true))
                {
                    if (ite.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var cur = ite.objectReferenceValue;
                    if (cur && remap.TryGetValue(cur, out var mapped))
                    {
                        ite.objectReferenceValue = mapped;
                    }
                }
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif

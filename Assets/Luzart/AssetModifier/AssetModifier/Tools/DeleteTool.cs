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
    /// Delete main and sub-assets. Before deleting, walks every sibling asset and
    /// nulls out direct object references, and removes any array elements that
    /// pointed at the objects — preventing dangling references.
    /// </summary>
    public class DeleteTool : AssetModifierTool
    {
        public override string Label => "Delete";

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
                GuiHelpers.GuideText("Delete main and sub assets. Cleans up references pointing to them in the same asset files."));
            GuiHelpers.Separator();
            if (GUILayout.Button("Delete selected")) DeleteAll();
        }

        void DeleteAll()
        {
            var objs = GetSelectedObjects().Where(o => o).ToList();
            if (objs.Count == 0) return;

            SplitDeletion(objs, out var mainAssets, out var subAssets);

            int subCount = subAssets.Values.Sum(l => l.Count);
            var msg = $"Delete {mainAssets.Count} main asset(s) and {subCount} sub-asset(s) (inside {subAssets.Count} other main asset(s))?\nThis cannot be undone.";
            if (!EditorUtility.DisplayDialog("Delete assets", msg, "Delete", "Cancel")) return;

            NullRelatedReferences(objs);

            AssetDatabase.StartAssetEditing();
            try
            {
                if (mainAssets.Count > 0)
                {
                    var paths = mainAssets.Select(AssetDatabase.GetAssetPath).ToList();
                    var failed = new List<string>();
                    AssetDatabase.DeleteAssets(paths.ToArray(), failed);
                    if (failed.Count > 0)
                        Debug.LogError("Deletion failed for:\n" + string.Join("\n", failed));
                }
                foreach (var kv in subAssets)
                    foreach (var s in kv.Value)
                        AssetDatabase.RemoveObjectFromAsset(s);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.SaveAssets();
            foreach (var o in subAssets.Keys)
                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(o), ImportAssetOptions.ForceUpdate);
        }

        static void NullRelatedReferences(IEnumerable<Object> deletions)
        {
            var toDelete = new HashSet<Object>(deletions);
            var related = deletions
                .Select(AssetDatabase.GetAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .Where(o => o)
                .Distinct()
                .ToList();

            foreach (var ro in related)
            {
                using var so = new SerializedObject(ro);
                var p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType == SerializedPropertyType.ObjectReference && !AssetHelpers.IsArrayElementProperty(p))
                    {
                        if (toDelete.Contains(p.objectReferenceValue))
                            p.objectReferenceValue = null;
                    }
                    else if (p.isArray)
                    {
                        for (int i = p.arraySize - 1; i >= 0; i--)
                        {
                            var ap = p.GetArrayElementAtIndex(i);
                            if (ap.propertyType == SerializedPropertyType.ObjectReference
                                && toDelete.Contains(ap.objectReferenceValue))
                                p.DeleteArrayElementAtIndex(i);
                        }
                    }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void SplitDeletion(IEnumerable<Object> objs, out List<Object> mainAssets, out Dictionary<Object, List<Object>> subAssets)
        {
            mainAssets = objs.Where(AssetDatabase.IsMainAsset).ToList();
            subAssets = new Dictionary<Object, List<Object>>();
            foreach (var o in objs)
            {
                if (!AssetDatabase.Contains(o)) continue;
                if (AssetDatabase.IsMainAsset(o)) continue;
                var main = AssetHelpers.GetMainAsset(o);
                if (main == null || mainAssets.Contains(main)) continue;
                if (!subAssets.TryGetValue(main, out var l))
                    subAssets[main] = l = new List<Object>();
                l.Add(o);
            }
        }
    }
}
#endif

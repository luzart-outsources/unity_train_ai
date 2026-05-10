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
    /// Miscellaneous structural fixes on selected ScriptableObjects.
    /// Currently: auto-link null object-reference fields to sub-assets by closest-name
    /// match (Levenshtein distance).
    /// </summary>
    public class ModifyTool : AssetModifierTool
    {
        public override string Label => "Modify";

        public override void OnGUI()
        {
            GuiHelpers.BoxedGroup("Guide", () =>
            {
                GuiHelpers.GuideText("Auto-link null object-reference fields on selected ScriptableObjects to sub-assets with the closest-matching name (Levenshtein distance <= 2).");
            });
            GuiHelpers.Separator();

            if (GUILayout.Button("Auto select null child references by property name"))
                AutoSelectChildReferences(GetSelectedObjects().OfType<ScriptableObject>());
        }

        static void AutoSelectChildReferences(IEnumerable<ScriptableObject> objects)
        {
            foreach (var obj in objects)
            {
                var all = AssetHelpers.GetAllAssets(obj);
                var nameMap = all.ToDictionary(a => a, a => a.name.ToLower().Replace(" ", "").Replace("|", ""));

                using var so = new SerializedObject(obj);
                var ite = so.GetIterator();
                while (ite.Next(true))
                {
                    if (ite.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (ite.objectReferenceValue) continue;

                    var wanted = ite.name.ToLower().Replace(" ", "");
                    Object best = null;
                    int bestDist = int.MaxValue;
                    foreach (var kv in nameMap)
                    {
                        int d = AssetHelpers.Levenshtein(wanted, kv.Value);
                        if (d < bestDist) { bestDist = d; best = kv.Key; }
                    }
                    if (bestDist <= 2 && best != null) ite.objectReferenceValue = best;
                }
                so.ApplyModifiedProperties();
            }
        }
    }
}
#endif

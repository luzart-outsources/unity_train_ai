using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "DayDB", menuName = "TrainAI/DB/Day")]
    public class DayDB : ScriptableObject
    {
        public List<DaySO> all = new();

        public DaySO ByIndex(int dayIndex)
            => all.FirstOrDefault(d => d != null && d.dayIndex == dayIndex);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Days/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:DaySO", new[] { "Assets/_Data/Days" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<DaySO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .OrderBy(d => d.dayIndex)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

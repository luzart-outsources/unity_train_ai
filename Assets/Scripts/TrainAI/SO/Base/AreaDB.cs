using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "AreaDB", menuName = "TrainAI/DB/Area")]
    public class AreaDB : ScriptableObject
    {
        public List<AreaSO> all = new();

        public AreaSO ById(string id) => all.FirstOrDefault(a => a != null && a.id == id);

        public AreaSO NearestTo(Vector3 worldPos)
        {
            AreaSO best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) continue;
                float sqr = (all[i].worldPos - worldPos).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = all[i]; }
            }
            return best;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Areas/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:AreaSO", new[] { "Assets/_Data/Areas" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<AreaSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

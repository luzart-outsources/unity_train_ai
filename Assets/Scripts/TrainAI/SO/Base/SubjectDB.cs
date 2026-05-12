using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "SubjectDB", menuName = "TrainAI/DB/Subject")]
    public class SubjectDB : ScriptableObject
    {
        public List<SubjectSO> all = new();

        public SubjectSO ById(string id) => all.FirstOrDefault(s => s != null && s.id == id);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Strategies/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:SubjectSO", new[] { "Assets/_Data" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<SubjectSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

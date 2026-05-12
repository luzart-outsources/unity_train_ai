using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "QuestDB", menuName = "TrainAI/DB/Quest")]
    public class QuestDB : ScriptableObject
    {
        public List<QuestSO> all = new();

        public QuestSO ById(string id) => all.FirstOrDefault(q => q != null && q.id == id);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Quests/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:QuestSO", new[] { "Assets/_Data/Quests" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<QuestSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

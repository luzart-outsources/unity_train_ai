using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "InteractableDB", menuName = "TrainAI/DB/Interactable")]
    public class InteractableDB : ScriptableObject
    {
        public List<InteractableSO> all = new();

        public InteractableSO ById(string id) => all.FirstOrDefault(i => i != null && i.id == id);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Interactables/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:InteractableSO", new[] { "Assets/_Data/Interactables" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<InteractableSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

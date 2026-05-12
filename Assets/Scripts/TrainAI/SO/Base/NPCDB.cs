using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "NPCDB", menuName = "TrainAI/DB/NPC")]
    public class NPCDB : ScriptableObject
    {
        public List<NPCSO> all = new();

        public NPCSO ById(string id) => all.FirstOrDefault(n => n != null && n.id == id);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/NPCs/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:NPCSO", new[] { "Assets/_Data/NPCs" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<NPCSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TrainAI.SO.Base
{
    [CreateAssetMenu(fileName = "QuizDB", menuName = "TrainAI/DB/Quiz")]
    public class QuizDB : ScriptableObject
    {
        public List<QuizSetSO> all = new();

        public QuizSetSO BySubject(SubjectSO subject)
            => all.FirstOrDefault(q => q != null && q.subject == subject);

#if UNITY_EDITOR
        [ContextMenu("Auto-populate from /Assets/_Data/Quizzes/")]
        void AutoPopulate()
        {
            all = UnityEditor.AssetDatabase
                .FindAssets("t:QuizSetSO", new[] { "Assets/_Data/Quizzes" })
                .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<QuizSetSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
                .Where(x => x != null)
                .ToList();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

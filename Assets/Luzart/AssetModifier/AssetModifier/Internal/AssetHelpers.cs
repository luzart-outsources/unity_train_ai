#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetModifier.Internal
{
    /// <summary>
    /// Pure-Unity replacements for the few utility calls the window relies on.
    /// </summary>
    internal static class AssetHelpers
    {
        public static bool IsSubAsset(Object o)
        {
            if (o == null) return false;
            if (!AssetDatabase.Contains(o)) return false;
            return !AssetDatabase.IsMainAsset(o);
        }

        public static Object GetMainAsset(Object o)
        {
            if (o == null) return null;
            var path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadMainAssetAtPath(path);
        }

        /// <summary>
        /// Returns every asset at the same path as <paramref name="o"/>'s main asset
        /// (main + all sub-assets).
        /// </summary>
        public static Object[] GetAllAssets(Object o)
        {
            var path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) return System.Array.Empty<Object>();
            return AssetDatabase.LoadAllAssetsAtPath(path);
        }

        public static void AddSubAsset(Object mainAsset, Object child)
        {
            AssetDatabase.AddObjectToAsset(child, mainAsset);
        }

        /// <summary>Short, human-readable id for logging.</summary>
        public static string ReadableId(Object o)
        {
            if (o == null) return "<null>";
            var path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) return o.name;
            var file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (AssetDatabase.IsMainAsset(o)) return file;
            return file + "/" + o.name;
        }

        /// <summary>
        /// Classic Levenshtein distance (iterative, O(n*m)). Used by ModifyTool auto-link.
        /// </summary>
        public static int Levenshtein(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b)) return a.Length;
            var dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    dp[i, j] = Mathf.Min(Mathf.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                }
            return dp[a.Length, b.Length];
        }

        public static IEnumerable<SerializedProperty> IterateArrayElements(SerializedProperty arr)
        {
            if (arr == null || !arr.isArray) yield break;
            for (int i = 0; i < arr.arraySize; i++) yield return arr.GetArrayElementAtIndex(i);
        }

        public static bool IsArrayElementProperty(SerializedProperty p)
        {
            if (p == null) return false;
            var path = p.propertyPath;
            return path.Contains("Array.data[") && path.EndsWith("]");
        }
    }
}
#endif

using System.IO;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class BlueprintLoader
    {
        public const string BlueprintPath = "Assets/Editor/Automation/world_template.json";

        public static WorldBlueprint Load()
        {
            if (!File.Exists(BlueprintPath))
            {
                Debug.LogError($"[BlueprintLoader] blueprint missing at {BlueprintPath}");
                return null;
            }
            try
            {
                string txt = File.ReadAllText(BlueprintPath);
                return JsonUtility.FromJson<WorldBlueprint>(txt);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BlueprintLoader] parse failed: {e.Message}");
                return null;
            }
        }

        public static T CreateOrLoad<T>(string assetPath) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (existing != null) return existing;
            var dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var so = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(so, assetPath);
            return so;
        }
    }
}

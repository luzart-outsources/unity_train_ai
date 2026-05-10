using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    // Scan project tim cac asset can dung trong setup tool.
    // Khong throw - tra ve null neu khong tim duoc, caller log.
    public static class AssetScanner
    {
        public static Sprite FindSpriteByName(string nameContains)
        {
            string[] guids = AssetDatabase.FindAssets("t:Sprite");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.IndexOf(nameContains, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp != null) return sp;
            }
            // Fallback: find png and try as Sprite import.
            return null;
        }

        public static Sprite FindKenneyButton(string color = "blue")
        {
            // Try button_<color> first.
            var s = FindSpriteByName($"button_{color}");
            if (s != null) return s;
            return FindSpriteByName("button_grey") ?? FindSpriteByName("button_brown");
        }

        public static Sprite FindKenneyPanel()
        {
            var s = FindSpriteByName("panel_brown");
            if (s != null) return s;
            return FindSpriteByName("panel");
        }

        public static Sprite FindKenneyCheckbox(bool checkedState)
        {
            var s = FindSpriteByName(checkedState ? "checkbox_brown_checked" : "checkbox_brown_empty");
            if (s != null) return s;
            return FindSpriteByName(checkedState ? "checkbox_grey_checked" : "checkbox_grey_empty");
        }

        public static TMP_FontAsset FindDefaultTmpFont()
        {
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            foreach (var g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (path.Contains("LiberationSans") || path.Contains("Liberation"))
                    return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            }
            // Fallback: any TMP font.
            foreach (var g in guids)
            {
                var a = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) return a;
            }
            return null;
        }

        public static GameObject FindCharacterFallback()
        {
            // Try existing Player prefab first.
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (player != null) return player;

            // PolygonFarm character.
            string[] candidates = {
                "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_FarmBoy_01.prefab",
                "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Male_01.prefab",
            };
            foreach (var p in candidates)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) return go;
            }
            return null;
        }

        public static GameObject FindNPCFallback()
        {
            string[] candidates = {
                "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Male_Old_01.prefab",
                "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_Farmer_Female_01.prefab",
                "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_FarmGirl_01.prefab",
            };
            foreach (var p in candidates)
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (go != null) return go;
            }
            return FindCharacterFallback();
        }

        public static AudioClip FindAnyAudioClip()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static void EnsureFolder(string path)
        {
            // path = "Assets/Configs/TrainAI/Quests"
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}

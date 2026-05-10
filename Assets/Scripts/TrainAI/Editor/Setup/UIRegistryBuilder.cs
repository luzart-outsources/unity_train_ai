using System.Collections.Generic;
using System.Reflection;
using Luzart;
using TrainAI.UI.Components;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor.Setup
{
    public static class UIRegistryBuilder
    {
        private const string Path = "Assets/Configs/TrainAI/UIRegistry.asset";

        public static UIRegistrySO Build(PrefabBuilder.BuiltPrefabs prefabs)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UIRegistrySO>(Path);
            UIRegistrySO registry = existing != null ? existing : ScriptableObject.CreateInstance<UIRegistrySO>();

            var entries = BuildEntries(prefabs);

            // Reflection vao field 'entries' (private).
            var f = typeof(UIRegistrySO).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
            if (f == null)
            {
                Debug.LogError("[UIRegistryBuilder] Cannot find UIRegistrySO.entries field via reflection.");
                return registry;
            }
            f.SetValue(registry, entries);
            EditorUtility.SetDirty(registry);

            if (existing == null) AssetDatabase.CreateAsset(registry, Path);
            registry.BuildLookup();
            AssetDatabase.SaveAssets();
            return registry;
        }

        private static List<UIConfig> BuildEntries(PrefabBuilder.BuiltPrefabs p)
        {
            var list = new List<UIConfig>();
            list.Add(new UIConfig
            {
                Id = UIId.MainMenu,
                StringId = "main_menu",
                AssetRef = p.MainMenu,
                Lane = UILayer.Screen,
                CachePolicy = UICachePolicy.KeepLoaded,
                PreloadOnBoot = true,
                AllowMultiInstance = false,
                DismissByEscape = false,
                PausableWhenOverlaid = false,
            });
            list.Add(new UIConfig
            {
                Id = UIId.CreateCharacter,
                StringId = "char_create",
                AssetRef = p.CharCreate,
                Lane = UILayer.Screen,
                CachePolicy = UICachePolicy.PoolOnClose,
                DismissByEscape = false,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.OpeningCutscene,
                StringId = "cutscene_opening",
                AssetRef = p.OpeningCutscene,
                Lane = UILayer.Screen,
                CachePolicy = UICachePolicy.ReleaseOnClose,
                DismissByEscape = false,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.Ending,
                StringId = "ending",
                AssetRef = p.Ending,
                Lane = UILayer.Screen,
                CachePolicy = UICachePolicy.ReleaseOnClose,
                DismissByEscape = false,
            });
            list.Add(new UIConfig
            {
                Id = UIId.GameplayHud,
                StringId = "hud",
                AssetRef = p.GameplayHud,
                Lane = UILayer.Hud,
                CachePolicy = UICachePolicy.KeepLoaded,
                PausableWhenOverlaid = false,
            });
            list.Add(new UIConfig
            {
                Id = UIId.Loading,
                StringId = "loading",
                AssetRef = p.Loading,
                Lane = UILayer.System,
                CachePolicy = UICachePolicy.KeepLoaded,
                PreloadOnBoot = true,
                DismissByEscape = false,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.Confirm,
                StringId = "confirm",
                AssetRef = p.Confirm,
                Lane = UILayer.Popup,
                CachePolicy = UICachePolicy.KeepLoaded,
                PreloadOnBoot = true,
                PausableWhenOverlaid = true,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.Quiz,
                StringId = "quiz",
                AssetRef = p.Quiz,
                Lane = UILayer.Popup,
                CachePolicy = UICachePolicy.PoolOnClose,
                PreloadOnBoot = true,
                DismissByEscape = false,
                PausableWhenOverlaid = true,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.Dialogue,
                StringId = "dialogue",
                AssetRef = p.Dialogue,
                Lane = UILayer.Popup,
                CachePolicy = UICachePolicy.KeepLoaded,
                PausableWhenOverlaid = true,
            });
            list.Add(new UIConfig
            {
                Id = UIIdGame.KickedOut,
                StringId = "kicked_out",
                AssetRef = p.KickedOut,
                Lane = UILayer.System,
                CachePolicy = UICachePolicy.KeepLoaded,
                PreloadOnBoot = true,
                DismissByEscape = false,
            });
            list.Add(new UIConfig
            {
                Id = UIId.Toast,
                StringId = "toast",
                AssetRef = p.Toast,
                Lane = UILayer.Toast,
                CachePolicy = UICachePolicy.KeepLoaded,
                PreloadOnBoot = true,
                AllowMultiInstance = true,
                DismissByEscape = false,
            });
            return list;
        }
    }
}

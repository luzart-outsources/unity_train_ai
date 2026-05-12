using System.IO;
using TrainAI.SO.Base;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    public static class DebugMenu
    {
        [MenuItem("Tools/Debug/TrainAI/Reset Save (slot 1)")]
        public static void ResetSave()
        {
            var path = Path.Combine(Application.persistentDataPath, "save_slot1.json");
            if (File.Exists(path)) { File.Delete(path); Debug.Log($"[Debug] deleted {path}"); }
            else Debug.Log("[Debug] no save file to delete");
        }

        [MenuItem("Tools/Debug/TrainAI/Skip Clock +1 day")]
        public static void SkipDay()
        {
            var clock = LoadFirst<GameClockRSO>();
            if (clock == null) { Debug.LogWarning("[Debug] no GameClockRSO"); return; }
            clock.day++;
            EditorUtility.SetDirty(clock);
            Debug.Log($"[Debug] day -> {clock.day}");
        }

        [MenuItem("Tools/Debug/TrainAI/Reset PlayerState")]
        public static void ResetPlayerState()
        {
            var st = LoadFirst<PlayerStateRSO>();
            if (st == null) { Debug.LogWarning("[Debug] no PlayerStateRSO"); return; }
            st.Reset();
            EditorUtility.SetDirty(st);
            Debug.Log("[Debug] PlayerStateRSO reset");
        }

        static T LoadFirst<T>() where T : ScriptableObject
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (var g in guids)
            {
                var a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g));
                if (a != null) return a;
            }
            return null;
        }
    }
}

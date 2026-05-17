using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Batch-mode entry point for offline scene/prefab regeneration. Invoked via:
    //   Unity.exe -batchmode -nographics -projectPath <path>
    //            -executeMethod TrainAI.Editor.AutoBuildEntry.RunAll
    //            -quit -logFile Build.log
    //
    // Lets us rebuild the world + swap the player visual without needing the
    // MCP bridge to be working — handy when the editor is being driven from
    // CLI or recovery scenarios.
    public static class AutoBuildEntry
    {
        public static void RunAll()
        {
            Debug.Log("[AutoBuild] starting");
            try
            {
                Step("Swap Player Visual",     PlayerVisualSwapper.SwapVisual);
                Step("Build HOLA Layout",      HolaMapLayoutBuilder.BuildHolaLayout);
                Step("Attach MinimapThrottle", MinimapThrottleSetup.Attach);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[AutoBuild] DONE");
            }
            catch (Exception e)
            {
                Debug.LogError("[AutoBuild] FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }

        static void Step(string label, Action a)
        {
            Debug.Log("[AutoBuild] >> " + label);
            a();
            Debug.Log("[AutoBuild] << " + label);
        }
    }
}

using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // Batch entry: rebuild GLBs from scratch, bake them into combined meshes,
    // and report renderer count. Use this from CLI with -executeMethod to
    // validate the baked workflow without play mode.
    public static class BakeAndTestEntry
    {
        public static void RunBake()
        {
            try
            {
                Debug.Log("[BakeAndTest] step 1: rebuild HOLA layout (restore GLBs)");
                HolaMapLayoutBuilder.BuildHolaLayout();

                Debug.Log("[BakeAndTest] step 2: bake GLBs into combined meshes");
                BakeBuildingsTool.Bake();

                Debug.Log("[BakeAndTest] step 3: re-attach MinimapThrottle (Bake re-saved scene)");
                MinimapThrottleSetup.Attach();

                Debug.Log("[BakeAndTest] step 4: re-apply minimal visuals (NPCs off, Player static)");
                MinimalVisualMode.Enable();

                Debug.Log("[BakeAndTest] DONE");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[BakeAndTest] FAILED: " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}

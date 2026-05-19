using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // Single batch-mode entry point that runs the post-fix build steps in
    // sequence (idempotent). Designed for: Unity.exe -batchmode -quit
    //   -executeMethod TrainAI.Editor.AutoFinalizeFixes.Run
    //
    // Steps:
    //   1. Open 10_World
    //   2. Build minimap markers (y=0.6 above ground)
    //   3. Spawn NPCs (4 total: commander + 3 students)
    //   4. Run StressSim (logic regression)
    //   5. Run Playtest30DaySim (full economy)
    // Reports are written to Library/playtest_*_sim.md by the sims.
    public static class AutoFinalizeFixes
    {
        public static void Run()
        {
            Debug.Log("[AutoFinalizeFixes] === START ===");
            try
            {
                Debug.Log("[AutoFinalizeFixes] Step 1: Build minimap markers");
                MinimapMarkerBuilder.Build();

                Debug.Log("[AutoFinalizeFixes] Step 2: Spawn NPCs");
                NpcPlacer.SpawnNpcs();

                Debug.Log("[AutoFinalizeFixes] Step 3: Stress Sim");
                PlaytestStressSim.Run();

                Debug.Log("[AutoFinalizeFixes] Step 4: 30-Day Sim");
                Playtest30DaySim.Run();

                Debug.Log("[AutoFinalizeFixes] === DONE ===");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AutoFinalizeFixes] FAILED: {e}");
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }
    }
}

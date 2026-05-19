using System;
using System.Collections;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEngine;

namespace TrainAI.Presentation
{
    // Stages NPC.prefab instantiation across multiple frames so the heavy
    // AdvancedPeopleSystem2 character (5+ SkinnedMeshRenderers + humanoid
    // Animator + materials) doesn't all upload to the GPU in one
    // command-buffer dispatch. That all-at-once upload tripped a D3D12 TDR
    // on the GTX 1060 Max-Q when 7 NPCs spawned simultaneously at scene
    // load (per project memory 2026-05-19 03:29 crash dump).
    //
    // Why staged instead of plain:
    //   - Player + first-frame burst of 7 NPCs = 8 skinned characters
    //     registering with the renderer at the same tick → GPU command
    //     queue overflow on weaker hardware. 1 NPC per few frames keeps the
    //     incremental load comfortably below TDR threshold.
    //   - Trade visual: first 2-3 NPCs are visible by ~10 frames in (negligible);
    //     last NPC visible by ~15 frames (~0.25s at 60 FPS). Player's spawn
    //     animation usually covers this delay.
    //
    // Why a separate component (not inside NpcPlacer at edit time):
    //   - Editor-time placement instantiates all prefabs in one
    //     EditorSceneManager.SaveScene pass, which Unity loads in a single
    //     frame at play-mode entry. Coroutines only exist in runtime, so
    //     the staging must happen runtime-side.
    //   - Keeps NpcPlacer responsible for "where" (positions, NPCSO bindings)
    //     and the spawner responsible for "when" (frame budget).
    public class NpcStagedSpawner : MonoBehaviour
    {
        [Serializable]
        public struct SpawnEntry
        {
            public NPCSO npcDef;
            public Vector3 position;
            public Vector3 eulerAngles;
            public string label; // for hierarchy name; ""=use npcDef.id
        }

        [Tooltip("Prefab containing the AdvancedPeopleSystem2 visual + NpcView component.")]
        [SerializeField] GameObject npcPrefab;

        [Tooltip("ServiceLocator the spawned NPCs should register with.")]
        [SerializeField] ServiceLocatorSO services;

        [Tooltip("Per-NPC spawn data baked at editor time by NpcPlacer.")]
        [SerializeField] SpawnEntry[] entries;

        [Tooltip("Frames to wait between each NPC spawn. 3 frames @ 60FPS = 50ms, enough breathing room for GPU command queue to drain between heavy character uploads.")]
        [SerializeField] int framesBetweenSpawns = 3;

        [Tooltip("Frames to wait before the FIRST spawn — lets the scene's own renderers (V11 buildings, minimap RT first render) settle before adding any NPC.")]
        [SerializeField] int initialDelayFrames = 5;

        IEnumerator Start()
        {
            if (npcPrefab == null || entries == null || entries.Length == 0)
            {
                Debug.LogWarning("[NpcStagedSpawner] no prefab or entries configured — skipping spawn.");
                yield break;
            }

            for (int i = 0; i < initialDelayFrames; i++) yield return null;

            int spawned = 0;
            for (int i = 0; i < entries.Length; i++)
            {
                var e = entries[i];
                if (e.npcDef == null) continue;

                var go = Instantiate(npcPrefab, e.position, Quaternion.Euler(e.eulerAngles), transform);
                go.name = $"NPC_{(string.IsNullOrEmpty(e.label) ? e.npcDef.id : e.label)}";

                // Wire NpcView refs via Configure() — inspector serialization
                // can't propagate to a freshly Instantiated object that copies
                // the prefab's default (null) refs.
                var view = go.GetComponent<NpcView>();
                if (view == null) view = go.AddComponent<NpcView>();
                view.Configure(e.npcDef, services);

                // AnimatorDriver for the AdvancedPeopleSystem2 visual's
                // `walk` bool — Player uses the same pattern. NpcAnimatorDriver
                // tolerates absent animator (early-returns), so safe to add
                // unconditionally.
                if (go.GetComponent<NpcAnimatorDriver>() == null)
                    go.AddComponent<NpcAnimatorDriver>();

                spawned++;

                for (int f = 0; f < framesBetweenSpawns; f++) yield return null;
            }

            Debug.Log($"[NpcStagedSpawner] staged {spawned}/{entries.Length} NPCs across {entries.Length * framesBetweenSpawns + initialDelayFrames} frames.");
        }

        // Editor-time helper so NpcPlacer can populate the entries without
        // touching SerializedObject machinery from the runtime side.
        public void SetSpawnData(GameObject prefab, ServiceLocatorSO svc, SpawnEntry[] entryList)
        {
            npcPrefab = prefab;
            services = svc;
            entries = entryList;
        }
    }
}

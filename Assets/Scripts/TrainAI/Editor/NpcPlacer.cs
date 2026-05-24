using System.Collections.Generic;
using TrainAI.Presentation;
using TrainAI.Services;
using TrainAI.SO.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Spawns student NPCs in 10_World so the campus feels populated. Per
    // GDD section 88-89 student NPCs ("NPC di chuyển tự do — học sinh")
    // walk along their schedule between SanVanDong / DonVeSinh / LopHoc /
    // KTX waypoints, and the đại đội trưởng (static commander) sits at
    // his dialogue spot.
    //
    // Why an Editor tool instead of runtime spawn:
    //   - Same pattern as HOLA layout builders (idempotent, re-runnable
    //     from the menu, clean root container so the placement is easy
    //     to inspect in the hierarchy).
    //   - Avoids the BootstrapEntry singleton dance — every NPC is just a
    //     plain scene-side GameObject that NpcView.Start() registers with
    //     NPCDirector when services come online.
    //   - Lets a designer manually tweak per-NPC starting positions in
    //     the scene without code edits.
    //
    // Idempotence: rerunning the menu item wipes the previous "_NPCs"
    // container and rebuilds from current data. Designer changes inside
    // _NPCs get overwritten — by design, since this is the canonical
    // population step.
    public static class NpcPlacer
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";
        const string ContainerName = "_NPCs";
        const string LocatorPath = "Assets/_Data/Config/ServiceLocator.asset";

        // Per GDD, students roam between waypoints set in their Schedule SO.
        // Spawn coords are eyeballed against the V11 HOLA layout (B plaza
        // and SVD area) — exact positions don't matter; the scheduler will
        // route them out within the first in-game minute.
        struct Slot { public string npcGuid; public Vector3 pos; public string label; }

        [MenuItem("Tools/Build Game/NPCs/Spawn Students in 10_World (LIGHTWEIGHT)", false, 240)]
        public static void SpawnNpcs()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            // Previous version instantiated NPC.prefab which carries an
            // AdvancedPeopleSystem2 character visual (~5 SkinnedMeshRenderers
            // + humanoid Animator + several materials per instance). 7
            // instances of that triggered a GPU TDR (DXGI_ERROR_DEVICE_HUNG,
            // 887a0006) on the GTX 1060 Max-Q during play-mode entry —
            // command-buffer dispatch timed out from first-frame skinned
            // mesh upload + minimap RT render. Memory was NOT exhausted
            // (561 MB / 5.5 GB budget) per the crash report.
            //
            // LIGHTWEIGHT path: each NPC is a primitive capsule with a flat
            // colored material and a vertical "head" cylinder for silhouette
            // distinction. No skinned mesh, no Animator (NpcAnimatorDriver
            // tolerates missing animator), no rig. ~1 draw call per NPC vs
            // ~10 for APS2. Trade visual fidelity for stability — per memory
            // the project has had repeated D3D12 crashes around heavy GPU
            // pressure on this exact GPU.
            //
            // If we later restore APS2 visuals, do so on max 1-2 NPCs and
            // pre-warm shaders before spawning (Shader.WarmupAllShaders or
            // ShaderVariantCollection) to avoid the first-frame stall that
            // tripped this TDR.

            var locator = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
            if (locator == null)
            {
                Debug.LogError($"[NpcPlacer] ServiceLocator not found at {LocatorPath}");
                return;
            }

            // Look up the NPC defs by their canonical asset paths instead of
            // GUIDs — more robust against meta-file churn during refactors.
            var npcHocSinh01 = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_HocSinh_01.asset");
            var npcHocSinh02 = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_HocSinh_02.asset");
            var npcDaiDoi   = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_DaiDoiTruong.asset");
            if (npcHocSinh01 == null || npcHocSinh02 == null)
            {
                Debug.LogError("[NpcPlacer] NPC_HocSinh_01 / NPC_HocSinh_02 asset(s) missing");
                return;
            }

            // Drop the previous container so we can rerun this idempotently.
            var existing = GameObject.Find(ContainerName);
            if (existing != null) Object.DestroyImmediate(existing);
            // Wipe SceneBuilder's legacy auto-spawned NPC_* root objects too.
            var rootGOs = scene.GetRootGameObjects();
            foreach (var go in rootGOs)
                if (go != null && go.name.StartsWith("NPC_")) Object.DestroyImmediate(go);
            var root = new GameObject(ContainerName);
            root.transform.position = Vector3.zero;

            // THREE student spawn points + 1 commander (4 NPCs total).
            // Previous 7-NPC spawn crashed Unity during play-mode entry —
            // the editor.log showed "Local memory CurrentUsage: 2.89 GB
            // / Budget: 2.6 GB" inside GfxDeviceD3D12::ExecuteCommandBuffersAsync,
            // i.e. the AdvancedPeopleSystem2 visual prefab is too heavy
            // to instantiate at 7x alongside Player on this hardware.
            // 4 instances (Player + Commander + 2 students) fit in budget
            // per the V11 renderer-count headroom (185 baked + ~200/NPC).
            // If 4 still crashes we'll fall back to primitive capsules.
            var studentSlots = new[]
            {
                new Slot { npcGuid = "01", pos = new Vector3(-12,  0,  -2), label = "HocSinh_01_a" },
                new Slot { npcGuid = "02", pos = new Vector3(  8,  0,  -6), label = "HocSinh_02_a" },
                new Slot { npcGuid = "01", pos = new Vector3(-22,  0,  10), label = "HocSinh_01_b" },
            };

            int spawned = 0;
            foreach (var slot in studentSlots)
            {
                var def = slot.npcGuid == "01" ? npcHocSinh01 : npcHocSinh02;
                var go = BuildLightweightNpc(slot.label, slot.pos, NpcTint(slot.label));
                go.transform.SetParent(root.transform, false);
                ConfigureNpc(go, def, locator);
                spawned++;
            }

            // Đại đội trưởng — static dialogue NPC, distinct color so the
            // player can spot him at a glance. Movement_Idle so he doesn't
            // wander off the plaza centre.
            if (npcDaiDoi != null)
            {
                var commander = BuildLightweightNpc("DaiDoiTruong", new Vector3(78, 0, -22), new Color(0.85f, 0.15f, 0.20f));
                commander.transform.SetParent(root.transform, false);
                commander.transform.rotation = Quaternion.Euler(0, 180, 0);
                ConfigureNpc(commander, npcDaiDoi, locator);
                spawned++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[NpcPlacer] spawned {spawned} LIGHTWEIGHT NPCs in '{ContainerName}'.");
        }

        // 3-primitive capsule body + head silhouette. Each NPC = 2 mesh
        // renderers sharing the same material per color (so the dynamic
        // batcher folds same-color NPCs into a single draw call). No
        // SkinnedMeshRenderer, no Animator → safe for play-mode entry on
        // GPUs that struggled with the APS2 character (GTX 1060 Max-Q TDR).
        static GameObject BuildLightweightNpc(string label, Vector3 pos, Color tint)
        {
            var npc = new GameObject($"NPC_{label}");
            npc.tag = "NPC";
            npc.transform.position = pos;

            // Trigger capsule on the root so PlayerInteractor's OverlapSphere
            // finds the NPC at standard interact range. NpcView's interaction
            // path already expects a Collider on the host transform.
            var col = npc.AddComponent<CapsuleCollider>();
            col.radius = 0.35f;
            col.height = 1.8f;
            col.center = new Vector3(0, 0.9f, 0);
            col.isTrigger = false;

            // Body capsule — primitive's built-in CapsuleCollider would
            // double-trigger overlaps with the root one, so strip it.
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(npc.transform, false);
            body.transform.localPosition = new Vector3(0, 0.9f, 0);
            body.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var bodyCol = body.GetComponent<Collider>();
            if (bodyCol != null) Object.DestroyImmediate(bodyCol);
            body.GetComponent<Renderer>().sharedMaterial = NpcMat(tint);

            // Head sphere — slightly smaller, lighter color so different NPCs
            // are visually distinguishable at a glance from the third-person
            // view. Same shared material as the body for batching.
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(npc.transform, false);
            head.transform.localPosition = new Vector3(0, 1.85f, 0);
            head.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var headCol = head.GetComponent<Collider>();
            if (headCol != null) Object.DestroyImmediate(headCol);
            head.GetComponent<Renderer>().sharedMaterial = NpcMat(tint * 1.3f);

            return npc;
        }

        // Shared NPC material cache so dynamic batcher pools same-tint
        // NPCs into one draw call. Without this, 4 NPCs × 2 renderers ×
        // unique materials = 8 draw calls; with sharing, 1-2.
        static readonly Dictionary<string, Material> _matCache = new();
        static Material NpcMat(Color c)
        {
            string key = $"npc_{c.r:F2}_{c.g:F2}_{c.b:F2}";
            if (_matCache.TryGetValue(key, out var existing) && existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var m = new Material(shader) { name = key, color = c };
            _matCache[key] = m;
            return m;
        }

        // Stable color per slot label so same student instances always look
        // the same after re-spawn. Hash-based so adding new slots doesn't
        // shuffle the existing palette.
        static Color NpcTint(string label)
        {
            int h = label.GetHashCode();
            float hue = (Mathf.Abs(h) % 1000) / 1000f;
            return Color.HSVToRGB(hue, 0.7f, 0.85f);
        }

        // Wires every instance the same way: NpcView ↔ NPCSO + ServiceLocator,
        // and stamps NpcAnimatorDriver on top so the AdvancedPeopleSystem2
        // child's `walk` bool flips when the NPC moves. Idempotent: skips
        // duplicate adds when re-running.
        static void ConfigureNpc(GameObject go, NPCSO def, ServiceLocatorSO locator)
        {
            var view = go.GetComponent<NpcView>();
            if (view == null) view = go.AddComponent<NpcView>();
            var serView = new SerializedObject(view);
            var defProp = serView.FindProperty("npcDef");
            var svcProp = serView.FindProperty("services");
            if (defProp != null) defProp.objectReferenceValue = def;
            if (svcProp != null) svcProp.objectReferenceValue = locator;
            serView.ApplyModifiedPropertiesWithoutUndo();

            if (go.GetComponent<NpcAnimatorDriver>() == null) go.AddComponent<NpcAnimatorDriver>();
        }

        [MenuItem("Tools/Build Game/NPCs/Clear Spawned NPCs", false, 241)]
        public static void ClearNpcs()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(ContainerName);
            if (existing != null) Object.DestroyImmediate(existing);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[NpcPlacer] cleared NPCs.");
        }

        // Staged APS2 mode (Option B per the user's choice 2026-05-19):
        // installs an _NPCSpawner GameObject carrying an NpcStagedSpawner
        // component. At runtime the spawner coroutine instantiates the full
        // NPC.prefab (AdvancedPeopleSystem2 character) ONE AT A TIME with a
        // few-frame gap between spawns, so the GPU command queue isn't asked
        // to upload N skinned characters in one frame (the cause of the
        // 03:29 TDR with 7 simultaneous spawns).
        //
        // Why 3 NPCs (not the original 7): user picked Option B which scopes
        // to "2-3 NPCs". Plus DaiDoiTruong = 4 total. Even with staging, the
        // GTX 1060 Max-Q has a soft ceiling on simultaneously alive APS2
        // instances — 4 (1 player + 3 NPCs) sits well below the V11 renderer
        // budget headroom.
        //
        // Editor tool only creates the _NPCSpawner container + bakes data;
        // actual instantiation happens at runtime via NpcStagedSpawner.Start
        // coroutine. Designers can tweak framesBetweenSpawns / initialDelayFrames
        // in the inspector after running this menu without re-running.
        const string SpawnerContainerName = "_NPCSpawner";

        [MenuItem("Tools/Build Game/NPCs/Spawn Students in 10_World (APS2 + STAGED, recommended)", false, 239)]
        public static void SpawnNpcsStaged()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/TrainAI/NPC.prefab");
            if (npcPrefab == null)
            {
                Debug.LogError("[NpcPlacer] NPC.prefab not found at Assets/Prefabs/TrainAI/NPC.prefab");
                return;
            }

            var locator = AssetDatabase.LoadAssetAtPath<ServiceLocatorSO>(LocatorPath);
            if (locator == null)
            {
                Debug.LogError($"[NpcPlacer] ServiceLocator not found at {LocatorPath}");
                return;
            }

            var npcHocSinh01 = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_HocSinh_01.asset");
            var npcHocSinh02 = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_HocSinh_02.asset");
            var npcDaiDoi   = AssetDatabase.LoadAssetAtPath<NPCSO>("Assets/_Data/NPCs/NPC_DaiDoiTruong.asset");
            if (npcHocSinh01 == null || npcHocSinh02 == null)
            {
                Debug.LogError("[NpcPlacer] NPC_HocSinh_01 / NPC_HocSinh_02 asset(s) missing");
                return;
            }

            // Clear BOTH containers — staged mode is mutually exclusive with
            // the lightweight in-scene capsule path. Whichever menu the
            // designer ran last is the active mode.
            var existingNpcs = GameObject.Find(ContainerName);
            if (existingNpcs != null) Object.DestroyImmediate(existingNpcs);
            var existingSpawner = GameObject.Find(SpawnerContainerName);
            if (existingSpawner != null) Object.DestroyImmediate(existingSpawner);

            // Also wipe any root-level "NPC_*" GameObjects from SceneBuilder's
            // legacy NPCSO.spawnPos auto-spawn path. Otherwise a designer who
            // runs "5. Build Scenes" followed by this NPC menu ends up with
            // 7 NPCs (3 SceneBuilder + 4 staged) and the commander shows up
            // in two places: the asset's spawnPos and the staged V11 coord.
            var rootGOs = scene.GetRootGameObjects();
            foreach (var go in rootGOs)
                if (go != null && go.name.StartsWith("NPC_")) Object.DestroyImmediate(go);

            var spawnerGo = new GameObject(SpawnerContainerName);
            spawnerGo.transform.position = Vector3.zero;
            var spawner = spawnerGo.AddComponent<NpcStagedSpawner>();

            // 3 students + 1 commander = 4 APS2 instances total (matching
            // user's "2-3 NPCs" guidance + the commander dialogue NPC per
            // GDD). Positions eyeballed against V11 HOLA layout so they're
            // visible from the player's spawn near B plaza.
            var entries = new[]
            {
                new NpcStagedSpawner.SpawnEntry { npcDef = npcHocSinh01, position = new Vector3(-12, 0,  -2), eulerAngles = new Vector3(0, 180, 0), label = "HocSinh_01" },
                new NpcStagedSpawner.SpawnEntry { npcDef = npcHocSinh02, position = new Vector3(  8, 0,  -6), eulerAngles = new Vector3(0, 200, 0), label = "HocSinh_02" },
                new NpcStagedSpawner.SpawnEntry { npcDef = npcHocSinh01, position = new Vector3(-22, 0,  10), eulerAngles = new Vector3(0, 160, 0), label = "HocSinh_01_b" },
                npcDaiDoi != null
                    ? new NpcStagedSpawner.SpawnEntry { npcDef = npcDaiDoi, position = new Vector3(78, 0, -22), eulerAngles = new Vector3(0, 180, 0), label = "DaiDoiTruong" }
                    : default
            };

            // Strip the null-def commander entry if asset missing.
            int validCount = 0;
            foreach (var e in entries) if (e.npcDef != null) validCount++;
            var validEntries = new NpcStagedSpawner.SpawnEntry[validCount];
            int idx = 0;
            foreach (var e in entries) if (e.npcDef != null) validEntries[idx++] = e;

            spawner.SetSpawnData(npcPrefab, locator, validEntries);

            // Persist via SerializedObject so the inspector shows the wired
            // data (SetSpawnData mutates private fields directly; without
            // SerializedObject.ApplyModifiedPropertiesWithoutUndo the scene
            // wouldn't pick up the changes on save).
            var so = new SerializedObject(spawner);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(spawner);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[NpcPlacer] STAGED mode installed in '{SpawnerContainerName}' with {validEntries.Length} APS2 NPCs (3 frames between spawns).");
        }
    }
}

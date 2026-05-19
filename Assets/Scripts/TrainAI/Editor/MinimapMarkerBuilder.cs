using System.Collections.Generic;
using TrainAI.Presentation;
using TrainAI.SO.Base;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Drops a colored ground decal beneath every InteractableMarker in
    // 10_World so the minimap camera (top-down, orthographic, full culling
    // mask) renders a recognizable hotspot for each interaction zone. Per
    // GDD section 18-19 the minimap should communicate "where can I do
    // things" — without colored decals the player sees nothing but green
    // grass on the map.
    //
    // Why a per-area color scheme:
    //   - LopHoc_Door (class) -> deep blue   (study)
    //   - NhaAn_Door  (food)  -> warm orange (eat)
    //   - KTX_Door    (sleep) -> purple      (rest)
    //   - SanVanDong  (PE)    -> green       (exercise)
    //   - DonVeSinh   (chore) -> bright yellow (cleanup)
    //   - FreeArea            -> neutral grey (no quest tied to it)
    //
    // Decals are thin (height 0.05m), placed at y=0.06 so they sit just
    // above the ground material without significant z-fighting. The third-
    // person camera barely notices them at run distance; the minimap (60m+
    // up) sees them clearly.
    //
    // Per project rule "no menu overwrites": this tool is its own menu item
    // under Tools/Build Game/Minimap/...; HOLA layout builders and bake
    // tools stay untouched.
    public static class MinimapMarkerBuilder
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";
        const string RootName = "_MinimapMarkers";

        struct AreaPalette
        {
            public string idPrefix;
            public Color color;
            public Vector2 size;
        }

        // High-contrast palette picked to NOT clash with the V11 placeholder
        // building colors (dorm blue, canteen orange, dining brown, pitch
        // green). Markers use saturated cyan/magenta/red/yellow so even at
        // 256×256 minimap RT resolution they read as distinct "hotspot"
        // pins instead of blending into the buildings they sit under.
        //
        // Sizes are deliberately oversized (10×10 for doors, not 6×6) — the
        // RT is small, the world is 200×140, so a 6m square is barely 8px
        // on the minimap. 10m+ ensures the pin is visible at a glance.
        static readonly AreaPalette[] Palette = new[]
        {
            new AreaPalette { idPrefix = "LopHoc",     color = new Color(0.10f, 0.95f, 0.95f), size = new Vector2(10, 10) }, // bright cyan
            new AreaPalette { idPrefix = "NhaAn",      color = new Color(0.95f, 0.20f, 0.10f), size = new Vector2(10, 10) }, // bright red
            new AreaPalette { idPrefix = "KTX",        color = new Color(0.95f, 0.20f, 0.85f), size = new Vector2(10, 10) }, // bright magenta
            new AreaPalette { idPrefix = "SanVanDong", color = new Color(1.00f, 0.90f, 0.00f), size = new Vector2(20, 12) }, // bright yellow
            new AreaPalette { idPrefix = "DonVeSinh",  color = new Color(1.00f, 0.55f, 0.00f), size = new Vector2(10, 7)  }, // bright orange-yellow
            new AreaPalette { idPrefix = "FreeArea",   color = new Color(1.00f, 1.00f, 1.00f), size = new Vector2(8, 8)   }, // bright white
        };

        [MenuItem("Tools/Build Game/Minimap/Build Markers (under each interactable)", false, 220)]
        public static void Build()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            // Shared materials per palette entry so dynamic batching collapses
            // all markers of the same type into one draw call. Without this,
            // 6 unique materials = 6 draw calls per minimap frame, which at
            // 0.2s throttle still racks up.
            var matCache = new Dictionary<string, Material>();
            Material GetMat(string key, Color c)
            {
                if (matCache.TryGetValue(key, out var existingMat) && existingMat != null) return existingMat;
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                var m = new Material(shader) { name = "MinimapMarker_" + key, color = c };
                // Boost emission so the marker stays visible on a darkened
                // minimap RT (URP exposure can dim them otherwise).
                if (m.HasProperty("_EmissionColor"))
                {
                    m.SetColor("_EmissionColor", c * 0.6f);
                    m.EnableKeyword("_EMISSION");
                }
                matCache[key] = m;
                return m;
            }

            int placed = 0;
            var markers = Object.FindObjectsByType<InteractableMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                if (marker == null || marker.interactable == null || marker.interactable.area == null) continue;
                var area = marker.interactable.area;
                var palette = ResolvePalette(area.id);

                var decal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                decal.name = $"MapDecal_{area.id}_{placed:D2}";
                decal.transform.SetParent(root.transform, false);
                Vector3 worldPos = marker.transform.position;
                // The "Ground" primitive is a Cube at y=0 with scale.y=1, so
                // its TOP surface sits at y=+0.5. A decal placed at y=0.06
                // ends up BURIED inside the ground cube and never renders.
                // Anchor at y=0.6 so the decal sits just above the ground
                // surface — visible from the minimap camera (look-down ortho
                // from y=60), invisible-ish from third-person eye level.
                const float DecalY = 0.6f;
                decal.transform.position = new Vector3(worldPos.x, DecalY, worldPos.z);
                decal.transform.localScale = new Vector3(palette.size.x, 0.05f, palette.size.y);
                decal.GetComponent<Renderer>().sharedMaterial = GetMat(palette.idPrefix, palette.color);
                // Decals should never block movement / interaction — they
                // sit at ground height and would interrupt the player's
                // OverlapSphere probe.
                var col = decal.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
                decal.isStatic = true;
                placed++;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MinimapMarkers] placed {placed} decals under {markers.Length} interactables (root: {RootName}).");
        }

        [MenuItem("Tools/Build Game/Minimap/Clear Markers", false, 221)]
        public static void Clear()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MinimapMarkers] cleared.");
        }

        static AreaPalette ResolvePalette(string areaId)
        {
            if (!string.IsNullOrEmpty(areaId))
            {
                for (int i = 0; i < Palette.Length; i++)
                    if (areaId.StartsWith(Palette[i].idPrefix)) return Palette[i];
            }
            // Unknown area -> neutral grey square, never crash on missing entry.
            return new AreaPalette { idPrefix = "Other", color = new Color(0.60f, 0.60f, 0.60f), size = new Vector2(4, 4) };
        }
    }
}

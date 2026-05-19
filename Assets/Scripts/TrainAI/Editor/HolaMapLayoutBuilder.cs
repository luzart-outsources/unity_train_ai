using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Performance-conscious campus layout for 10_World.
    //
    // Renderer budget: prior v7 had ~2,800 renderers (mostly from 6× Toa Hoc @ 296 ea
    // and 4× cang tin @ 141 ea). v8 cuts that ~60% by using fewer GLB instances and
    // sharing materials across all primitive polish so dynamic batching works.
    public static class HolaMapLayoutBuilder
    {
        const string RootName = "_HOLA_Layout";
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";

        const string GlbToaHoc  = "Assets/_Assets/Toa Hoc.glb";
        const string GlbKTX     = "Assets/_Assets/KTX.glb";
        const string GlbCangTin = "Assets/_Assets/cang tin.glb";

        // v9 model set — adds variety so the campus stops looking like 3
        // copies of the same building. All paths resolved via AssetDatabase
        // with explicit null guards in PlaceBuilding so missing files just
        // warn instead of crashing the build.
        const string GlbTruongHoc   = "Assets/_Assets/Truong Hoc.glb";
        const string GlbD2KTX       = "Assets/_Assets/D2_KTX.glb";
        const string GlbD5ToaHoc    = "Assets/_Assets/D5_Toa Hoc.glb";
        const string GlbCNhanAn     = "Assets/_Assets/C_NhanAn.glb";
        const string GlbECangTin    = "Assets/_Assets/E_cang tin.glb";
        const string GlbSVD         = "Assets/_Assets/SVĐ.glb";
        const string GlbDaiPhun     = "Assets/_Assets/Đài Phun nước ở giữa đường hình tròn.glb";
        const string FbxGreenhouse  = "Assets/_Assets/A_VuonHoa/source/Greenhouse.fbx";
        const string FbxParabolGate = "Assets/_Assets/Cong_Parabol/source/Parabol hust.fbx";
        const string FbxGardenGate  = "Assets/_Assets/Cong_Phu/source/Garden Gate.fbx";
        const string FbxD1School    = "Assets/_Assets/D1_KTX/source/school.fbx";

        const float MapWidth = 200f;
        const float MapDepth = 120f;

        struct Place { public string name; public string glb; public Vector3 pos; public Vector2 fp; public float yaw; public float maxH; }

        // === Shared materials (one instance per color reused everywhere) ===
        static Dictionary<string, Material> _matCache;
        static Material Mat(string key, Color c)
        {
            _matCache ??= new Dictionary<string, Material>();
            if (_matCache.TryGetValue(key, out var existing) && existing != null) return existing;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            // Note: enableInstancing left FALSE. With GPU instancing on + URP +
            // D3D12, large scenes were hitting command-buffer limits during
            // sustained camera rotation. Plain forward rendering is slower
            // but doesn't crash.
            var m = new Material(shader) { name = "HolaMat_" + key, color = c, enableInstancing = false };
            _matCache[key] = m;
            return m;
        }

        [MenuItem("Tools/Build Game/HOLA Map/Build Layout in 10_World", false, 200)]
        public static void BuildHolaLayout()
        {
            _matCache = new Dictionary<string, Material>();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            // Ground material reuse
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(MapWidth / 10f, 1f, MapDepth / 10f);
                var gr = ground.GetComponent<Renderer>();
                if (gr != null) gr.sharedMaterial = Mat("grass", new Color(0.55f, 0.78f, 0.42f));
                ground.isStatic = true;
            }

            // === Buildings (reduced count for perf) ===
            // Tổng 7 building (giảm từ 13). Toa Hoc 2× (vs 6×) là cú giảm renderer lớn nhất.
            var places = new[] {
                new Place{ name="Bld_ToaHoc_Main",  glb=GlbToaHoc,  pos=new Vector3(-50, 0,  18), fp=new Vector2(40,26), yaw=0,  maxH=12 },
                new Place{ name="Bld_ToaHoc_West",  glb=GlbToaHoc,  pos=new Vector3(-80, 0, -10), fp=new Vector2(26,18), yaw=0,  maxH=11 },
                new Place{ name="Bld_KTX_N",        glb=GlbKTX,     pos=new Vector3( 65, 0,  30), fp=new Vector2(24,18), yaw=0,  maxH=14 },
                new Place{ name="Bld_KTX_S",        glb=GlbKTX,     pos=new Vector3( 65, 0, -10), fp=new Vector2(24,18), yaw=0,  maxH=14 },
                new Place{ name="E_CangTin",        glb=GlbCangTin, pos=new Vector3(  8, 0,  12), fp=new Vector2(20,14), yaw=0,  maxH=8  },
                new Place{ name="C_NhaAn",          glb=GlbCangTin, pos=new Vector3( 30, 0, -22), fp=new Vector2(20,14), yaw=0,  maxH=8  },
            };
            foreach (var p in places) PlaceBuilding(p, root.transform);

            // === Areas (flat walkable cubes, no collider) ===
            PlaceArea("A_VuonHoa",          new Vector3(-88, 0.05f, 50), new Vector3(18, 0.3f, 14), Mat("garden",  new Color(0.50f, 0.78f, 0.42f)), root.transform);
            PlaceArea("B_SanChaoCo_Plaza",  new Vector3(-30, 0.05f, -8), new Vector3(35, 0.3f, 18), Mat("plaza",   new Color(0.70f, 0.68f, 0.62f)), root.transform);
            PlaceArea("P_Parking",          new Vector3(  0, 0.05f, 52), new Vector3(22, 0.3f, 14), Mat("asphalt", new Color(0.30f, 0.30f, 0.34f)), root.transform);
            PlacePitch(new Vector3(55, 0.05f, -42), new Vector3(60, 0.5f, 28), root.transform);

            // Reduced trash count
            var trashPos = new[] {
                new Vector3(-92, 0,  45),
                new Vector3(-15, 0, -50),
                new Vector3( 55, 0, -55),
                new Vector3( 88, 0,  10),
            };
            for (int i = 0; i < trashPos.Length; i++) PlaceTrash($"D_Trash_{i + 1:D2}", trashPos[i], root.transform);

            // === Gates + perimeter walls + paths ===
            PlaceGate("Gate_Main", new Vector3(98, 0, -8), 0f, root.transform);
            PlaceGate("Gate_Side", new Vector3(60, 0, 58), 0f, root.transform);
            BuildPerimeter(root.transform);
            BuildPaths(root.transform);

            // === Lighter polish layer (fewer trees + no labels, no fence forest) ===
            BuildPolish(root.transform);

            // Warmer afternoon directional light
            var dl = GameObject.Find("DirectionalLight");
            if (dl != null)
            {
                var lc = dl.GetComponent<Light>();
                if (lc != null)
                {
                    lc.color = new Color(1f, 0.95f, 0.84f);
                    lc.intensity = 1.2f;
                    dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            // DO NOT mark GLB-imported buildings as static — some Sketchfab
            // submeshes export with Lines/Points topology (debug wireframe
            // helpers), which Unity's static batcher chokes on with hundreds
            // of "Failed getting triangles. Submesh topology is lines or
            // points." errors per frame, eventually crashing the D3D12
            // driver. Only mark the primitive polish objects (cubes, cylinders)
            // static — they're plain triangles and batch safely.
            MarkPrimitivesStatic(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int totalRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v8] built — {places.Length} buildings, total renderers in _HOLA_Layout: {totalRenderers}");
        }

        // V11 placeholder mode: keeps ONLY GLB/FBX models ≤10 MB on disk
        // and replaces every heavy building (≥10 MB) with a labeled cube +
        // BoxCollider placeholder. Designed to fix the scene-load crashes
        // caused by 475-970 MB baked mesh assets blowing out RAM/VRAM on
        // mid-spec machines.
        //
        // Source-size classification:
        //   KEEP (≤10 MB source GLB/FBX):
        //     KTX.glb (6 MB), D2_KTX.glb (6 MB), C_NhanAn.glb (4 MB),
        //     D1_KTX/source/school.fbx (5 MB), Parabol hust.fbx (2 MB),
        //     Garden Gate.fbx (<1 MB)
        //   CUBE PLACEHOLDER (>10 MB source — would crash Unity at load):
        //     Greenhouse (41), cang tin (62×2), Đài Phun (66), SVĐ (80),
        //     Truong Hoc (119), D5_Toa Hoc (475), Toa Hoc (475)
        //
        // GameObject names follow the HOLA poster legend exactly (A, B, C1,
        // D1, D2, D3, D5, D7, D8, D, E1, E2, G, Fountain, SVD, CongChinh,
        // CongPhu) so designers can spot positions in the hierarchy without
        // cross-referencing.
        //
        // No bake step — cubes are already 1 mesh, kept GLBs are small.
        // Scene load should be <2s on SSD instead of 20-30s.
        [MenuItem("Tools/Build Game/HOLA Map/Build Layout V11 (placeholders, fast load)", false, 197)]
        public static void BuildHolaLayoutV11()
        {
            _matCache = new Dictionary<string, Material>();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            // Ground
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(MapWidth / 10f, 1f, 140f / 10f);
                var gr = ground.GetComponent<Renderer>();
                if (gr != null) gr.sharedMaterial = Mat("grass", new Color(0.55f, 0.78f, 0.42f));
                ground.isStatic = true;
            }

            // === Color palette for placeholders (visual coding) ===
            // Dorms = blue family, food = orange/brown, garden = green,
            // laundry = cyan, special = white. Designers can eyeball the
            // scene and immediately identify building types.
            var matDorm     = Mat("ph_dorm",    new Color(0.40f, 0.55f, 0.78f));
            var matCanteen  = Mat("ph_canteen", new Color(0.92f, 0.65f, 0.32f));
            var matDining   = Mat("ph_dining",  new Color(0.60f, 0.42f, 0.28f));
            var matGarden   = Mat("ph_garden",  new Color(0.35f, 0.68f, 0.40f));
            var matLaundry  = Mat("ph_laundry", new Color(0.55f, 0.78f, 0.85f));
            var matFountain = Mat("ph_fount",   new Color(0.30f, 0.55f, 0.85f));
            var matPitch    = Mat("ph_pitch",   new Color(0.28f, 0.62f, 0.32f));

            // === LIGHT models — keep as GLB/FBX instances ===
            // Pos values eyeballed from HOLA poster (same coords as V10).
            var places = new[]
            {
                new Place{ name="D1",  glb=FbxD1School, pos=new Vector3(-65, 0,  22), fp=new Vector2(26,18), yaw=0, maxH=14 },
                new Place{ name="D2",  glb=GlbD2KTX,    pos=new Vector3(-30, 0,  35), fp=new Vector2(22,16), yaw=0, maxH=14 },
                new Place{ name="D7",  glb=GlbKTX,      pos=new Vector3(-65, 0, -45), fp=new Vector2(22,16), yaw=0, maxH=13 },
                new Place{ name="D8",  glb=GlbD2KTX,    pos=new Vector3(-30, 0, -45), fp=new Vector2(22,16), yaw=0, maxH=13 },
                new Place{ name="D",   glb=GlbKTX,      pos=new Vector3(-42, 0, -58), fp=new Vector2(18,12), yaw=0, maxH=11 },
                new Place{ name="C1",  glb=GlbCNhanAn,  pos=new Vector3(-55, 0, -22), fp=new Vector2(28,18), yaw=0, maxH=10 },
            };
            foreach (var p in places) PlaceBuilding(p, root.transform);

            // === HEAVY model positions — cube placeholders ===
            // (label, x, z, fpX, fpZ, height, material)
            PlaceCubeLabeled("A",  new Vector3(-78, 0,  52), new Vector2(20, 12), 7f,  matGarden,  root.transform);
            PlaceCubeLabeled("G",  new Vector3(-32, 0,  15), new Vector2(14, 10), 6f,  matLaundry, root.transform);
            PlaceCubeLabeled("D3", new Vector3( 12, 0,  42), new Vector2(28, 18), 13f, matDorm,    root.transform);
            PlaceCubeLabeled("D5", new Vector3( 35, 0,  16), new Vector2(22, 16), 12f, matDorm,    root.transform);
            PlaceCubeLabeled("E1", new Vector3( 15, 0,   2), new Vector2(18, 12), 8f,  matCanteen, root.transform);
            PlaceCubeLabeled("E2", new Vector3( 18, 0, -20), new Vector2(18, 12), 8f,  matCanteen, root.transform);
            PlaceCubeLabeled("C2", new Vector3(-15, 0,  -3), new Vector2(16, 12), 9f,  matDining,  root.transform);

            // === Fountain placeholder (cylinder instead of 66 MB GLB) ===
            var fountain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fountain.name = "Fountain";
            fountain.transform.SetParent(root.transform, false);
            fountain.transform.position = new Vector3(0, 1f, -22);
            fountain.transform.localScale = new Vector3(6f, 1f, 6f);
            fountain.GetComponent<Renderer>().sharedMaterial = matFountain;
            // Cylinder primitive already has CapsuleCollider — that's fine for "player can't walk through"

            // === SVĐ placeholder (football pitch as flat green rect + center stripe) ===
            // SVĐ.glb is 80 MB; the pitch shape itself reads as football
            // perfectly with just colored primitives.
            var svdRoot = new GameObject("SVD");
            svdRoot.transform.SetParent(root.transform, false);
            svdRoot.transform.position = new Vector3(55, 0, -50);

            var pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "Pitch";
            pitch.transform.SetParent(svdRoot.transform, false);
            pitch.transform.localPosition = new Vector3(0, 0.3f, 0);
            pitch.transform.localScale = new Vector3(56, 0.6f, 28);
            pitch.GetComponent<Renderer>().sharedMaterial = matPitch;

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "CenterStripe";
            stripe.transform.SetParent(svdRoot.transform, false);
            stripe.transform.localPosition = new Vector3(0, 0.65f, 0);
            stripe.transform.localScale = new Vector3(0.4f, 0.05f, 28);
            stripe.GetComponent<Renderer>().sharedMaterial = Mat("white", Color.white);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());

            // === Gates — both FBX files are <2 MB, safe to keep ===
            PlaceLandmark("CongChinh", FbxParabolGate, new Vector3(85, 0, -28), new Vector2(11, 9), 7f, root.transform);
            PlaceLandmark("CongPhu",   FbxGardenGate, new Vector3(58, 0, 56), new Vector2(7, 6), 4f, root.transform);

            // === Areas (walkable markers) ===
            PlaceArea("B",         new Vector3(-25, 0.05f, -5),  new Vector3(28, 0.3f, 16), Mat("plaza",   new Color(0.70f, 0.68f, 0.62f)), root.transform);
            PlaceArea("P_Parking", new Vector3( 40, 0.05f,  42), new Vector3(18, 0.3f, 12), Mat("asphalt", new Color(0.30f, 0.30f, 0.34f)), root.transform);

            // === Trash pins ("D = Khu Vực Rác" on legend) ===
            var trashPins = new[] {
                new Vector3(-50, 0,  48),
                new Vector3( 22, 0,  35),
                new Vector3( 80, 0,   6),
                new Vector3(-15, 0, -38),
                new Vector3( 30, 0, -38),
            };
            for (int i = 0; i < trashPins.Length; i++)
                PlaceTrash($"D_TrashPin_{i + 1:D2}", trashPins[i], root.transform);

            BuildPerimeterV10(root.transform);
            BuildPathsV10(root.transform);
            BuildCircularRoad(root.transform);
            BuildPolish(root.transform);

            // Light setup
            var dl = GameObject.Find("DirectionalLight");
            if (dl != null)
            {
                var lc = dl.GetComponent<Light>();
                if (lc != null)
                {
                    lc.color = new Color(1f, 0.95f, 0.84f);
                    lc.intensity = 1.2f;
                    dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            MarkPrimitivesStatic(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int totalRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v11] placeholder mode built — {places.Length} light GLBs + 7 cube placeholders + SVD/fountain prims. " +
                      $"NO BAKE. Total renderers: {totalRenderers}");
        }

        // Cube placeholder: primitive Cube primitive (already has BoxCollider
        // by default), positioned by ground-aligned center so y=height/2.
        // Renderer uses the shared "ph_*" material so dynamic batching pools
        // every placeholder of the same type into one draw call.
        static void PlaceCubeLabeled(string label, Vector3 pos, Vector2 fp, float height, Material mat, Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = label;
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(pos.x, height / 2f, pos.z);
            cube.transform.localScale = new Vector3(fp.x, height, fp.y);
            cube.GetComponent<Renderer>().sharedMaterial = mat;
            // BoxCollider stays (Unity primitive cube ships with one) — that's the placeholder's gameplay role.
        }

        // V10 faithful: re-positions every placement to match the actual
        // HOLA MAP poster (top-left A=Vườn Hoa, fountain dead-center with a
        // circular road around it, SVĐ bottom-right corner, 7 dorms scattered
        // not gridded, G=Nhà Giặt added, 2× canteens + 2× nhà ăn, 5 trash
        // pins at the legend's pin coordinates instead of random spots).
        //
        // Coordinate derivation: HOLA poster's map content is ~430×360 px.
        // We map that to a 200×140 world rect, origin at center. Per-pixel
        // scale ≈ 0.47 world-units X, 0.39 Z (Z flipped because the image y
        // grows downward but Unity world z grows northward). Each Place's
        // pos was eyeballed off the poster then snapped to round values.
        //
        // Total placements: 16 buildings/structures + circular road ring
        // + 5 trash pins + perimeter walls + polish. Pre-bake renderer
        // count lands around ~2000; bake collapses to ~250 (same budget
        // as V9).
        [MenuItem("Tools/Build Game/HOLA Map/Build Layout V10 (HOLA-faithful + bake)", false, 198)]
        public static void BuildHolaLayoutV10()
        {
            _matCache = new Dictionary<string, Material>();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            // Ground — same as V8/V9 but slightly larger to fit 200×140 layout.
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(MapWidth / 10f, 1f, 140f / 10f);
                var gr = ground.GetComponent<Renderer>();
                if (gr != null) gr.sharedMaterial = Mat("grass", new Color(0.55f, 0.78f, 0.42f));
                ground.isStatic = true;
            }

            // === Buildings — exact HOLA MAP positions ===
            // Naming follows the legend (A/B/C/D1.../E/G + map pin labels)
            // so it's obvious from the hierarchy which placement is which.
            //
            // Note on dorm reuse: HOLA shows 7 dorm-like buildings (D1, D2,
            // D3, D5, D7, D8, plain D — D4/D6 missing on the poster). We have
            // 3 distinct dorm models (KTX, D2_KTX, D1_KTX/school.fbx) plus
            // D5_Toa Hoc (the red-cross study/medical building). The rest
            // reuse KTX.glb so all 7 slots fill — bake collapses each into
            // its own combined mesh.
            var places = new[]
            {
                // Top row (north side)
                new Place{ name="A_VuonHoa_Greenhouse", glb=FbxGreenhouse, pos=new Vector3(-78, 0,  52), fp=new Vector2(20,12), yaw=0, maxH=8  },
                new Place{ name="D1_KTX",               glb=FbxD1School,   pos=new Vector3(-65, 0,  22), fp=new Vector2(26,18), yaw=0, maxH=14 },
                new Place{ name="D2_KTX",               glb=GlbD2KTX,      pos=new Vector3(-30, 0,  35), fp=new Vector2(22,16), yaw=0, maxH=14 },
                new Place{ name="G_NhaGiat",            glb=GlbKTX,        pos=new Vector3(-32, 0,  15), fp=new Vector2(16,10), yaw=0, maxH=7  },
                new Place{ name="D3_KTX",               glb=GlbTruongHoc,  pos=new Vector3( 12, 0,  42), fp=new Vector2(28,18), yaw=0, maxH=13 },

                // Middle
                new Place{ name="D5_ToaHoc",            glb=GlbD5ToaHoc,   pos=new Vector3( 35, 0,  16), fp=new Vector2(22,16), yaw=0, maxH=12 },
                new Place{ name="E1_CangTin",           glb=GlbCangTin,    pos=new Vector3( 15, 0,   2), fp=new Vector2(18,12), yaw=0, maxH=8  },
                new Place{ name="E2_CangTinEast",       glb=GlbECangTin,   pos=new Vector3( 18, 0, -20), fp=new Vector2(18,12), yaw=0, maxH=8  },
                new Place{ name="C1_NhaAn_Big",         glb=GlbCNhanAn,    pos=new Vector3(-55, 0, -22), fp=new Vector2(28,18), yaw=0, maxH=10 },

                // Bottom row (south side)
                new Place{ name="D7_KTX",               glb=GlbKTX,        pos=new Vector3(-65, 0, -45), fp=new Vector2(22,16), yaw=0, maxH=13 },
                new Place{ name="D8_KTX",               glb=GlbD2KTX,      pos=new Vector3(-30, 0, -45), fp=new Vector2(22,16), yaw=0, maxH=13 },
                new Place{ name="D_KTX",                glb=GlbKTX,        pos=new Vector3(-42, 0, -58), fp=new Vector2(18,12), yaw=0, maxH=11 },
                new Place{ name="C2_NhaAn_Mid",         glb=GlbToaHoc,     pos=new Vector3(-15, 0,  -3), fp=new Vector2(16,12), yaw=0, maxH=9  },
            };
            foreach (var p in places) PlaceBuilding(p, root.transform);

            // === Centerpieces ===
            // Fountain dead-center at (0, 0, -20) — matches HOLA's circular
            // pond between B/E2/D7-D8. The circular road wraps it (below).
            PlaceLandmark("Landmark_DaiPhun_Center", GlbDaiPhun, new Vector3(0, 0, -22), new Vector2(8, 8), 4f, root.transform);

            // SVĐ bottom-right corner — the iconic football pitch from the
            // poster. Larger footprint than V9's (50×26 vs 48×24) to push it
            // into the corner properly.
            PlaceLandmark("Landmark_SVD_BottomRight", GlbSVD, new Vector3(55, 0, -50), new Vector2(56, 28), 6f, root.transform);

            // === Areas (walkable markers) ===
            // B = plaza (Sân Chào Cờ) center-left; A garden marker around
            // the Vườn Hoa Greenhouse; Parking top-right.
            PlaceArea("A_VuonHoa_Marker",  new Vector3(-78, 0.05f,  52), new Vector3(22, 0.3f, 14), Mat("garden",  new Color(0.50f, 0.78f, 0.42f)), root.transform);
            PlaceArea("B_SanChaoCo_Plaza", new Vector3(-25, 0.05f, -5),  new Vector3(28, 0.3f, 16), Mat("plaza",   new Color(0.70f, 0.68f, 0.62f)), root.transform);
            PlaceArea("P_Parking_TopRight",new Vector3( 40, 0.05f,  42), new Vector3(18, 0.3f, 12), Mat("asphalt", new Color(0.30f, 0.30f, 0.34f)), root.transform);

            // === Trash pins (5 exact HOLA pin coords) ===
            // The "D = Khu Vực Rác" markers on the poster are pins, not
            // dorm buildings. Quest "Don ve sinh" routes the player to one
            // of these — random placement (V8/V9) broke quest routing when
            // the player's current quest area didn't match any pin.
            var trashPins = new[] {
                new Vector3(-50, 0,  48),  // top-left corner pin
                new Vector3( 22, 0,  35),  // top-center pin
                new Vector3( 80, 0,   6),  // east-mid pin
                new Vector3(-15, 0, -38),  // bottom-mid pin
                new Vector3( 30, 0, -38),  // bottom-right-ish pin
            };
            for (int i = 0; i < trashPins.Length; i++)
                PlaceTrash($"D_TrashPin_{i + 1:D2}", trashPins[i], root.transform);

            // === Gates ===
            // Cổng Chính (main gate) = east side (legend pin "Cổng Chính" is
            // on the right of the poster). Parabol HUST FBX fits since it's
            // the actual HUST main gate model.
            PlaceLandmark("Gate_Main_Parabol_East", FbxParabolGate, new Vector3(85, 0, -28), new Vector2(11, 9), 7f, root.transform);
            // Cổng Phụ (side gate) = north (top of poster). Garden Gate FBX.
            PlaceLandmark("Gate_Side_Garden_North", FbxGardenGate, new Vector3(58, 0, 56), new Vector2(7, 6), 4f, root.transform);

            BuildPerimeterV10(root.transform);
            BuildPathsV10(root.transform);
            BuildCircularRoad(root.transform);
            BuildPolish(root.transform);

            // Warmer afternoon directional light (same as V9)
            var dl = GameObject.Find("DirectionalLight");
            if (dl != null)
            {
                var lc = dl.GetComponent<Light>();
                if (lc != null)
                {
                    lc.color = new Color(1f, 0.95f, 0.84f);
                    lc.intensity = 1.2f;
                    dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            MarkPrimitivesStatic(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int preBakeRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v10] pre-bake: {places.Length} buildings + 4 landmarks/gates + circular road, renderers={preBakeRenderers}");

            BakeBuildingsTool.Bake();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int postBakeRenderers = GameObject.Find(RootName).GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v10] post-bake: renderers={postBakeRenderers} (cut from {preBakeRenderers})");
        }

        // Circular road around the fountain at world center. 16 thin Cube
        // segments arranged in a ring, all sharing the "path" material so
        // dynamic batching gathers them automatically. Cheaper than a torus
        // mesh and doesn't need a custom shader.
        static void BuildCircularRoad(Transform parent)
        {
            var holder = new GameObject("CircularRoad_AroundFountain");
            holder.transform.SetParent(parent, false);
            var mat = Mat("path", new Color(0.78f, 0.74f, 0.62f));
            const int segments = 24;
            const float radius = 16f;       // ring radius around fountain center (0,0,-22)
            const float segWidth = 4f;      // road width
            const float segHeight = 0.12f;  // sits slightly above ground
            for (int i = 0; i < segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = -22f + Mathf.Sin(angle) * radius;
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seg.name = $"RoadSeg_{i:D2}";
                seg.transform.SetParent(holder.transform, false);
                seg.transform.position = new Vector3(x, segHeight / 2f, z);
                // Tangent-aligned: rotate so the long axis is along the arc.
                float tangentAngle = angle + Mathf.PI / 2f;
                seg.transform.rotation = Quaternion.Euler(0, -tangentAngle * Mathf.Rad2Deg, 0);
                // Arc segment length ≈ 2π·r / segments
                float segLen = (2f * Mathf.PI * radius) / segments + 0.2f; // overlap a bit
                seg.transform.localScale = new Vector3(segLen, segHeight, segWidth);
                seg.GetComponent<Renderer>().sharedMaterial = mat;
                var col = seg.GetComponent<Collider>();
                if (col != null) Object.DestroyImmediate(col);
            }
        }

        // Slightly larger perimeter for V10's 200×140 layout. Same wall mat
        // as V8/V9 so the bake batches them.
        static void BuildPerimeterV10(Transform parent)
        {
            var holder = new GameObject("Perimeter");
            holder.transform.SetParent(parent, false);
            var mat = Mat("wall", new Color(0.65f, 0.65f, 0.65f));
            float w = MapWidth, d = 140f, h = 3f, t = 1f;
            MakeWall("Wall_N",   new Vector3(0, h / 2f, d / 2f),  new Vector3(w, h, t), mat, holder.transform);
            MakeWall("Wall_S",   new Vector3(0, h / 2f, -d / 2f), new Vector3(w, h, t), mat, holder.transform);
            MakeWall("Wall_W",   new Vector3(-w / 2f, h / 2f, 0), new Vector3(t, h, d), mat, holder.transform);
            MakeWall("Wall_E_a", new Vector3(w / 2f, h / 2f, 30), new Vector3(t, h, 60), mat, holder.transform);
            MakeWall("Wall_E_b", new Vector3(w / 2f, h / 2f, -45),new Vector3(t, h, 40), mat, holder.transform);
        }

        // Paths: E-W avenue + N-S connector + diagonal to bottom-right SVĐ.
        static void BuildPathsV10(Transform parent)
        {
            var holder = new GameObject("Paths");
            holder.transform.SetParent(parent, false);
            var mat = Mat("path", new Color(0.78f, 0.74f, 0.62f));
            MakeFloor("Path_EW_North", new Vector3(0, 0.08f, 30), new Vector3(180, 0.1f, 4), mat, holder.transform);
            MakeFloor("Path_EW_Mid",   new Vector3(0, 0.08f, -2), new Vector3(180, 0.1f, 4), mat, holder.transform);
            MakeFloor("Path_NS_Spine", new Vector3(0, 0.08f,  0), new Vector3(4,   0.1f, 120), mat, holder.transform);
            // Diagonal-ish path toward SVĐ (just a rotated long strip).
            var diag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diag.name = "Path_DiagToSVD";
            diag.transform.SetParent(holder.transform, false);
            diag.transform.position = new Vector3(30, 0.08f, -30);
            diag.transform.rotation = Quaternion.Euler(0, 25, 0);
            diag.transform.localScale = new Vector3(50, 0.1f, 4);
            diag.GetComponent<Renderer>().sharedMaterial = mat;
            var c = diag.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        // V9 expanded campus: 9 buildings with model variety, plus SVD
        // stadium / fountain centerpiece / 2 FBX gates instead of primitive
        // cubes. Auto-bakes via BakeBuildingsTool at the end so the renderer
        // budget stays sane — without baking, the v9 buildings collectively
        // import as ~1500+ submesh renderers which trashes draw-call perf.
        [MenuItem("Tools/Build Game/HOLA Map/Build Layout V9 (varied GLBs + bake)", false, 199)]
        public static void BuildHolaLayoutV9()
        {
            _matCache = new Dictionary<string, Material>();

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(RootName);
            root.transform.position = Vector3.zero;

            // Ground material reuse (same as v8)
            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(MapWidth / 10f, 1f, MapDepth / 10f);
                var gr = ground.GetComponent<Renderer>();
                if (gr != null) gr.sharedMaterial = Mat("grass", new Color(0.55f, 0.78f, 0.42f));
                ground.isStatic = true;
            }

            // === Buildings — varied model set so the campus doesn't look duplicated ===
            //
            // North row (study + dorm side): 5 buildings, mix of Toa Hoc / KTX variants
            // South row (admin + canteen + greenhouse): 4 buildings
            //
            // Footprint targets are picked so each building visually fills its lot
            // without pushing into the central plaza or paths.
            var places = new[]
            {
                // North row
                new Place{ name="Bld_KTX_Main",     glb=GlbKTX,        pos=new Vector3(-78, 0,  35), fp=new Vector2(22,18), yaw=0, maxH=14 },
                new Place{ name="Bld_KTX_D2",       glb=GlbD2KTX,      pos=new Vector3(-48, 0,  35), fp=new Vector2(22,18), yaw=0, maxH=14 },
                new Place{ name="Bld_D1_School",    glb=FbxD1School,   pos=new Vector3(-12, 0,  38), fp=new Vector2(30,20), yaw=0, maxH=15 },
                new Place{ name="Bld_ToaHoc_Main",  glb=GlbToaHoc,     pos=new Vector3( 32, 0,  35), fp=new Vector2(24,18), yaw=0, maxH=12 },
                new Place{ name="Bld_ToaHoc_D5",    glb=GlbD5ToaHoc,   pos=new Vector3( 62, 0,  35), fp=new Vector2(24,18), yaw=0, maxH=12 },

                // South row
                new Place{ name="Bld_TruongHoc",    glb=GlbTruongHoc,  pos=new Vector3(-58, 0, -36), fp=new Vector2(34,20), yaw=0, maxH=14 },
                new Place{ name="Bld_NhaAn",        glb=GlbCNhanAn,    pos=new Vector3(-15, 0, -36), fp=new Vector2(20,16), yaw=0, maxH=9  },
                new Place{ name="Bld_CangTin_E",    glb=GlbECangTin,   pos=new Vector3( 18, 0, -36), fp=new Vector2(20,16), yaw=0, maxH=8  },
                new Place{ name="Bld_VuonHoa_GH",   glb=FbxGreenhouse, pos=new Vector3( 60, 0, -32), fp=new Vector2(16,12), yaw=0, maxH=8  },
            };
            foreach (var p in places) PlaceBuilding(p, root.transform);

            // === Centerpieces ===
            // Đài Phun nước (fountain) sits in the plaza middle as a landmark.
            PlaceLandmark("Landmark_DaiPhun", GlbDaiPhun, new Vector3(-30, 0, -8), new Vector2(8, 8), 4f, root.transform);

            // SVĐ — real stadium model replaces the primitive FootballPitch from v8.
            PlaceLandmark("Landmark_SVD", GlbSVD, new Vector3(38, 0, 0), new Vector2(48, 24), 6f, root.transform);

            // === Areas (walkable markers) — same as v8, kept for quest area-allowed checks ===
            PlaceArea("A_VuonHoa",         new Vector3(75, 0.05f, -25), new Vector3(14, 0.3f, 10), Mat("garden",  new Color(0.50f, 0.78f, 0.42f)), root.transform);
            PlaceArea("B_SanChaoCo_Plaza", new Vector3(-30, 0.05f, -8), new Vector3(35, 0.3f, 18), Mat("plaza",   new Color(0.70f, 0.68f, 0.62f)), root.transform);
            PlaceArea("P_Parking",         new Vector3(  0, 0.05f, 58), new Vector3(22, 0.3f, 12), Mat("asphalt", new Color(0.30f, 0.30f, 0.34f)), root.transform);

            // Trash bins (light primitives)
            var trashPos = new[] {
                new Vector3(-92, 0,  45),
                new Vector3(-15, 0, -50),
                new Vector3( 55, 0, -55),
                new Vector3( 88, 0,  10),
            };
            for (int i = 0; i < trashPos.Length; i++) PlaceTrash($"D_Trash_{i + 1:D2}", trashPos[i], root.transform);

            // === Gates ===
            // Cong_Parabol = the HUST parabolic main gate, on the east entrance.
            PlaceLandmark("Gate_Main_Parabol", FbxParabolGate, new Vector3(98, 0, -8), new Vector2(10, 8), 7f, root.transform);
            // Cong_Phu (garden gate) on the north side
            PlaceLandmark("Gate_Side_Garden", FbxGardenGate, new Vector3(60, 0, 58), new Vector2(6, 6), 4f, root.transform);

            BuildPerimeter(root.transform);
            BuildPaths(root.transform);
            BuildPolish(root.transform);

            // Warmer afternoon directional light (same as v8)
            var dl = GameObject.Find("DirectionalLight");
            if (dl != null)
            {
                var lc = dl.GetComponent<Light>();
                if (lc != null)
                {
                    lc.color = new Color(1f, 0.95f, 0.84f);
                    lc.intensity = 1.2f;
                    dl.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                }
            }

            // Static batching only for the primitives layer (see v8 comment).
            MarkPrimitivesStatic(root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int preBakeRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v9] pre-bake: {places.Length} buildings + 4 landmarks/gates, renderers={preBakeRenderers}");

            // Auto-bake — collapses each building's hundreds of submesh
            // renderers into a single MeshRenderer with material slots,
            // mirroring how an FBX-imported asset would look. This keeps
            // the runtime renderer budget on par with v8 (~1k) instead of
            // exploding to 3k+.
            BakeBuildingsTool.Bake();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int postBakeRenderers = GameObject.Find(RootName).GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[HOLA v9] post-bake: renderers={postBakeRenderers} (cut from {preBakeRenderers})");
        }

        // Lightweight landmark placer: instantiates a model (GLB or FBX),
        // strips bad-topology submeshes (Sketchfab Lines/Points), fits into
        // a footprint+height envelope, and drops a single BoxCollider. No
        // material override — landmark models keep their imported visuals.
        static void PlaceLandmark(string name, string assetPath, Vector3 pos, Vector2 fp, float maxH, Transform parent)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (src == null) { Debug.LogWarning("[HOLA v9] missing asset: " + assetPath); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            if (inst == null) { Debug.LogWarning("[HOLA v9] InstantiatePrefab returned null for " + assetPath); return; }
            inst.name = name;
            inst.transform.SetParent(parent, false);

            // Strip Lines/Points submeshes that crash the D3D12 batcher.
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh;
                if (m == null) continue;
                bool bad = false;
                for (int si = 0; si < m.subMeshCount; si++)
                {
                    var t = m.GetTopology(si);
                    if (t != MeshTopology.Triangles && t != MeshTopology.Quads) { bad = true; break; }
                }
                if (bad)
                {
                    var rend = mf.GetComponent<Renderer>();
                    if (rend != null) rend.enabled = false;
                }
            }
            inst.transform.localScale = Vector3.one;

            var rends = inst.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) { inst.transform.position = pos; return; }
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float sx = b.size.x > 0.001f ? fp.x / b.size.x : 1f;
            float sz = b.size.z > 0.001f ? fp.y / b.size.z : 1f;
            float sy = (maxH > 0f && b.size.y > 0.001f) ? maxH / b.size.y : float.MaxValue;
            float s = Mathf.Min(sx, sz, sy);
            inst.transform.localScale = new Vector3(s, s, s);

            rends = inst.GetComponentsInChildren<Renderer>(true);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float dy = -b.min.y;
            inst.transform.position = new Vector3(pos.x, dy, pos.z);

            foreach (var mc in inst.GetComponentsInChildren<MeshCollider>(true))
                Object.DestroyImmediate(mc);
            rends = inst.GetComponentsInChildren<Renderer>(true);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var box = inst.AddComponent<BoxCollider>();
            box.center = inst.transform.InverseTransformPoint(b.center);
            var locSize = inst.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(locSize.x), Mathf.Abs(locSize.y), Mathf.Abs(locSize.z));
        }

        [MenuItem("Tools/Build Game/HOLA Map/Clear Layout", false, 201)]
        public static void ClearLayout()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[HOLA] Layout cleared.");
        }

        static void PlaceBuilding(Place p, Transform parent)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(p.glb);
            if (src == null) { Debug.LogWarning("[HOLA] missing GLB: " + p.glb); return; }
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(src);
            inst.name = p.name;
            inst.transform.SetParent(parent, false);

            // Strip renderers whose mesh has any non-Triangle submesh (Lines /
            // Points from Sketchfab debug helpers). Leaving them in caused
            // hundreds of "Failed getting triangles. Submesh topology is lines
            // or points." asserts per frame and eventually crashed the D3D12
            // driver. Triangulated meshes pass through untouched.
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh;
                if (m == null) continue;
                bool bad = false;
                for (int si = 0; si < m.subMeshCount; si++)
                {
                    var t = m.GetTopology(si);
                    if (t != MeshTopology.Triangles && t != MeshTopology.Quads)
                    { bad = true; break; }
                }
                if (bad)
                {
                    var rend = mf.GetComponent<Renderer>();
                    if (rend != null) rend.enabled = false;
                }
            }
            inst.transform.localScale = Vector3.one;

            var rends = inst.GetComponentsInChildren<Renderer>(true);
            if (rends.Length == 0) { inst.transform.position = p.pos; return; }
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float sx = b.size.x > 0.001f ? p.fp.x / b.size.x : 1f;
            float sz = b.size.z > 0.001f ? p.fp.y / b.size.z : 1f;
            float sy = (p.maxH > 0f && b.size.y > 0.001f) ? p.maxH / b.size.y : float.MaxValue;
            float s = Mathf.Min(sx, sz, sy);
            inst.transform.localScale = new Vector3(s, s, s);

            rends = inst.GetComponentsInChildren<Renderer>(true);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float dy = -b.min.y;
            inst.transform.position = new Vector3(p.pos.x, dy, p.pos.z);
            inst.transform.rotation = Quaternion.Euler(
                inst.transform.rotation.eulerAngles.x,
                inst.transform.rotation.eulerAngles.y + p.yaw,
                inst.transform.rotation.eulerAngles.z);

            // Strip any expensive MeshColliders inside; replace with a single root BoxCollider.
            // Sketchfab GLBs sometimes include per-mesh MeshColliders which cost a lot.
            foreach (var mc in inst.GetComponentsInChildren<MeshCollider>(true))
                Object.DestroyImmediate(mc);
            rends = inst.GetComponentsInChildren<Renderer>(true);
            b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            var box = inst.AddComponent<BoxCollider>();
            box.center = inst.transform.InverseTransformPoint(b.center);
            var locSize = inst.transform.InverseTransformVector(b.size);
            box.size = new Vector3(Mathf.Abs(locSize.x), Mathf.Abs(locSize.y), Mathf.Abs(locSize.z));
        }

        static void PlaceArea(string name, Vector3 pos, Vector3 size, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        static void PlaceTrash(string name, Vector3 pos, Transform parent)
        {
            var bin = new GameObject(name);
            bin.transform.SetParent(parent, false);
            bin.transform.position = new Vector3(pos.x, 0, pos.z);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "Body"; body.transform.SetParent(bin.transform, false);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            body.GetComponent<Renderer>().sharedMaterial = Mat("binBody", new Color(0.18f, 0.36f, 0.55f));
            var lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "Lid"; lid.transform.SetParent(bin.transform, false);
            lid.transform.localPosition = new Vector3(0, 1.25f, 0);
            lid.transform.localScale = new Vector3(0.75f, 0.05f, 0.75f);
            lid.GetComponent<Renderer>().sharedMaterial = Mat("binLid", new Color(0.10f, 0.22f, 0.40f));
        }

        static void PlacePitch(Vector3 center, Vector3 size, Transform parent)
        {
            var pitch = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pitch.name = "FootballPitch";
            pitch.transform.SetParent(parent, false);
            pitch.transform.position = center;
            pitch.transform.localScale = size;
            pitch.GetComponent<Renderer>().sharedMaterial = Mat("pitch", new Color(0.30f, 0.65f, 0.30f));
            Object.DestroyImmediate(pitch.GetComponent<Collider>());

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.name = "Pitch_CenterLine";
            stripe.transform.SetParent(pitch.transform, true);
            stripe.transform.localScale = new Vector3(0.4f / size.x, 1.1f, 1f);
            stripe.transform.localPosition = new Vector3(0, 0.5f, 0);
            stripe.GetComponent<Renderer>().sharedMaterial = Mat("white", Color.white);
            Object.DestroyImmediate(stripe.GetComponent<Collider>());
        }

        static void PlaceGate(string name, Vector3 pos, float yaw, Transform parent)
        {
            var gate = new GameObject(name);
            gate.transform.SetParent(parent, false);
            gate.transform.position = pos;
            gate.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var matGate = Mat("gate", new Color(0.85f, 0.75f, 0.30f));
            CreateChildPrimitive(gate.transform, "PillarL", new Vector3(-3, 3, 0), new Vector3(1, 6, 1), matGate);
            CreateChildPrimitive(gate.transform, "PillarR", new Vector3(3, 3, 0),  new Vector3(1, 6, 1), matGate);
            CreateChildPrimitive(gate.transform, "Lintel",  new Vector3(0, 6.5f, 0), new Vector3(7.5f, 1, 1.2f), matGate);
        }

        static GameObject CreateChildPrimitive(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat, PrimitiveType type = PrimitiveType.Cube, bool keepCollider = true)
        {
            var p = GameObject.CreatePrimitive(type);
            p.name = name;
            p.transform.SetParent(parent, false);
            p.transform.localPosition = localPos;
            p.transform.localScale = scale;
            p.GetComponent<Renderer>().sharedMaterial = mat;
            if (!keepCollider) { var c = p.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
            return p;
        }

        static void BuildPerimeter(Transform parent)
        {
            var holder = new GameObject("Perimeter");
            holder.transform.SetParent(parent, false);
            var mat = Mat("wall", new Color(0.65f, 0.65f, 0.65f));
            float w = MapWidth, d = MapDepth, h = 3f, t = 1f;
            MakeWall("Wall_N",   new Vector3(0, h / 2f, d / 2f),  new Vector3(w, h, t), mat, holder.transform);
            MakeWall("Wall_S",   new Vector3(0, h / 2f, -d / 2f), new Vector3(w, h, t), mat, holder.transform);
            MakeWall("Wall_W",   new Vector3(-w / 2f, h / 2f, 0), new Vector3(t, h, d), mat, holder.transform);
            MakeWall("Wall_E_a", new Vector3(w / 2f, h / 2f, 30), new Vector3(t, h, 60), mat, holder.transform);
            MakeWall("Wall_E_b", new Vector3(w / 2f, h / 2f, -40),new Vector3(t, h, 40), mat, holder.transform);
        }

        static GameObject MakeWall(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void BuildPaths(Transform parent)
        {
            var holder = new GameObject("Paths");
            holder.transform.SetParent(parent, false);
            var mat = Mat("path", new Color(0.78f, 0.74f, 0.62f));
            MakeFloor("Path_EW", new Vector3(0, 0.08f, -15), new Vector3(180, 0.1f, 5), mat, holder.transform);
            MakeFloor("Path_NS", new Vector3(0, 0.08f,   0), new Vector3(5,  0.1f, 100), mat, holder.transform);
        }

        static void MakeFloor(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            var c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        // === Polish (kept light for perf) ===
        static void BuildPolish(Transform parent)
        {
            var holder = new GameObject("Polish");
            holder.transform.SetParent(parent, false);

            var poleMat   = Mat("pole",  new Color(0.78f, 0.78f, 0.78f));
            var flagMat   = Mat("flag",  new Color(0.85f, 0.10f, 0.10f));
            var trunkMat  = Mat("trunk", new Color(0.40f, 0.27f, 0.18f));
            var leavesMat = Mat("leaves",new Color(0.20f, 0.58f, 0.24f));
            var benchMat  = Mat("bench", new Color(0.45f, 0.30f, 0.20f));

            // Flag pole only — single landmark at plaza
            CreateChildPrimitive(holder.transform, "FlagPole", new Vector3(-30, 6, -8), new Vector3(0.3f, 6f, 0.3f), poleMat, PrimitiveType.Cylinder);
            CreateChildPrimitive(holder.transform, "Flag",     new Vector3(-28.5f, 11, -8), new Vector3(3f, 2f, 0.05f), flagMat, PrimitiveType.Cube, keepCollider: false);

            // Trees scattered (reduced to 8)
            var trees = new[] {
                new Vector3(-85, 0,  15),
                new Vector3(-85, 0, -10),
                new Vector3(-15, 0,  45),
                new Vector3( 15, 0,  45),
                new Vector3( 88, 0,  20),
                new Vector3( 88, 0, -30),
                new Vector3(-30, 0, -45),
                new Vector3(-50, 0,  50),
            };
            foreach (var tp in trees) MakeTree(tp, holder.transform, trunkMat, leavesMat);

            // Benches on plaza (kept — they're cheap & shared mat)
            MakeBench(new Vector3(-42, 0, -2),  holder.transform, benchMat);
            MakeBench(new Vector3(-18, 0, -2),  holder.transform, benchMat);
            MakeBench(new Vector3(-42, 0, -14), holder.transform, benchMat);
            MakeBench(new Vector3(-18, 0, -14), holder.transform, benchMat);

            // Reduced street lights (5 instead of 8)
            for (int x = -80; x <= 80; x += 40)
                MakeStreetLight(new Vector3(x, 0, -18), holder.transform, poleMat);
        }

        static void MakeTree(Vector3 pos, Transform parent, Material trunk, Material leaves)
        {
            var t = new GameObject("Tree");
            t.transform.SetParent(parent, false);
            t.transform.position = pos;
            CreateChildPrimitive(t.transform, "Trunk", new Vector3(0, 1.5f, 0), new Vector3(0.4f, 1.5f, 0.4f), trunk, PrimitiveType.Cylinder);
            CreateChildPrimitive(t.transform, "Crown", new Vector3(0, 3.5f, 0), new Vector3(2.5f, 2.5f, 2.5f), leaves, PrimitiveType.Sphere);
        }

        static void MakeBench(Vector3 pos, Transform parent, Material mat)
        {
            var b = new GameObject("Bench");
            b.transform.SetParent(parent, false);
            b.transform.position = pos;
            CreateChildPrimitive(b.transform, "Seat", new Vector3(0, 0.5f, 0),       new Vector3(2.5f, 0.15f, 0.6f), mat);
            CreateChildPrimitive(b.transform, "Back", new Vector3(0, 0.9f, -0.27f),  new Vector3(2.5f, 0.6f,  0.08f), mat);
        }

        static void MakeStreetLight(Vector3 pos, Transform parent, Material poleMat)
        {
            var sl = new GameObject("StreetLight");
            sl.transform.SetParent(parent, false);
            sl.transform.position = pos;
            CreateChildPrimitive(sl.transform, "Stem", new Vector3(0, 2.5f, 0), new Vector3(0.15f, 2.5f, 0.15f), poleMat, PrimitiveType.Cylinder);
            CreateChildPrimitive(sl.transform, "Bulb", new Vector3(0, 5.2f, 0), new Vector3(0.5f, 0.5f, 0.5f), Mat("bulb", new Color(1f, 0.95f, 0.6f)), PrimitiveType.Sphere);
        }

        static void MarkPrimitivesStatic(Transform t)
        {
            // Walks the hierarchy and marks only nodes that are NOT part of a
            // GLB import (i.e. nodes whose MeshFilter mesh wasn't sourced from
            // a .glb asset). Cube/cylinder primitives we created live inside
            // the layout root and have built-in Unity meshes — those are safe
            // to mark static. Anything inside an imported GLB prefab instance
            // is left non-static so the static batcher doesn't try to merge
            // its (potentially Lines/Points) submeshes.
            if (IsGlbInstance(t)) return; // skip the entire GLB subtree
            t.gameObject.isStatic = true;
            for (int i = 0; i < t.childCount; i++) MarkPrimitivesStatic(t.GetChild(i));
        }

        static bool IsGlbInstance(Transform t)
        {
            // PrefabUtility marks instantiated prefab roots; we use that as a
            // cheap proxy for "this is an imported model". For our layout, the
            // only prefab-instance children of _HOLA_Layout are GLBs.
            var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
            if (src == null) return false;
            string p = UnityEditor.AssetDatabase.GetAssetPath(src);
            return !string.IsNullOrEmpty(p) && (p.EndsWith(".glb") || p.EndsWith(".gltf"));
        }
    }
}

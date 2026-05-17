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
            var m = new Material(shader) { name = "HolaMat_" + key, color = c, enableInstancing = true };
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

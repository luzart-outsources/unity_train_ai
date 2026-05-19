using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Walks every imported GLB building instance under _HOLA_Layout, gathers
    // all of its child MeshFilters, and merges them into a single Mesh on a
    // single MeshRenderer per building. The visual ends up identical (or
    // very close) to the original Sketchfab model, but the draw-call cost
    // collapses from "one renderer per submesh" (~150–300 per building) to
    // "one renderer with N material slots per building" — typically 5–20.
    //
    // This is the equivalent of importing an FBX with the "Combine Meshes"
    // option enabled, but applied at edit time to GLBs that glTFast already
    // unpacked into a hierarchy.
    //
    // Trade-off vs. SimpleBuildingsMode: keeps the proper architecture
    // appearance but uses more GPU than plain cubes. Trade-off vs. the raw
    // GLB layout: loses per-piece culling (a building is one renderer now,
    // either fully visible or fully culled) and slightly bigger memory
    // footprint per combined Mesh.
    public static class BakeBuildingsTool
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";
        const string RootName = "_HOLA_Layout";
        const string BakedFolder = "Assets/_Data/BakedMeshes";

        [MenuItem("Tools/Build Game/HOLA Map/Bake GLB Buildings into Combined Meshes", false, 234)]
        public static void Bake()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("[BakeBuildings] _HOLA_Layout missing — run Build Layout first."); return; }

            if (!AssetDatabase.IsValidFolder(BakedFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Data", "BakedMeshes");
            }

            var toBake = new List<GameObject>();
            foreach (Transform child in root.transform)
                if (IsGlbBuilding(child.gameObject)) toBake.Add(child.gameObject);

            int beforeRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            foreach (var go in toBake) BakeOne(go, root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            int afterRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[BakeBuildings] baked {toBake.Count} buildings; renderers {beforeRenderers} -> {afterRenderers}");
        }

        static bool IsGlbBuilding(GameObject go)
        {
            var src = PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src == null) return false;
            string path = AssetDatabase.GetAssetPath(src);
            return !string.IsNullOrEmpty(path) && (path.EndsWith(".glb") || path.EndsWith(".gltf"));
        }

        static void BakeOne(GameObject original, Transform parent)
        {
            string name = original.name;
            Vector3 worldPos = original.transform.position;
            Quaternion worldRot = original.transform.rotation;
            Vector3 worldScale = original.transform.lossyScale;

            // Collect every renderable mesh under this building, grouped by material.
            // We keep one CombineInstance list per unique material so the final mesh
            // has one submesh per material — exactly how Unity wants to dispatch
            // material draws.
            var byMaterial = new Dictionary<Material, List<CombineInstance>>();
            var mfs = original.GetComponentsInChildren<MeshFilter>(true);
            // Pre-compute the building's world-to-local so combined verts stay in
            // local space (important for culling + transform updates later).
            Matrix4x4 worldToLocal = original.transform.worldToLocalMatrix;
            int srcRendererCount = 0;
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh == null) continue;
                var rend = mf.GetComponent<Renderer>();
                if (rend == null || !rend.enabled) continue;
                srcRendererCount++;

                // Skip submeshes that aren't Triangles/Quads (Sketchfab debug helpers).
                var mesh = mf.sharedMesh;
                int subCount = mesh.subMeshCount;
                var mats = rend.sharedMaterials;
                for (int si = 0; si < subCount; si++)
                {
                    var topo = mesh.GetTopology(si);
                    if (topo != MeshTopology.Triangles && topo != MeshTopology.Quads) continue;
                    var mat = si < mats.Length ? mats[si] : (mats.Length > 0 ? mats[0] : null);
                    if (mat == null) continue;
                    if (!byMaterial.TryGetValue(mat, out var list))
                    {
                        list = new List<CombineInstance>();
                        byMaterial[mat] = list;
                    }
                    list.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = si,
                        transform = worldToLocal * mf.transform.localToWorldMatrix
                    });
                }
            }
            if (byMaterial.Count == 0) { Debug.LogWarning($"[BakeBuildings] {name}: nothing to bake"); return; }

            // Build one CombineInstance[] per material, merge each into a single
            // mesh, then CombineMeshes(false) at the end to keep submeshes separate.
            var finalCombine = new List<CombineInstance>();
            var orderedMats = new List<Material>();
            foreach (var kv in byMaterial)
            {
                var perMat = kv.Value.ToArray();
                var partMesh = new Mesh();
                partMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                partMesh.CombineMeshes(perMat, true /* one submesh */, true);
                finalCombine.Add(new CombineInstance { mesh = partMesh, subMeshIndex = 0, transform = Matrix4x4.identity });
                orderedMats.Add(kv.Key);
            }
            var finalMesh = new Mesh { name = name + "_Combined" };
            finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            finalMesh.CombineMeshes(finalCombine.ToArray(), false /* keep submeshes */, false);
            finalMesh.RecalculateBounds();

            // Reorder index and vertex buffers for GPU vertex-cache locality.
            // Free perf win at bake time — the actual draw cost on baked
            // meshes drops a few percent because the post-T&L cache sees
            // better triangle ordering. Safe to call once before persist:
            // it mutates the mesh in place, doesn't change rendered output.
            finalMesh.Optimize();

            // Persist mesh so the scene reference survives reloads.
            string meshPath = $"{BakedFolder}/{name}.asset";
            AssetDatabase.CreateAsset(finalMesh, meshPath);

            // Spawn replacement: one GameObject with the combined mesh + materials.
            var replacement = new GameObject(name);
            replacement.transform.SetParent(parent, false);
            replacement.transform.position = worldPos;
            replacement.transform.rotation = worldRot;
            replacement.transform.localScale = worldScale;
            var mfNew = replacement.AddComponent<MeshFilter>();
            mfNew.sharedMesh = finalMesh;
            var mrNew = replacement.AddComponent<MeshRenderer>();
            mrNew.sharedMaterials = orderedMats.ToArray();

            // Keep a BoxCollider for player physics.
            var box = replacement.AddComponent<BoxCollider>();
            box.center = finalMesh.bounds.center;
            box.size = finalMesh.bounds.size;

            replacement.isStatic = true;

            Object.DestroyImmediate(original);
            Debug.Log($"[BakeBuildings] {name}: collapsed {srcRendererCount} renderers -> 1 (with {orderedMats.Count} material slots)");
        }
    }
}

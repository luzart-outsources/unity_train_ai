using TrainAI.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Nuclear-option visual cut. The Sketchfab GLBs (Toa Hoc, KTX, cang tin)
    // total ~960 renderers in _HOLA_Layout, which has been overwhelming the
    // user's GPU even with all the other minimal-mode mitigations. Replace
    // every GLB building instance in _HOLA_Layout with a single sized cube
    // that matches the GLB's bounds. Drops the building renderer count from
    // ~960 to 6.
    //
    // Visuals are obviously much worse (just colored blocks), but the
    // gameplay layer doesn't depend on the GLB internals — only the Area
    // markers and Building doors. So the quest flow, interact zones, etc.
    // stay intact and the world is GPU-survivable on weaker cards.
    public static class SimpleBuildingsMode
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";
        const string RootName = "_HOLA_Layout";

        // Per-building tint to keep the layout readable at a glance.
        static readonly System.Collections.Generic.Dictionary<string, Color> Tints = new()
        {
            { "Bld_ToaHoc",   new Color(0.85f, 0.45f, 0.35f) }, // brick red
            { "Bld_KTX",      new Color(0.70f, 0.72f, 0.78f) }, // dorm gray
            { "E_CangTin",    new Color(0.95f, 0.75f, 0.35f) }, // canteen orange
            { "C_NhaAn",      new Color(0.55f, 0.45f, 0.95f) }, // dining purple
        };

        [MenuItem("Tools/Build Game/HOLA Map/Replace GLBs with Cubes (perf test)", false, 232)]
        public static void Replace()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var root = GameObject.Find(RootName);
            if (root == null) { Debug.LogWarning("[SimpleBuildings] _HOLA_Layout missing"); return; }

            // Collect first (can't modify hierarchy while iterating)
            var toReplace = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in root.transform)
            {
                if (IsGlbBuilding(child.gameObject)) toReplace.Add(child.gameObject);
            }
            foreach (var go in toReplace) ReplaceWithCube(go, root.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            int totalRenderers = root.GetComponentsInChildren<Renderer>(true).Length;
            Debug.Log($"[SimpleBuildings] replaced {toReplace.Count} GLB buildings with cubes; total renderers now: {totalRenderers}");
        }

        static bool IsGlbBuilding(GameObject go)
        {
            // Identify GLB instances by prefab source path (gltFast imports .glb).
            var src = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(go);
            if (src == null) return false;
            string path = AssetDatabase.GetAssetPath(src);
            return !string.IsNullOrEmpty(path) && (path.EndsWith(".glb") || path.EndsWith(".gltf"));
        }

        static void ReplaceWithCube(GameObject original, Transform parent)
        {
            // Capture the world-space bounds of the original so the cube matches footprint.
            var rends = original.GetComponentsInChildren<Renderer>(true);
            Bounds b;
            if (rends.Length > 0)
            {
                b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            }
            else
            {
                b = new Bounds(original.transform.position, Vector3.one * 5f);
            }

            string name = original.name;
            float rotY = original.transform.rotation.eulerAngles.y;

            // Spawn the cube
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = new Vector3(b.center.x, b.size.y * 0.5f, b.center.z);
            cube.transform.localScale = new Vector3(b.size.x, b.size.y, b.size.z);
            cube.transform.rotation = Quaternion.Euler(0, rotY, 0);

            // Tint from name prefix
            Color tint = new Color(0.7f, 0.7f, 0.7f);
            foreach (var kv in Tints) if (name.StartsWith(kv.Key)) { tint = kv.Value; break; }
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = tint, name = "SimpleBld_" + name };
            cube.GetComponent<Renderer>().sharedMaterial = mat;

            cube.isStatic = true;

            Object.DestroyImmediate(original);
        }

        [MenuItem("Tools/Build Game/HOLA Map/Restore GLB Buildings", false, 233)]
        public static void Restore()
        {
            // Easy path: just rebuild via HolaMapLayoutBuilder, which destroys
            // and recreates _HOLA_Layout from scratch using the GLB prefabs.
            HolaMapLayoutBuilder.BuildHolaLayout();
        }
    }
}

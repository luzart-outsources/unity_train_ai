using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Strips heavy visuals from 10_World so the player can test gameplay
    // without the GPU dying. The user reported repeated D3D12 device-removed
    // crashes when reaching the first quest, even after Sentis ONNX models
    // were detached. The remaining heavy hitters are:
    //   - the AdvancedPeopleSystem2 rigged character (lots of
    //     SkinnedMeshRenderers + per-frame compute skinning)
    //   - the 3 NPCs in 10_World (each also skinned)
    //
    // This tool flips both off in the scene without destroying the assets,
    // so we can re-enable later via the companion menu.
    public static class MinimalVisualMode
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";
        const string PlayerPrefabPath = "Assets/Prefabs/TrainAI/Player.prefab";
        const string FallbackVisual = "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_FarmBoy_01.prefab";

        [MenuItem("Tools/Build Game/HOLA Map/Minimal Visuals (test mode)", false, 230)]
        public static void Enable()
        {
            // 1) Revert Player.prefab visual to PolygonFarm static mesh
            PlayerVisualSwapper.RevertVisual();

            // 2) Disable NPCs + MinimapCamera in 10_World
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            int disabled = 0;
            foreach (var go in scene.GetRootGameObjects())
                disabled += DisableMatching(go, name => name.StartsWith("NPC_"));

            var mini = GameObject.Find("MinimapCamera");
            if (mini != null) { mini.SetActive(false); disabled++; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MinimalVisuals] enabled: Player reverted to static visual, disabled {disabled} heavy objects (NPC_* + MinimapCamera).");
        }

        [MenuItem("Tools/Build Game/HOLA Map/Restore Full Visuals", false, 231)]
        public static void Disable()
        {
            // 1) Swap Player back to AdvancedPeopleSystem2 + Animator
            PlayerVisualSwapper.SwapVisual();

            // 2) Re-enable NPCs + Minimap
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            int enabled = 0;
            foreach (var go in scene.GetRootGameObjects())
                enabled += EnableMatching(go, name => name.StartsWith("NPC_"));

            // Use FindObjectsByType so we catch inactive too
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "MinimapCamera" && !t.gameObject.activeSelf) { t.gameObject.SetActive(true); enabled++; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[MinimalVisuals] restored: Player back to rigged char, re-enabled {enabled} objects.");
        }

        static int DisableMatching(GameObject root, System.Func<string, bool> predicate)
        {
            int count = 0;
            if (predicate(root.name) && root.activeSelf) { root.SetActive(false); count++; }
            foreach (Transform c in root.transform) count += DisableMatching(c.gameObject, predicate);
            return count;
        }

        static int EnableMatching(GameObject root, System.Func<string, bool> predicate)
        {
            int count = 0;
            if (predicate(root.name) && !root.activeSelf) { root.SetActive(true); count++; }
            foreach (Transform c in root.transform) count += EnableMatching(c.gameObject, predicate);
            return count;
        }
    }
}

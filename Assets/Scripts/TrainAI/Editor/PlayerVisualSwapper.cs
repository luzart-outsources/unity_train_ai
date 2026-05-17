using TrainAI.Presentation;
using UnityEditor;
using UnityEngine;

namespace TrainAI.Editor
{
    // One-click swap: replaces the legacy PolygonFarm static visual on
    // Assets/Prefabs/TrainAI/Player.prefab with the rigged
    // AdvancedPeopleSystem2 CustomizableCharacter so the player has an
    // Animator + walk animations instead of a T-pose static mesh.
    //
    // Idempotent: re-running removes any existing "Visual" child first.
    public static class PlayerVisualSwapper
    {
        const string PlayerPrefabPath = "Assets/Prefabs/TrainAI/Player.prefab";
        const string AdvancedCharPath = "Assets/AdvancedPeopleSystem2/Prefabs/CustomizableCharacter.prefab";
        const string MaleFbxPath      = "Assets/AdvancedPeopleSystem2/Models/CivilMale/male.fbx";
        const string MaleAnimCtrl     = "Assets/AdvancedPeopleSystem2/Models/CivilMale/MaleAnimator.controller";

        [MenuItem("Tools/Build Game/HOLA Map/Swap Player Visual (AdvancedPeopleSystem2)", false, 210)]
        public static void SwapVisual()
        {
            var playerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerSrc == null) { Debug.LogError($"[PlayerVisualSwap] Player prefab missing: {PlayerPrefabPath}"); return; }

            var maleFbx = AssetDatabase.LoadAssetAtPath<GameObject>(MaleFbxPath);
            var animCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MaleAnimCtrl);
            if (maleFbx == null) { Debug.LogError($"[PlayerVisualSwap] Male FBX missing: {MaleFbxPath}"); return; }
            if (animCtrl == null) Debug.LogWarning($"[PlayerVisualSwap] AnimatorController missing at {MaleAnimCtrl} — Animator will be unconfigured.");

            // Edit the prefab contents in-place
            string path = AssetDatabase.GetAssetPath(playerSrc);
            var content = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Drop existing Visual
                var existing = content.transform.Find("Visual");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);

                // Instantiate the rigged male character as Visual
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(maleFbx, content.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;

                // Wire the Animator
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                if (animCtrl != null) animator.runtimeAnimatorController = animCtrl;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

                // Strip any colliders left on the visual (Player root has the
                // CharacterController; we don't want visual-mesh colliders
                // fighting it).
                foreach (var col in visual.GetComponentsInChildren<Collider>(true))
                    Object.DestroyImmediate(col);

                // Make sure root has the AnimatorDriver
                if (content.GetComponent<AnimatorDriver>() == null)
                    content.AddComponent<AnimatorDriver>();

                PrefabUtility.SaveAsPrefabAsset(content, path);
                Debug.Log("[PlayerVisualSwap] Player.prefab visual swapped to AdvancedPeopleSystem2 male + Animator wired.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(content);
            }
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Build Game/HOLA Map/Revert Player Visual to PolygonFarm", false, 211)]
        public static void RevertVisual()
        {
            const string polyPath = "Assets/Imported Asset/PolygonFarm/Prefabs/Characters/SM_Chr_FarmBoy_01.prefab";
            var poly = AssetDatabase.LoadAssetAtPath<GameObject>(polyPath);
            if (poly == null) { Debug.LogError($"[PlayerVisualSwap] PolygonFarm fallback missing: {polyPath}"); return; }

            var playerSrc = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerSrc == null) return;
            string path = AssetDatabase.GetAssetPath(playerSrc);
            var content = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var existing = content.transform.Find("Visual");
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(poly, content.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                foreach (var c in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
                // Remove AnimatorDriver if any
                var ad = content.GetComponent<AnimatorDriver>();
                if (ad != null) Object.DestroyImmediate(ad);
                PrefabUtility.SaveAsPrefabAsset(content, path);
                Debug.Log("[PlayerVisualSwap] reverted to PolygonFarm static visual.");
            }
            finally { PrefabUtility.UnloadPrefabContents(content); }
            AssetDatabase.Refresh();
        }
    }
}

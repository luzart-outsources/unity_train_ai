using TrainAI.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // One-shot attach of the MinimapThrottle component to the MinimapCamera
    // in 10_World. Idempotent: re-running is safe.
    //
    // Reason this is a separate menu entry: SceneBuilder.cs creates the
    // MinimapCamera but doesn't know about MinimapThrottle (added later to
    // fix a GPU command-list overflow crash). Rather than modify the legacy
    // SceneBuilder flow, we patch the scene with this tool.
    public static class MinimapThrottleSetup
    {
        const string WorldScenePath = "Assets/Scenes/TrainAI/10_World.unity";

        [MenuItem("Tools/Build Game/HOLA Map/Attach MinimapThrottle", false, 220)]
        public static void Attach()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != WorldScenePath)
                scene = EditorSceneManager.OpenScene(WorldScenePath, OpenSceneMode.Single);

            var cam = GameObject.Find("MinimapCamera");
            if (cam == null)
            {
                Debug.LogWarning("[MinimapThrottleSetup] MinimapCamera not found in 10_World — nothing to attach.");
                return;
            }
            if (cam.GetComponent<MinimapThrottle>() != null)
            {
                Debug.Log("[MinimapThrottleSetup] MinimapThrottle already attached, skipping.");
                return;
            }
            cam.AddComponent<MinimapThrottle>();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[MinimapThrottleSetup] attached MinimapThrottle to MinimapCamera.");
        }
    }
}

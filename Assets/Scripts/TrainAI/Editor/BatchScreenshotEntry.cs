using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Headless screenshot grabber for batch mode. Loads 10_World, places an
    // aerial camera, renders a single frame to PNG, exits. No play mode needed
    // — useful for visual verification when the MCP bridge is unavailable.
    //
    // Usage:
    //   Unity.exe -batchmode -projectPath . -executeMethod TrainAI.Editor.BatchScreenshotEntry.Run
    //             -quit -logFile Shot.log
    public static class BatchScreenshotEntry
    {
        public static void Run()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/TrainAI/10_World.unity", OpenSceneMode.Single);

                int w = 1024, h = 576;
                var tex = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
                var cam = new GameObject("__ShotCam").AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.transform.position = new Vector3(120f, 80f, 60f);
                cam.transform.LookAt(Vector3.zero);
                cam.fieldOfView = 60f;
                cam.farClipPlane = 500f;
                cam.targetTexture = tex;
                cam.Render();
                RenderTexture.active = tex;
                var snap = new Texture2D(w, h, TextureFormat.RGB24, false);
                snap.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                snap.Apply();
                RenderTexture.active = null;
                cam.targetTexture = null;

                Directory.CreateDirectory("Assets/Screenshots");
                var bytes = snap.EncodeToPNG();
                string path = "Assets/Screenshots/batch_aerial.png";
                File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);

                Object.DestroyImmediate(cam.gameObject);
                Object.DestroyImmediate(tex);
                Object.DestroyImmediate(snap);
                Debug.Log("[BatchShot] wrote " + path);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[BatchShot] FAILED " + e);
                EditorApplication.Exit(1);
            }
        }
    }
}

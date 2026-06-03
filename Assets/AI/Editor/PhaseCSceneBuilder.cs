// Phase C — Editor menu builder for the new HTTP-based hybrid chat tester.
//
// AI > 5. Phase C — Hybrid Chat Test (Embedding + RAG)
//
// Builds a minimal scene that wires up:
//   - HybridChatClient (HTTP client → 127.0.0.1:8765)
//   - GameStateContext (reflection-based RSO reader)
//   - PhaseCChatTester (OnGUI tester UI)
//
// The Python server (AI_Training/phase_c_chat/scripts/chat_server.py)
// must be running for the chat to work. The tester shows a friendly
// "server không phản hồi" message if it isn't.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TrainAI.AI;

public static class PhaseCSceneBuilder
{
    [MenuItem("AI/5. Phase C — Hybrid Chat Test (Embedding + RAG)", false, 400)]
    public static void BuildPhaseC()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[PhaseC] Stop Play mode first.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var root = new GameObject("PhaseC_HybridChat");
        var client  = root.AddComponent<HybridChatClient>();
        client.serverUrl = "http://127.0.0.1:8765";
        client.timeoutSec = 20;
        client.useGroqFallback = true;
        client.topK = 5;

        var ctx     = root.AddComponent<GameStateContext>();
        // Leave SO references empty — defaults to fallbackDay=1 / fallbackTime=07:30 / fallbackArea="doanh trại".
        // Users wire actual RSO assets in the Inspector when integrating with the real game.

        var tester  = root.AddComponent<PhaseCChatTester>();
        tester.client = client;

        // Save scene.
        const string path = "Assets/AI/Scenes/PhaseC_HybridChat.unity";
        System.IO.Directory.CreateDirectory("Assets/AI/Scenes");
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[PhaseC] Scene built at {path}.");
        Debug.Log("[PhaseC] Make sure chat_server.py is running at 127.0.0.1:8765, then hit Play.");
        Debug.Log("[PhaseC] Start server:  AI_Training/phase_a_sentis/.venv/Scripts/python.exe  AI_Training/phase_c_chat/scripts/chat_server.py");
    }
}

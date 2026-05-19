using TrainAI.SO.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TrainAI.Presentation
{
    /// Makes the Player survive scene transitions and teleport to a "PlayerSpawn"
    /// GameObject when a new scene loads (if one exists in that scene).
    ///
    /// Why this exists:
    ///   Sub-scenes (11_LopHoc, 12_NhaAn, 13_KyTucXa) ship with a PlayerSpawn
    ///   GameObject at a known position. With LoadReplacing replacing 10_World
    ///   when entering a building, the player needs to (a) not get destroyed
    ///   along with 10_World, and (b) appear at the sub-scene's spawn point
    ///   rather than at world coordinates that mean nothing inside the new
    ///   scene.
    ///
    /// Attach to:
    ///   The Player root GameObject (the one with PlayerController + CharacterController).
    ///   Attaches ONCE per game session; subsequent copies destroy themselves so
    ///   re-loading a scene with a baked Player doesn't double-spawn.
    ///
    /// Singleton-by-survival pattern: if an Instance already exists (from an
    /// earlier scene), the newer one self-destructs. This is more robust than
    /// FindObjectsOfType-based dedup, which races with scene Awake order.
    [DefaultExecutionOrder(-900)]
    public class PlayerPersistence : MonoBehaviour
    {
        public static PlayerPersistence Instance { get; private set; }

        [Tooltip("Name of the GameObject this script looks for in newly loaded scenes to teleport to.")]
        [SerializeField] string spawnPointName = "PlayerSpawn";

        [Tooltip("Scene name of the world map. When loaded, player warps to PlayerStateRSO.worldExitPos instead of PlayerSpawn — restores their position at the door they used to enter the sub-scene.")]
        [SerializeField] string worldSceneName = "10_World";

        [Tooltip("Optional state asset for saving/restoring worldExitPos. If null, the world-pos snapshot pattern is disabled (player just goes to PlayerSpawn / stays put).")]
        [SerializeField] PlayerStateRSO playerState;

        [Tooltip("If true, log every scene-load teleport. Useful while wiring up new sub-scenes.")]
        [SerializeField] bool verbose = false;

        void Awake()
        {
            // Duplicate guard: if we're a clone re-spawned by a scene reload,
            // bail out before doing any side effects.
            if (Instance != null && Instance != this)
            {
                if (verbose) Debug.Log($"[PlayerPersistence] '{name}' is a duplicate, destroying.");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || !scene.isLoaded) return;

            bool isWorld = !string.IsNullOrEmpty(worldSceneName) && scene.name == worldSceneName;

            if (isWorld)
            {
                // Returning to the world. Restore to the saved exit position
                // (the door the player used to leave). Skip if we never saved
                // one (e.g. fresh game) — fall back to whatever position the
                // player is currently at (typically PlayerSpawn of the prior
                // sub-scene, which is fine for a fresh start).
                if (playerState != null && playerState.worldExitPos.sqrMagnitude > 0.001f)
                {
                    WarpTo(playerState.worldExitPos);
                    if (verbose) Debug.Log($"[PlayerPersistence] returned to '{scene.name}' at saved exit pos {playerState.worldExitPos}");
                }
                else if (verbose)
                {
                    Debug.Log($"[PlayerPersistence] '{scene.name}': no saved worldExitPos, staying at {transform.position}");
                }
                return;
            }

            // Entering a sub-scene. BEFORE teleporting, snapshot the current
            // world position so we can return to it later. transform.position
            // at this moment is the door's world coord (the player was
            // standing there when the interaction fired).
            if (playerState != null) playerState.worldExitPos = transform.position;

            GameObject spawn = null;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length && spawn == null; i++)
            {
                if (roots[i].name == spawnPointName) { spawn = roots[i]; break; }
                var t = roots[i].transform.Find(spawnPointName);
                if (t != null) spawn = t.gameObject;
            }

            if (spawn == null)
            {
                if (verbose) Debug.Log($"[PlayerPersistence] sub-scene '{scene.name}' has no '{spawnPointName}', staying put at {transform.position}.");
                return;
            }

            WarpTo(spawn.transform.position, spawn.transform.rotation);
            if (verbose) Debug.Log($"[PlayerPersistence] entered '{scene.name}' at {spawn.transform.position} (saved exit pos {playerState?.worldExitPos})");
        }

        void WarpTo(Vector3 pos) => WarpTo(pos, transform.rotation);

        void WarpTo(Vector3 pos, Quaternion rot)
        {
            // Disable the CharacterController briefly because it resists
            // external position writes (it has its own internal collision-
            // resolution state); flicking it off lets the warp land cleanly.
            var cc = GetComponent<CharacterController>();
            bool hadCC = cc != null && cc.enabled;
            if (hadCC) cc.enabled = false;
            transform.position = pos;
            transform.rotation = rot;
            if (hadCC) cc.enabled = true;
        }
    }
}

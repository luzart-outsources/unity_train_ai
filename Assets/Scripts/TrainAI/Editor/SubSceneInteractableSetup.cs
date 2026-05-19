using System.IO;
using TrainAI.Presentation;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TrainAI.Editor
{
    // Wires the `Interactable_Center` GameObject in each sub-scene to the
    // correct InteractionSO per GDD:
    //
    //   11_LopHoc  → OpenQuizInteractionSO (returnScene = 10_World)
    //   12_NhaAn   → OpenConfirmInteractionSO "Bạn đang ăn" (returnScene = 10_World)
    //   13_KyTucXa → SleepInteractionSO "Đi ngủ" (returnScene = 10_World, advanceDay)
    //
    // Why this exists:
    //   SceneBuilder creates an Interactable_Center cube + InteractableMarker
    //   in each sub-scene but leaves the marker's `interactable` field unset.
    //   Without an SO assigned, PlayerInteractor finds the marker but every
    //   nearby check skips it ("interactable.interactable == null → ignore").
    //   This tool fills in the SOs once.
    //
    // Idempotent: re-running rewrites the same fields. Creating the new SO
    // assets is also gated on AssetDatabase exists check.
    public static class SubSceneInteractableSetup
    {
        const string StrategyDir       = "Assets/_Data/Strategies";
        const string WorldSceneRefPath = "Assets/_Data/Scenes/SceneRef_10_World.asset";

        // Strategy SOs (the WHAT-happens part)
        const string InteractEatPath   = "Assets/_Data/Strategies/Interact_Eat_NhaAn.asset";
        const string InteractQuizPath  = "Assets/_Data/Strategies/Interact_Quiz_LopHoc.asset";
        const string InteractSleepPath = "Assets/_Data/Strategies/Interact_Sleep_KTX.asset";

        // World-side door Interactables — we read their `area` field so each
        // sub-scene interactable inherits the same area. That way the
        // QuestRouter.IsInteractableAllowed area-match check passes whether
        // the player still has the parent quest active or it already
        // completed on door entry.
        const string DoorLopHocPath  = "Assets/_Data/Interactables/Interactable_LopHoc_Door.asset";
        const string DoorNhaAnPath   = "Assets/_Data/Interactables/Interactable_NhaAn_Door.asset";
        const string DoorKTXPath     = "Assets/_Data/Interactables/Interactable_KTX_Door.asset";

        // The inside-scene InteractableSOs (the WHERE-when-interacting part —
        // attached to InteractableMarker.interactable in each sub-scene).
        const string InsideLopHocPath = "Assets/_Data/Interactables/Interactable_LopHoc_Quiz.asset";
        const string InsideNhaAnPath  = "Assets/_Data/Interactables/Interactable_NhaAn_Eat.asset";
        const string InsideKTXPath    = "Assets/_Data/Interactables/Interactable_KTX_Bunk.asset";

        const string LopHocScene  = "Assets/Scenes/TrainAI/11_LopHoc.unity";
        const string NhaAnScene   = "Assets/Scenes/TrainAI/12_NhaAn.unity";
        const string KyTucXaScene = "Assets/Scenes/TrainAI/13_KyTucXa.unity";

        [MenuItem("Tools/Build Game/HOLA Map/Setup Sub-Scene Interactables (per GDD)", false, 240)]
        public static void Setup()
        {
            // Make sure the strategies folder exists.
            if (!AssetDatabase.IsValidFolder(StrategyDir))
                AssetDatabase.CreateFolder("Assets/_Data", "Strategies");

            var world = AssetDatabase.LoadAssetAtPath<SceneRefSO>(WorldSceneRefPath);
            if (world == null) { Debug.LogError($"[SubScene] Missing world SceneRef: {WorldSceneRefPath}"); return; }

            // === 1. Eat interaction (NhaAn) ===
            var eat = AssetDatabase.LoadAssetAtPath<OpenConfirmInteractionSO>(InteractEatPath);
            if (eat == null)
            {
                eat = ScriptableObject.CreateInstance<OpenConfirmInteractionSO>();
                AssetDatabase.CreateAsset(eat, InteractEatPath);
            }
            eat.confirmText = "Ban dang an com";
            eat.skipTime = false;
            eat.skipToHour = -1;
            eat.completeQuestOnConfirm = true;
            eat.returnScene = world;
            eat.returnTransitionText = "Roi nha an, quay ra san...";
            eat.promptText = "Bam E de an com";
            EditorUtility.SetDirty(eat);

            // === 2. Quiz interaction (LopHoc) ===
            var quiz = AssetDatabase.LoadAssetAtPath<OpenQuizInteractionSO>(InteractQuizPath);
            if (quiz == null)
            {
                quiz = ScriptableObject.CreateInstance<OpenQuizInteractionSO>();
                AssetDatabase.CreateAsset(quiz, InteractQuizPath);
            }
            // Try to find a QuizSet to attach by default — first one in the data folder.
            if (quiz.quizSet == null)
            {
                var quizGuids = AssetDatabase.FindAssets("t:QuizSetSO");
                if (quizGuids.Length > 0)
                {
                    var qsPath = AssetDatabase.GUIDToAssetPath(quizGuids[0]);
                    quiz.quizSet = AssetDatabase.LoadAssetAtPath<QuizSetSO>(qsPath);
                    Debug.Log($"[SubScene] Quiz default set to {qsPath}");
                }
                else Debug.LogWarning("[SubScene] No QuizSetSO found in project — quiz interaction will be inert until one is assigned.");
            }
            quiz.completeQuestAfter = true;
            quiz.returnScene = world;
            quiz.returnTransitionText = "Het tiet, ve san...";
            quiz.promptText = "Bam E de lam bai";
            EditorUtility.SetDirty(quiz);

            // === 3. Sleep interaction (KTX) — existing asset, just patch returnScene ===
            var sleep = AssetDatabase.LoadAssetAtPath<SleepInteractionSO>(InteractSleepPath);
            if (sleep == null)
            {
                sleep = ScriptableObject.CreateInstance<SleepInteractionSO>();
                AssetDatabase.CreateAsset(sleep, InteractSleepPath);
            }
            sleep.confirmText = "Di ngu";
            sleep.loadingText = "Sang ngay hom sau...";
            sleep.returnScene = world;
            sleep.returnTransitionText = "Sang ngay moi, ra san...";
            sleep.promptText = "Bam E de di ngu";
            EditorUtility.SetDirty(sleep);

            AssetDatabase.SaveAssets();

            // === 4. Build the inside-scene InteractableSO wrappers (data
            //         linking area + onInteract). Area is borrowed from the
            //         existing world-side door so the quest area-match check
            //         passes whether or not the parent quest is still active.
            EnsureInteractable(InsideLopHocPath, DoorLopHocPath, "LopHoc_Quiz",  quiz);
            EnsureInteractable(InsideNhaAnPath,  DoorNhaAnPath,  "NhaAn_Eat",    eat);
            EnsureInteractable(InsideKTXPath,    DoorKTXPath,    "KTX_Bunk",     sleep);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // === 5. Wire each scene's Interactable_Center. Pass PATHS, not
            // object refs — opening a scene in Single mode invalidates the
            // in-memory ScriptableObject pointers we just created. WireScene
            // re-loads by path after the scene open completes.
            WireScene(LopHocScene,  InsideLopHocPath, new Vector3(0f, 0.5f, 0f));   // center of desk grid
            WireScene(NhaAnScene,   InsideNhaAnPath,  new Vector3(0f, 0.5f, 0f));   // center of dining tables
            WireScene(KyTucXaScene, InsideKTXPath,    new Vector3(-7f, 0.5f, 6f));  // at the first bunk

            Debug.Log("[SubScene] Done. Each sub-scene's Interactable_Center now has its GDD-correct interaction wired.");
        }

        // Creates (or refreshes) an InteractableSO that shares its `area` with
        // an existing world-side door's interactable. If the source door is
        // missing, falls back to area=null (interaction still works when no
        // quest is active, which is the common state inside sub-scenes).
        static InteractableSO EnsureInteractable(string path, string doorRefPath, string id, InteractionSO onInteract)
        {
            var iso = AssetDatabase.LoadAssetAtPath<InteractableSO>(path);
            if (iso == null)
            {
                iso = ScriptableObject.CreateInstance<InteractableSO>();
                AssetDatabase.CreateAsset(iso, path);
            }
            iso.id = id;
            iso.onInteract = onInteract;
            // Critical: in-sub-scene interactables MUST bypass the quest gate.
            // Otherwise, AutoChain can advance the clock to a quest with a
            // different area while the player is mid-meal / mid-quiz / mid-
            // sleep, and they'd get "Chưa tới giờ" + no way to exit.
            iso.bypassQuestGate = true;
            var door = AssetDatabase.LoadAssetAtPath<InteractableSO>(doorRefPath);
            if (door != null && door.area != null)
            {
                iso.area = door.area;
                Debug.Log($"[SubScene] {id} inherited area '{door.area.id}' from {Path.GetFileName(doorRefPath)} (bypassQuestGate=true)");
            }
            else
            {
                Debug.LogWarning($"[SubScene] No world-side door at {doorRefPath} or its area is null — {id}.area stays as-is");
            }
            EditorUtility.SetDirty(iso);
            return iso;
        }

        static void WireScene(string scenePath, string interactableAssetPath, Vector3 movePos)
        {
            // Open scene FIRST. Loading the asset only after the scene swap
            // ensures the SO pointer is alive in the new editor state.
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var interactable = AssetDatabase.LoadAssetAtPath<InteractableSO>(interactableAssetPath);
            if (interactable == null)
            {
                Debug.LogError($"[SubScene] Could not re-load InteractableSO at {interactableAssetPath} after opening {scenePath}");
                return;
            }

            GameObject ic = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Interactable_Center") { ic = root; break; }
                var t = root.transform.Find("Interactable_Center");
                if (t != null) { ic = t.gameObject; break; }
            }
            if (ic == null)
            {
                // Create one if the scene wasn't built with SceneBuilder.
                ic = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ic.name = "Interactable_Center";
                ic.transform.position = movePos;
                ic.transform.localScale = new Vector3(2, 1, 2);
                var bc = ic.GetComponent<BoxCollider>();
                if (bc != null) bc.isTrigger = true;
            }
            else
            {
                // Reposition to be more accessible per scene layout.
                ic.transform.position = movePos;
            }
            var marker = ic.GetComponent<InteractableMarker>();
            if (marker == null) marker = ic.AddComponent<InteractableMarker>();
            marker.interactable = interactable;
            EditorUtility.SetDirty(ic);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SubScene] {Path.GetFileName(scenePath)} ← {interactable.name} (Interactable_Center at {movePos})");
        }
    }
}

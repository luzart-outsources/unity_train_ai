# Phase 11 — Automation tooling (Editor master builder) (blueprint)

**Goal:** Implement `SOGenerator`, `PrefabBuilder`, `SceneBuilder`, `ServiceWizard`, `Validator`, `MasterBuilder` in `Assets/Editor/Automation/`. `Tools/Build Game/Generate All (Master)` should reproduce the entire Phase 9-10 state from `world_template.json`.

**Spec refs:** §12, §13, [[technical/automation-tooling]], [[decisions/d08-automation-last]].

---

## Tasks

### 11.1 — `world_template.json` schema + sample
- [ ] Author skeleton at `Assets/Editor/Automation/world_template.json` with 1 day, 2 NPC, 5 areas, 2 subjects per spec §12.4.
- [ ] Define matching `WorldBlueprint` POCO + sub-types in `Assets/Scripts/Editor/Automation/Blueprint/`.

### 11.2 — `SOGenerator`
- [ ] `[MenuItem("Tools/Build Game/1. Generate Data SO")]` → parse JSON → `CreateOrUpdate<T>(path)` pattern for each SO type.
- [ ] Idempotent: re-run doesn't duplicate. Updates fields in place.
- [ ] Order: Areas → Subjects → Quizzes → Interactables → NPCs → Quests → Days → AutoPopulate all DBs.

### 11.3 — `StrategyGenerator`
- [ ] Generate Strategy SO presets: `Movement_Onnx.asset`, `Movement_NavMesh.asset`, `Movement_Idle.asset`, `Dialogue_Onnx.asset`, `Dialogue_Mock.asset`, `Rule_LatePenalty.asset`, `Rule_QuizScore.asset`, `Rule_Expulsion.asset`, `Rule_Ending_Academy.asset`.

### 11.4 — `PrefabBuilder`
- [ ] Build `Player.prefab`, `NPC_Static.prefab`, `NPC_Mobile.prefab`, `QuestArrow.prefab`, `UI*.prefab` from scratch with placeholder mesh + material.
- [ ] `PrefabUtility.SaveAsPrefabAsset`.

### 11.5 — `SceneBuilder`
- [ ] Generate 7 scenes programmatically.
- [ ] World scene: parse `areas`/`npcs` from blueprint, spawn cube placeholders at correct positions with correct triggers.
- [ ] Add `BootstrapEntry + GameLoopDriver` to `00_Bootstrap`.
- [ ] Register all scenes to `EditorBuildSettings.scenes`.

### 11.6 — `ServiceWizard`
- [ ] `BuildServiceLocator()` → find `ServiceLocator.asset` (or create) → assign all DB / RSO / Config / ModelAsset / responses.json refs by `AssetDatabase.FindAssets`.
- [ ] Mark dirty + save.

### 11.7 — `Validator`
- [ ] Reflect all `[Required]`-marked fields in every SO. Null → fail.
- [ ] Check no asmdef cycle (walk references).
- [ ] Check `CompilationPipeline.GetAssemblies()` for errors.
- [ ] Check every `SceneRefSO.sceneName` exists in `EditorBuildSettings.scenes`.
- [ ] Check `responses.json` parseable + has 8 intent keys.
- [ ] Output `Library/automation_report.md`.

### 11.8 — `MasterBuilder`
- [ ] `[MenuItem("Tools/Build Game/⚡ Generate All (Master)")]` → run 11.2 → 11.3 → 11.4 → 11.5 → 11.6 → `CompilationPipeline.RequestScriptCompilation()` → await `AssemblyReloadEvents.afterAssemblyReload` → 11.7.
- [ ] Open `00_Bootstrap.unity` at end.
- [ ] Log summary to Console + write report.

### 11.9 — Debug menu items
- [ ] `Tools/Debug/Reset Save` → delete save file.
- [ ] `Tools/Debug/Skip to Day X` → modal int input → set `GameClockRSO.day`.
- [ ] `Tools/Debug/Force Quest Complete` → call `QuestRouter.Complete(current, true)`.

---

## Acceptance

- [ ] `Tools/Build Game/Generate All (Master)` runs without throw.
- [ ] After running, `00_Bootstrap` opens; Press Play succeeds; Day 1 still playable.
- [ ] `Library/automation_report.md` shows all ✅.
- [ ] Running Master 2nd time produces same result (idempotent).

Proceed to `phase-12-master-build.md`.

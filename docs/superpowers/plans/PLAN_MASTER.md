# TrainAI — Master Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to execute per-phase plans below. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a full 30-day military-academy life-sim game in Unity 6 with SO-First Modular architecture, ONNX AI for NPC chat + pathfinding, and Editor automation to generate the entire 30-day content from a JSON blueprint.

**Architecture:** SO-First Modular (Strategy SO + DB SO + RSO + ServiceLocator-via-SO + BroadcastService typed pub/sub). uGUI for UI. 3D third-person with quest arrow under player. Unity AI Inference Engine (`Unity.InferenceEngine`, formerly Sentis) for ONNX runtime.

**Tech Stack:** Unity 6 · `com.unity.ai.inference` 2.6.1 · UniTask · Unity Input System 1.14.2 · TextMeshPro · uGUI · CharacterController (kinematic).

**Spec source:** [`docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md`](../specs/2026-05-12-trainai-gdd-tech-design.md)

---

## Phase index (build order)

| # | Phase | Plan file | Status | Acceptance |
|---|---|---|---|---|
| 0 | Project skeleton (asmdef, folders, ServiceLocator stub) | [phase-00-skeleton.md](./phase-00-skeleton.md) | Ready | `dotnet/Unity` compile clean; folder tree present |
| 1 | Core (BroadcastService, RuntimeSO base, util) | [phase-01-core.md](./phase-01-core.md) | Ready | Edit-mode unit test: subscribe/send/unsubscribe round-trip |
| 2 | SO base + concrete minimal (1 of each) | [phase-02-so-base.md](./phase-02-so-base.md) | Ready | Editor creates 1 asset of each base via `CreateAssetMenu` |
| 3 | Data SO classes + DB SO (auto-populate context menus) | [phase-03-data-so.md](./phase-03-data-so.md) | Blueprint | `DayDB.AutoPopulate()` scans `_Data/` and fills list |
| 4 | Services (Clock, Quest, Score, Scene, UI, Save) — stub Sentis | [phase-04-services.md](./phase-04-services.md) | Blueprint | Services instantiate via ServiceLocatorSO.Bootstrap; clock ticks, quest activates |
| 5 | Sentis integration (Tokenizer, IntentClassifier, OnnxMovementAgent) | [phase-05-sentis.md](./phase-05-sentis.md) | Blueprint | Edit-mode test runs intent classifier on sample input; soldier policy outputs 2D action |
| 6 | Player + NPC prefab + camera rig + quest arrow | [phase-06-prefabs.md](./phase-06-prefabs.md) | Blueprint | Walk Player in scene, arrow points to target, camera follows |
| 7 | UI (Confirm/Quiz/Dialogue/Loading/HUDs) uGUI | [phase-07-ui.md](./phase-07-ui.md) | Blueprint | All popups openable manually via test scene |
| 8 | Save/Load (JSON DTO + migrator) | [phase-08-save.md](./phase-08-save.md) | Blueprint | Round-trip: Save → restart → Load matches state |
| 9 | Scene 10_World + sub-scenes (manual 1-day prototype) | [phase-09-scenes.md](./phase-09-scenes.md) | Blueprint | One playable day in editor |
| 10 | Vertical slice test (1 day end-to-end) | [phase-10-vertical-slice.md](./phase-10-vertical-slice.md) | Blueprint | Day 1 complete with all systems wired |
| 11 | Automation tooling (Generator, Builder, Validator) | [phase-11-automation.md](./phase-11-automation.md) | Blueprint | `Tools/Build Game/Generate All` runs; `automation_report.md` PASS |
| 12 | Master Generate All → 30-day full content | [phase-12-master-build.md](./phase-12-master-build.md) | Blueprint | 30 days playable, 187+ quests, all assets generated |
| 13 | Polish, balance, ending screen | [phase-13-polish.md](./phase-13-polish.md) | Blueprint | Ending grades, balance pass, build artefact |

"Ready" = bite-sized step-by-step tasks. "Blueprint" = task list + acceptance criteria; expand to bite-sized in a fresh session when starting that phase.

---

## Global conventions enforced across all phases

1. **TDD where feasible**: every non-MonoBehaviour C# class gets an Edit-mode unit test in `Assets/Tests/EditMode/` under matching asmdef. MonoBehaviour gets Play-mode test in `Assets/Tests/PlayMode/`.
2. **Frequent commits**: each task (5-15 min) ends with a commit. Use Conventional Commits prefix: `feat(scope):`, `fix(scope):`, `test(scope):`, `chore(scope):`.
3. **No skipping hooks** (`--no-verify` forbidden).
4. **Tensor lifecycle**: every Sentis tensor is `using`-disposed. Lint rule: grep for `new Tensor` without preceding `using`.
5. **Editor-only**: any code that uses `UnityEditor.*` is in `Assets/Scripts/Editor/` with `TrainAI.Editor.asmdef` (`includePlatforms: ["Editor"]`).
6. **Idempotent generators**: any `Tools/Build Game/*` action must succeed on second run without duplicating assets.

---

## Acceptance criteria (global)

The implementation is "done" when:

- [x] Spec exists and is approved by user.
- [ ] All 14 phases checked in.
- [ ] Open `00_Bootstrap.unity` → Play → reach UIEnding on day 30 in a single playthrough (or via debug `Skip to Day X`).
- [ ] `Tools/Build Game/Generate All (Master)` produces a clean `Library/automation_report.md` (PASS).
- [ ] Swap `NPC_HocSinh_01.movementStrategy` from `MovementOnnx` to `MovementNavMesh` in Inspector → NPC still navigates → no compile error → no runtime error.
- [ ] Add a new quest type by inheriting `QuestSO` + creating asset → appears in `DayDB` and is activated correctly.
- [ ] Save game → quit → relaunch → Continue restores correct day/scene/score.

---

## How to use this plan

**In current session**: Read `phase-00-skeleton.md` and start executing. After Phase 0 acceptance met, move to Phase 1.

**In a new session / new machine**: See `docs/superpowers/HANDOFF.md` for context-loading procedure.

**Subagent driver**: Use `superpowers:subagent-driven-development` skill — it reads this master plan + the per-phase plan, dispatches a fresh subagent per task with full context, reviews between tasks.

**Inline driver**: Use `superpowers:executing-plans` skill — it batch-executes tasks in current session with checkpoint reviews.

---

## Phase ordering rationale

- **Phase 0-2** establish the type system and event backbone — everything else compiles against them.
- **Phase 3** populates data SO so Phase 4 services have something to read.
- **Phase 4** stubs Sentis so services don't crash when called; real Sentis comes in Phase 5.
- **Phase 5** activates AI without changing service signatures — just swaps stub for real `SentisRuntime`.
- **Phase 6-7** add Presentation and UI; can run without service backend (mock services) for quick UI iteration.
- **Phase 8** persistence — depends on RSO + service interfaces being stable.
- **Phase 9** first real scene work, prove the assembly all hangs together.
- **Phase 10** vertical slice — 1 day, all systems live. Critical milestone before automation.
- **Phase 11-12** automation: only after slice is stable, generators are safe to write because the schema is locked.
- **Phase 13** polish — non-blocking improvements + ending screen.

---

## Risk register (live)

| Risk | Mitigation |
|---|---|
| Sentis API change in Unity 6 (namespace `Unity.Sentis` → `Unity.InferenceEngine`) | Wrap inside `SentisRuntime` — only one file knows the API |
| `soldier.onnx` underperforms outside training arena | Phase 5 includes A/B test; if fail, switch to `NavMeshMovementSO` per [[decisions/d03-strategy-swap-ai]] |
| `intent_classifier.onnx` regresses on real input | Phase 5 has manual QA pass with real Vietnamese strings |
| Compile/asmdef cycle | Phase 0 sets up asmdef tree; Validator (Phase 11) detects cycles |
| Asset count too high → editor slow | DBs use plain `List<T>` → no scene-wide search; Phase 11 generator is idempotent |
| Save schema change breaks old saves | `SaveMigrator` chain from Phase 8 |
| 30-day content authoring effort | `world_template.json` is single source of truth; LLM can fill from GDD in fresh session |

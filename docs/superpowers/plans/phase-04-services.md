# Phase 4 — Services (Clock, Quest, Score, Scene, UI, Save, NPCDirector, InteractionRouter, MovementService, DialogueService) — Sentis stub

> Blueprint. Each task ~30-60 min.

**Goal:** Wire all 11 services in `ServiceLocatorSO.Bootstrap()`. SentisRuntime is a stub returning `null` Worker — concrete in Phase 5.

**Spec refs:** §4.3-4.5, [[technical/architecture]].

---

## Tasks

### 4.1 — Service interfaces
- [ ] Create `Assets/Scripts/Services/Interfaces/` and add `IGameClock`, `IQuestRouter`, `ISceneRouter`, `IScoreSystem`, `IInteractionRouter`, `INPCDirector`, `IDialogueService`, `IMovementService`, `IUIRouter`, `ISaveService`, `ISentisRuntime` exactly as spec §4.4.

### 4.2 — `GameClockService`
- [ ] Read `TimeConfigSO.gameHourPerRealMinute`. `Tick(dt)` accumulates fractional minutes; on minute rollover Broadcast `TimeTickMsg`. `Freeze/Resume`. `SkipTo(h,m)` sets immediately.
- [ ] Test: `Tick(180)` (3 min real = 1h game) → `Hour` advances 1.

### 4.3 — `ScoreSystem`
- [ ] Subscribe `QuestMissedMsg` → apply rules → `Broadcast ScoreChangedMsg`.
- [ ] Subscribe `QuizEndedMsg` → compute round((correct/total)*10) → +int to `hocTap` (cap to subject.maxPerLesson) → broadcast.
- [ ] If `state.renLuyen <= 0` → `Broadcast ExpelTriggeredMsg`.
- [ ] Tests: each rule, expulsion trigger.

### 4.4 — `QuestRouter`
- [ ] `StartDay(int day)` loads `DayDB.Days[day-1].quests`.
- [ ] Subscribe `TimeTickMsg` → activate next quest by `window.startHour/Minute`. Broadcast `QuestActivatedMsg`.
- [ ] `Tick(dt)` decrements remaining; if deadline missed and !complete → Broadcast `QuestMissedMsg`.
- [ ] `Complete(quest, success)` → invoke runtime `OnComplete(success)` → Broadcast `QuestCompletedMsg`.
- [ ] `IsInteractableAllowed(interactable)` → check area match.
- [ ] `GetTodaySummary()` → comma-join quest titles for today.
- [ ] Tests: activation by time, deadline, day rollover, gating.

### 4.5 — `SceneRouter`
- [ ] `LoadAdditive(SceneRefSO, transitionText)` → optionally `await UIRouter.ShowLoading(text, 3s)` then `SceneManager.LoadSceneAsync(..., Additive)`.
- [ ] `UnloadAdditive`, `LoadSingle`.
- [ ] Tests are PlayMode (need real scene loader) — defer to Phase 9.

### 4.6 — `UIRouter` skeleton
- [ ] Stub `Show*` methods that just `Debug.Log` and return immediately. Real prefabs in Phase 7.

### 4.7 — `SaveService`
- [ ] Implement `Save(slot)` writing JSON; `TryLoad(slot, out dto)` reading JSON.
- [ ] Use `Newtonsoft.Json` (already shipped with Unity) or `JsonUtility`.
- [ ] Tests: round-trip in EditMode using temp file path.

### 4.8 — `InteractionRouter`
- [ ] Subscribe `InteractPressedMsg`. Check `QuestRouter.IsInteractableAllowed`. Execute `target.onInteract.Execute(ctx)`.

### 4.9 — `MovementService` (stub)
- [ ] Dictionary `Transform → IMovementAgent`. `RegisterAgent`, `SetTarget`, `Tick(dt)` with 5Hz throttle. Real Sentis call in Phase 5.

### 4.10 — `DialogueService` (stub)
- [ ] `Reply(npc, input)` calls `npc.dialogueStrategy.Reply(input, ctx)`. Builds `NpcContext` from `IQuestRouter.GetTodaySummary()`, `PlayerStateRSO`, etc.

### 4.11 — `NPCDirector` (stub)
- [ ] Subscribe `TimeTickMsg`. Foreach NPC in `NPCDB`, lookup schedule entry, call `MovementService.SetTarget`. (NPC registration on World scene load — Phase 6.)

### 4.12 — `SentisRuntime` stub
- [ ] Implements `ISentisRuntime`. Both Workers return null. Dispose no-op. Real impl Phase 5.

### 4.13 — `ServiceLocatorSO.Bootstrap()` wire-up
- [ ] In `Bootstrap()`, instantiate all 11 services in dependency order. Pass DBs/RSOs/configs via constructor. Store in public properties.
- [ ] PlayMode test: enter Play in a test scene with `BootstrapEntry`, verify `Debug.Log` shows all services constructed without throw.

---

## Acceptance

- [ ] 11 service interfaces + concrete implementations.
- [ ] `ServiceLocatorSO.Bootstrap()` constructs all without exception.
- [ ] Clock ticks; quest activates by time in EditMode test.
- [ ] Save round-trip test PASS.
- [ ] Console clean after Bootstrap.

Proceed to `phase-05-sentis.md`.

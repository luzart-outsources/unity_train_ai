# Claude as Robot Player — Design

**Date:** 2026-05-18
**Goal:** Claude drives the TrainAI game directly through UnityMCP (no test framework code in the project), playing as a user would, observing state, and detecting deviations from the GDD as bugs.

## Constraints (hard rules)

1. **Game runtime code is frozen.** Zero changes to `Assets/Scripts/TrainAI/Services|Presentation|UI|Core/*.cs` for the purpose of being tested. If a test reveals a bug → fix it (that's a *real* fix), but never add test hooks, dev-only branches, "if (botMode) ..." switches, or robot-aware code paths into runtime files.
2. **Generic Editor-only utilities are permitted as a last resort.** If MCP cannot do something natively (e.g., enqueue an input event in Play mode), a small generic helper in `Assets/Editor/` is acceptable — but only if it is content-agnostic (e.g., "send keyboard event by keycode" — never "go to SanVanDong"). Robot scenario logic stays in Claude.
3. **Infrastructure logs are not test code.** If `BroadcastService.Send<T>` needs `Debug.Log($"[Broadcast] {typeof(T).Name}")` so external observers can tail events, that is a one-line *observability* change, not robot code. Prefer it over adding state-poll hacks if pure polling proves insufficient.
4. **No scenario hardcoding in Claude.** Decisions react to observed state (active quest, current scene, score, time-of-day). The bot does not have a script that says "Day 1 step 3: walk to NhaAn"; it sees `QuestActivatedMsg{quest=AnSang}` → reads `quest.area.worldPos` → walks there.

## Scope

- Drive Player movement with real input simulation (not teleport, not direct position writes — actual InputSystem key events so `PlayerController.ReadMove` exercises its full path)
- Drive UI by invoking button `onClick` via reflection or simulated clicks
- Observe game state reactively from `BroadcastService` messages, scene resources, and console
- Detect bug = "GDD expected X within T seconds, observed Y" or any error/exception in console
- Loop fix-test until the full Day 1 quest chain runs clean, then expand to 30 days

## Architecture

```
Claude (this agent)
   │
   ├── execute_code: inject runtime C# to simulate input
   │     - InputSystem.QueueDeltaStateEvent on Keyboard.current.wKey/aKey/sKey/dKey
   │     - Button.onClick.Invoke() for UI taps
   │     - BroadcastService.Send(InteractPressedMsg) to bypass keyboard for E
   │
   ├── find_gameobjects + ReadMcpResource: observe Player position, active quest, scene
   │
   ├── read_console: detect exceptions / errors / Debug.Log markers
   │
   └── manage_camera screenshot: visual sanity check at decision points
```

No new C# files in the project. Everything via MCP.

## Observation Channel

**Discovery findings (2026-05-18):**
- `Core/BroadcastService.Send<T>()` does **not** log anything. 17 message types exist (see `Core/Messages.cs`) — none of them surface on the Unity Console today.
- Many events have a durable downstream effect on a ScriptableObject (RSO) → **pollable**:
  - `QuestActivatedMsg` → `ActiveQuestRSO.current`
  - `QuestCompletedMsg` → `DayProgressRSO.completedToday`
  - `TimeTickMsg` / `DayStartedMsg` → `GameClockRSO.day,hour,minute`
  - `ScoreChangedMsg` → `PlayerStateRSO.hocTap,renLuyen`
- Some events are **transient** with no durable mirror — these are observation-blind via pure polling:
  - `InteractZoneEnteredMsg` / `InteractZoneExitedMsg` (the prompt UI element's visibility is a proxy, but not 1:1)
  - `DialogueRequestMsg`, `QuizStartedMsg` / `QuizEndedMsg` (UI panel active state is the proxy)
  - `InteractPressedMsg` (no proxy — it's an action, not a state)

**Strategy (in priority order):**

1. **Primary — poll RSOs.** State-based detection from `ActiveQuestRSO`, `GameClockRSO`, `PlayerStateRSO`, `DayProgressRSO`. Zero project changes. Sufficient for ~80% of bug rules in the table below.
2. **Secondary — query scene UI state.** For transient events with a UI proxy, query the active state of named GameObjects (`DialoguePanel`, `QuizPanel`, `InteractPrompt`). Done via MCP `find_gameobjects` + property read.
3. **Last resort — add one infrastructure log line.** If a specific transient event is critical to bug detection and has no scene proxy (e.g., we need to confirm `InteractPressedMsg` fired before the prompt closed), add `Debug.Log($"[Bcast] {typeof(T).Name}")` to `BroadcastService.Send<T>`. This is a 1-LOC observability change, not test code — and it benefits real-world debugging too. Document the addition in CLAUDE.md.

Day 0 must validate which of (1)(2)(3) is sufficient before committing to a path. Do not pre-emptively add logs.

## Reactive State Machine (in Claude's loop)

```
IDLE
  └─ poll: is _activeQuest.current != null?
       └─ yes → set target = quest.area.worldPos → WALKING

WALKING
  ├─ each tick: inject WASD input toward target
  ├─ poll Player.position vs target
  ├─ when dist < trigger range (≈3m): WAIT_PROMPT
  └─ timeout (walk took >2× expected): BUG_NO_REACH

WAIT_PROMPT
  ├─ poll for InteractPromptVisible (or check UIInteractPromptController.canvasGroup.alpha)
  ├─ when visible: INTERACTING
  └─ timeout (>3s in zone with no prompt): BUG_NO_PROMPT

INTERACTING
  ├─ press E (inject keyboard event)
  ├─ if confirm dialog appears: click OK button
  ├─ if quiz appears: answer all correct
  ├─ if scene transition: wait for subscene load + return
  └─ on QuestCompletedMsg: VERIFY_STATE

VERIFY_STATE
  ├─ expected: clock skipped per quest, hocTap/renLuyen updated correctly
  ├─ pass: → IDLE
  └─ mismatch: BUG_STATE_DRIFT
```

## Bug Detection Rules (GDD as oracle)

| Rule | Expected | Bug if |
|---|---|---|
| Quest activates on time | At quest.window.startHour:startMinute, `_activeQuest.current == quest` | No activation 5s after window starts |
| Quest area matches scene | `quest.area.worldPos == Area_*_Door.transform.position` | Mismatch (this is the bug we hit in Bug 2!) |
| Prompt visible in zone | `InteractZoneEnteredMsg` fired when player in trigger | No msg within 2s of overlap |
| Confirm completes quest | `QuestCompletedMsg(success=true)` within 3s of clicking OK | Timeout |
| Clock skips correctly | `SkipTo` jumps to next quest.start | Clock advances normally but no skip |
| Day rolls over | After last quest (Sleep), `DayStartedMsg(day+1)` fires | No rollover |
| No console errors | `read_console types=error count >0` returns empty | Any error logged |
| No crash | Unity process alive | crash dump folder count increased |

## Input Simulation Pattern

```csharp
// Inject via execute_code (one-shot, runs in editor context with play-mode awareness)
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using (StateEvent.From(Keyboard.current, out var ev))
{
    Keyboard.current.wKey.WriteValueIntoEvent(1.0f, ev);
    InputSystem.QueueEvent(ev);
}
InputSystem.Update();
```

State persists until the next state change. So "walk for 3 sec" = press W via one call, sleep 3s in Claude's loop, release W via another call.

For UI tap: `GameObject.Find("CanvasName").GetComponentInChildren<Button>().onClick.Invoke()` skips the click pipeline but exercises the button's handler — same end state as real click without mouse simulation overhead.

## Run Loop (Claude's perspective)

```
1. Open 00_Bootstrap, enter play mode (via manage_editor action=play)
2. Wait for services online (poll ServiceLocator)
3. Inject UI click on MainMenu/NewGame button
4. Wait for CreateChar, type "AutoTester", click Confirm
5. Wait for 10_World loaded
6. Run reactive loop until day 30 OR first bug
7. On bug:
   a. Log: state, expected, observed, screenshot
   b. Stop play mode
   c. Read code, find root cause
   d. Apply fix
   e. Restart from step 1
8. On 30-day completion: PASS
```

## Failure Detection Heuristics (timeout-based invariants)

Rather than "expected event sequence at step N", failures are detected by **invariants that hold over time**. Each invariant has a timeout — exceeding it means the bot is stuck or the game deviated.

| Invariant | Window | Failure label |
|---|---|---|
| If `ActiveQuestRSO.current != null`, player position must change by ≥ 0.5m every 5s | 5s no-move | `STUCK_NAVIGATING` (geometry block, navmesh hole, blocked door) |
| If player within 3m of `quest.area.worldPos` for ≥ 2s, an interactable prompt must be visible | 2s in-zone-no-prompt | `MISSING_PROMPT` (trigger not firing, prompt UI broken) |
| After pressing E in interact zone, either confirm dialog opens or `QuestCompletedMsg`/state change within 3s | 3s no-feedback | `INTERACT_DEAD` |
| Within 30s of `quest.window.startHour:startMinute`, `ActiveQuestRSO.current` should be that quest | 30s no-activate | `QUEST_NO_FIRE` |
| `DayProgressRSO.completedToday.Count + missedToday.Count` increases monotonically | regression | `STATE_ROLLBACK` |
| Unity Console error/exception count | any new error | `RUNTIME_ERROR` |
| Unity process responsive (manage_editor query returns) | 10s no-response | `CRASH_OR_HANG` |
| `GameClockRSO.day` advances after sleep quest completes | 30s no-rollover | `DAY_STUCK` |

On any failure: capture screenshot, dump RSO state, dump last 50 console lines, stop play mode, root-cause, fix in code, restart loop. Recovery is *fresh load* unless we have a checkpoint (see below).

## Checkpoints (resume mid-run after a bug fix)

Game already has `Services/SaveService.cs` writing `SaveDTO` to `Application.persistentDataPath/save_slot{N}.json`. Persisted fields: `playerName, day, weekday, hour, minute, hocTap, renLuyen, currentScene, posX/Y/Z, completedQuestIds, missedQuestIds`.

**Use it as-is** — no extension needed for the bot:
- After every successful day's last quest (Sleep), Claude invokes `SaveService.Save(slot=dayNumber)` via MCP (e.g., reflection through ServiceLocator).
- After a bug fix, Claude restarts at 00_Bootstrap → enters play → invokes `SaveService.TryLoad(slot=lastGoodDay)` → jumps directly into the broken day's flow.

**Caveat:** `SaveDTO` does **not** persist `ActiveQuestRSO.current` or quiz-mid-progress. Resume always starts at the beginning of a *day*, not mid-quest. That's fine for our granularity.

## Time Budget

Walking is real-time. Day 1 quest schedule spans 5:00-23:59 in game time. With `Time.timeScale = 3` and `gameHourPerRealMinute = 1/3`, 1 game day = ~8 real minutes. 30 days = 4 hours per full run.

Optimization: between quests, jump time forward via `Clock.SkipTo(nextQuestStart)` instead of waiting full game minutes. Reduces full run to ~30 real minutes.

## Out of Scope (for now)

- NPC schedule verification (NPCs disabled in minimal-visual mode)
- Sentis ONNX dialogue (models detached for GPU stability)
- Subscene-internal logic (LopHoc quiz UI, NhaAn dining, KTX dorm) — bot just enters subscene, waits, returns
- Visual regression (screenshots stored but not diffed against baseline)

## Resume Plan in Next Session

### Day 0 — MCP Capability Discovery (do this BEFORE writing any reactive loop)

Verify each of these against the live UnityMCP bridge. Document any gap.

| Capability | How to test | If missing |
|---|---|---|
| Enter / exit Play mode | `manage_editor action=play` then `action=stop` | Blocker. Cannot proceed. |
| Read GameObject state in Play mode | Find `Player` GameObject, read `transform.position` | Blocker. |
| Read ScriptableObject field at runtime | Resolve `ActiveQuestRSO.current` via ServiceLocator or asset path | Blocker. |
| Tail Unity Console (errors + recent logs) | Trigger a `Debug.LogError` from menu item, read it back | Blocker. |
| Execute arbitrary C# in Play mode | Try `Keyboard.current != null` returns true | If missing → add generic `Assets/Editor/InputInjector.cs` helper (allowed by Constraint 2). |
| Send keyboard `StateEvent` to `Keyboard.current` | Inject W down for 1s, observe Player.transform.position change | If missing → blocker, escalate to user. |
| Invoke `Button.onClick` by GameObject name | Click MainMenu/NewGame button | If missing → fall back to mouse pixel click. |
| Take screenshot for visual sanity | `manage_camera` or similar | Nice-to-have, not blocker. |

### Day 1 onwards

1. Confirm 10_World scene state (baked buildings + minimal visuals applied — last commit `4702d76`)
2. Verify AreaSO worldPos matches scene Area_* positions (already done in commit cd7a60a)
3. Implement reactive loop step-by-step, starting with Day 1 first quest (Tap the duc)
4. After each successful day, invoke `SaveService.Save(slot=dayNumber)` so a bug fix lets us resume from the failing day, not Day 1.
5. Loop fix-test until Day 1 clean
6. Expand to 30 days

### What "no scenario hardcoding" means in practice

When implementing the loop, Claude's prompt context for the loop iteration receives a snapshot of observable state (active quest type, current scene, player pos, time-of-day, last 5 console lines). Claude decides the next *action primitive* (`press_key`, `release_key`, `click_button`, `wait_seconds`, `read_state`). The mapping from quest type → target area is read from `quest.area.worldPos` at runtime, not encoded in Claude's loop prompt. New quests added to the GDD work without changing the loop logic.

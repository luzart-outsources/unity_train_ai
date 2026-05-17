# Claude as Robot Player — Design

**Date:** 2026-05-18
**Goal:** Claude drives the TrainAI game directly through UnityMCP (no test framework code in the project), playing as a user would, observing state, and detecting deviations from the GDD as bugs.

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

## Time Budget

Walking is real-time. Day 1 quest schedule spans 5:00-23:59 in game time. With `Time.timeScale = 3` and `gameHourPerRealMinute = 1/3`, 1 game day = ~8 real minutes. 30 days = 4 hours per full run.

Optimization: between quests, jump time forward via `Clock.SkipTo(nextQuestStart)` instead of waiting full game minutes. Reduces full run to ~30 real minutes.

## Out of Scope (for now)

- NPC schedule verification (NPCs disabled in minimal-visual mode)
- Sentis ONNX dialogue (models detached for GPU stability)
- Subscene-internal logic (LopHoc quiz UI, NhaAn dining, KTX dorm) — bot just enters subscene, waits, returns
- Visual regression (screenshots stored but not diffed against baseline)

## Resume Plan in Next Session

1. Verify UnityMCP reconnects after Claude session restart
2. Confirm 10_World scene state (baked buildings + minimal visuals applied)
3. Verify AreaSO worldPos matches scene Area_* positions (already done in commit cd7a60a)
4. Implement reactive loop step-by-step, starting with Day 1 first quest (Tap the duc)
5. Loop fix-test until Day 1 clean
6. Expand to 30 days

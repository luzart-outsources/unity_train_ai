---
title: Quest System
category: systems
tags: [quest, so, strategy]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Quest System

Polymorphic `QuestSO` base; concrete subclasses cover all GDD quest types. Active quest is tracked in `ActiveQuestRSO`. `IQuestRouter` service evaluates time-based activation, deadlines, and completion.

## Base

```csharp
public abstract class QuestSO : ScriptableObject {
    public string id;
    public string title;
    public AreaSO area;
    public TimeRange window;       // start, deadline (in-game HH:MM)
    public int latePenalty = 5;
    public abstract IQuestRuntime CreateRuntime(QuestContext ctx);
}
```

Runtime instance is plain C# class (`IQuestRuntime`), separate from SO definition (see [[claims#c-20260512-11]]).

## Concrete types

| Concrete | Behavior |
|---|---|
| `GotoConfirmQuestSO` | Walk to area → press E → `UIConfirm` → `SkipTimeInteractionSO` → ±score |
| `QuizQuestSO` | Walk to area → additive load sub-scene → quiz (10 × 4 × 15s) → +score → unload |
| `SceneTransitionQuestSO` | Walk to door → additive load sub-scene → confirm action → unload |
| `SleepQuestSO` | Walk to bed → confirm → `UILoading` → `DayEndedMsg` |
| `FreeRoamQuestSO` | Time marker only, no required action |

Extending = subclass + asset + drag into `DayDB.Days[N].quests`. See [[decisions/d01-so-first-modular]].

## Activation flow

1. `GameClockService.Tick` raises `TimeTickMsg`.
2. `QuestRouter` scans `DayDB[currentDay].quests` for `window.start == now`.
3. Matched → set `ActiveQuestRSO.current` → `Broadcast QuestActivatedMsg`.
4. `QuestArrowHUD` (subscribes) → enable arrow pointing to `quest.area.worldPos`.

## Deadline / miss

`QuestRouter.Tick(dt)` decrements `deadlineRemainingSec`. If ≤ 0 without completion → `QuestMissedMsg`. `LatePenaltyRuleSO` subtracts `quest.latePenalty` from rèn luyện via `ScoreSystem`.

## Gating

`IsInteractableAllowed(InteractableSO i)` returns `i.area == ActiveQuestRSO.current.area`. If false, the interact press is consumed with a toast — per GDD line 61.

## Day rollover

`SleepQuestSO.Complete()` flow:
```
await UIRouter.ShowLoading("Sang ngày hôm sau...", 3f);
Broadcast DayEndedMsg
GameClock.SetTime(tomorrowFirstQuestHour - 30 minutes)
GameClock.IncrementDay (skip Sat+Sun if skipWeekend)
Broadcast DayStartedMsg
QuestRouter.StartDay(newDay)
```

Day 30 + last quest → `Broadcast GameEndedMsg{grade}` → `UIRouter.ShowEnding(grade)`.

## Backlinks
- [[overview]]
- [[entities/player]]
- [[systems/interaction-system]]
- [[systems/score-system]]
- [[decisions/d01-so-first-modular]]

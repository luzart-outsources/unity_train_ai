---
title: Score System
category: systems
tags: [score, gameplay]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Score System

Two scores: **học tập** 0–480 (per [[claims#c-20260512-03]]) and **rèn luyện** 0–100 (start 100, decrement on misses).

## ScoreRuleSO chain

```csharp
public abstract class ScoreRuleSO : ScriptableObject {
    public abstract void Apply(ScoreContext ctx, PlayerStateRSO state);
}
```

`ServiceLocatorSO.scoreRules` is `List<ScoreRuleSO>`. `ScoreSystem.Apply(ctx)` iterates all rules. Each rule may modify state; broadcast `ScoreChangedMsg` at end.

## Concrete rules

| Rule | Triggered by | Effect |
|---|---|---|
| `LatePenaltyRuleSO` | `QuestMissedMsg` | `state.renLuyen -= quest.latePenalty` (default 5) |
| `QuizScoreRuleSO` | `QuizEndedMsg{result}` | `state.hocTap += round(result.percentCorrect × subject.maxPerLesson)` |
| `ExpulsionRuleSO` | `ScoreChangedMsg` | if `state.renLuyen ≤ 0` → `Broadcast ExpelTriggeredMsg` |
| `IdlePenaltyRuleSO` | `IdleTimerMsg{minutes}` | every 15min idle without reaching active quest → `-5 renLuyen` (per GDD line 62) |

## Quiz percent rounding (per [[claims#c-20260512-06]])

GDD line 69 example: 66% → 7 điểm, 92% → 9 điểm. Implementation: `Mathf.RoundToInt(percent * 10)` of a 10-point scale, then scale up to subject `maxPerLesson` (default 40 per lesson, see `world_template.json` example).

## Ending grades

`MilitaryAcademyEndingSO` (concrete `EndingRuleSO`) — per [[claims#c-20260512-05]]:
```csharp
public override EndingGrade Evaluate(PlayerStateRSO s) {
    if (s.hocTap >= 432 && s.renLuyen >= 90) return EndingGrade.XuatSac;
    if (s.hocTap >= 288 && s.renLuyen >= 60) return EndingGrade.Tot;
    return EndingGrade.TrungBinh;
}
```

## Expulsion flow

```
state.renLuyen drops to 0
  → ExpulsionRuleSO triggers Broadcast ExpelTriggeredMsg
  → UIRouter.ShowExpel() → UIConfirm "Bạn bị đuổi học!"
  → user confirms → SceneRouter.LoadSingle(01_MainMenu)
```

## Backlinks
- [[systems/quest-system]]
- [[systems/ui-router]]
- [[decisions/d01-so-first-modular]]

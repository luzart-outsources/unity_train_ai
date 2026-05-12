---
title: D-01 SO-First Modular architecture
category: decisions
tags: [architecture, so, solid]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-01 — SO-First Modular architecture
**Date**: 2026-05-12
**Decided by**: User (Nguyễn Mạnh Quyền + collaborator)
**Status**: active

### Context
User explicitly required "config cực kỳ sâu, mọi thứ SO, code SOLID, mở rộng nhanh nhất". Game has 30 days × 7-8 quests/day × multiple NPCs + AI strategies — modifying core code per content addition is unacceptable.

### Options considered
1. **SO-First Modular + ServiceLocator-via-SO** (chosen) — Strategy SO + GameEvent (or BroadcastService) + ServiceLocatorSO. Designer can config logic in Inspector; extension = new asset + minimal class.
2. **DI-First (VContainer/Zenject)** — strict SOLID code-driven but loses Inspector configurability; adds dependency.
3. **MVC singleton manager** — simplest but every new quest type modifies manager → violates OCP. Rejected by user.

### Decision
**Option 1**. SO is the spine. Polymorphic Strategy SO (`QuestSO`, `InteractionSO`, `MovementStrategySO`, `DialogueStrategySO`, `ScoreRuleSO`, `EndingRuleSO`) sit between data and services. `ServiceLocatorSO` is the single composition root.

### Consequences
- Asset count grows large (~150-300 SO). Mitigated by Phase 11 generator + Editor "Auto-populate" on DBs.
- Strategy SO has no constructor → init in `OnEnable`, runtime state in plain C# (`IQuestRuntime`).
- Designer can swap AI ON ↔ NavMesh fake via Inspector (see [[decisions/d03-strategy-swap-ai]]).

## Backlinks
- [[overview]]
- [[technical/architecture]]
- [[technical/so-taxonomy]]
- [[systems/quest-system]]

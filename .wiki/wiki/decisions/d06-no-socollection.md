---
title: D-06 No SoCollection; plain List<T> databases
category: decisions
tags: [so, database]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-06 — Do not use NullTale SoCollection; plain `List<T>` DB SO instead
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
SoCollection auto-aggregates assets in a folder. User chose simpler approach: explicit `List<T>` in DB SO + Editor context menu "Auto-populate from folder".

### Decision
`QuestDB`, `NPCDB`, `InteractableDB`, `QuizDB`, `AreaDB`, `DayDB`, `SubjectDB` all inherit `ScriptableObject` and expose `public List<T> all`. Editor-only `[ContextMenu]` method scans `AssetDatabase.FindAssets("t:<T>")` to populate.

### Consequences
- One less plugin dependency.
- Slightly more friction for designer (need to remember Auto-populate). Acceptable trade-off.
- DB SO is the single authoritative reference in `ServiceLocatorSO`.

## Backlinks
- [[technical/so-taxonomy]]
- [[technical/architecture]]

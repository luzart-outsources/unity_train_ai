---
title: D-10 JSON save file; never persist SO asset
category: decisions
tags: [save, persistence]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-10 — Save = JSON DTO in persistentDataPath; never serialize SO assets
**Date**: 2026-05-12
**Decided by**: tech-lead
**Status**: active

### Context
ScriptableObject assets are tempting save targets, but Editor Play-mode writes can corrupt asset files on disk. Need decoupled persistence.

### Decision
- Save format: JSON DTO at `Application.persistentDataPath/save_slot{N}.json`.
- Schema version field for migration (`SaveMigrator.Migrate(from, to, JObject)`).
- Auto-save on `DayEndedMsg`. Save contains: playerName, day/hour/minute, scores, currentScene, playerPos, completed/missed quest IDs (by string ID, not asset ref).
- RSO state reset on `OnEnable`; restored from JSON via `SaveService.Load`.

### Consequences
- Runtime state never touches SO definition files — no risk of corrupting `_Data/` asset.
- Quest references stored as string IDs → robust against asset rename if ID kept stable.
- Forward-compatible: new fields default-initialized when older save read.

## Backlinks
- [[systems/save-load]]
- [[technical/so-taxonomy]]

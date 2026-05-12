---
title: D-03 Strategy SO for AI swap (ONNX ↔ NavMesh ↔ canned)
category: decisions
tags: [ai, strategy, onnx]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-03 — MovementStrategySO + DialogueStrategySO swap
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
User: "Đã có các file ONNX rồi, có thể sử dụng các file đó không, hoặc là có option fake thì dùng NavMesh cũng được, dùng strategy để đổi." Required: AI thật và fake same field, hot-swap qua Inspector.

### Options considered
1. **MovementStrategySO + DialogueStrategySO polymorphic** (chosen). NPC asset has a strategy field; swap = change asset reference.
2. Pure ML-Agents PPO retrain — rejected, overkill, deadline tight.
3. Pure NavMesh + canned chat — rejected, doesn't satisfy "ONNX cho chat và pathfinding".

### Decision
**Option 1**. `OnnxMovementSO` / `NavMeshMovementSO` / `WaypointMovementSO` / `IdleMovementSO` all inherit `MovementStrategySO`. Same for dialogue: `OnnxDialogueSO` / `CannedDialogueSO` / `MockDialogueSO`.

### Consequences
- 1-click swap for QA: build for thầy = NavMesh; build for demo = ONNX.
- All strategies must satisfy same `IMovementAgent` / `DialogueStrategySO.Reply` interface — no leaky abstractions.
- ONNX strategies depend on `ISentisRuntime` — passed in via `NpcContext`.

## Backlinks
- [[systems/npc-movement]]
- [[systems/npc-dialogue]]
- [[entities/npc-student]]
- [[entities/npc-commander]]

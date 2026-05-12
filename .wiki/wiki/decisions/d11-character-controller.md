---
title: D-11 CharacterController for kinematic agents; no Rigidbody for NPC ONNX
category: decisions
tags: [physics, movement, onnx]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/soldier.meta.json]
created: 2026-05-12
updated: 2026-05-12
---

## D-11 — `CharacterController` for Player and OnnxMovement NPC
**Date**: 2026-05-12
**Decided by**: tech-lead
**Status**: active

### Context
`soldier.onnx` was trained on a kinematic agent (direct position/rotation updates, no Rigidbody forces). Using `Rigidbody` + `AddForce` at runtime would create sim-to-real mismatch — the policy would behave erratically.

### Decision
- Player movement: `CharacterController.Move()`.
- NPC OnnxMovement: `CharacterController.Move()` after applying thrust/turn outputs from policy.
- NPC NavMeshMovement: `NavMeshAgent` (has its own kinematic controller; OK).
- No `Rigidbody` on player or AI-driven NPC.

### Consequences
- Gravity applied manually: `transform.position += Vector3.down * gravity * dt`.
- Collisions handled via `CharacterController` capsule sweep, not physics solver.
- Doors/cubes can be regular `BoxCollider` (static). Trigger zones use `isTrigger = true`.

## Backlinks
- [[entities/player]]
- [[entities/npc-student]]
- [[systems/npc-movement]]

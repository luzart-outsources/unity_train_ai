---
title: D-05 Third-person 3D with quest arrow under player
category: decisions
tags: [camera, arrow]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-05 — Third-person 3D, quest arrow under player feet
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
User: "Game sẽ theo góc nhìn 3D, mình nhìn người chơi di chuyển như các game trên thị trường... bên dưới chân player sẽ gán 1 cái gameobject có dạng mũi tên để chỉ đường."

### Decision
- Camera: third-person rig parent player. Camera at local `(0, 1.6, -3.5)`, smooth-damp follow (0.1s).
- Look input rotates yaw rig; pitch clamped `[-20°, 50°]`.
- Quest arrow: prefab child of player at local `(0, 0.05, 0.4)`. `arrow.LookAt(targetXZ)` each frame. Bobbing + scale pulse.
- `ThirdPersonCameraConfigSO` for tunable distance/height/speed.

### Consequences
- Player rotates to face camera yaw when moving (`Move.magnitude > 0.1`).
- Quest arrow hides when `ActiveQuest == null` (free roam).
- Replaces the deleted `quest arrow under player feet` work from commit `e848d23` per user resync after `chore: wipe TrainAI`.

## Backlinks
- [[entities/player]]
- [[systems/quest-system]]

---
title: NPC — Học sinh (Student)
category: entities
tags: [npc, movement, onnx, schedule]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/soldier.meta.json]
created: 2026-05-12
updated: 2026-05-12
---

# NPC Học sinh (Student)

Mobile NPC. Follows a `ScheduleSO` — moves between areas at given hours, mimicking the player's day-cycle. Uses `OnnxMovementSO` by default (swap to `NavMeshMovementSO` for fake mode).

## NPCSO config (sample)

```yaml
id: HocSinh_01
displayName: "Học sinh 1"
spawnPos: (-3, 0, 2)
movement: OnnxMovementSO
dialogue: CannedDialogueSO  (or none — student doesn't talk by default)
schedule: Schedule_HocSinh_01
```

## Schedule template (per GDD line 89)

```
05:00 → Area_SanVanDong
06:00 → Area_DonVeSinh
07:00 → Area_NhaAn_Door     (joins meal — wait at door)
07:30 → Area_LopHoc_Door
11:30 → Area_FreeArea       (lunch/nghỉ trưa)
14:00 → Area_LopHoc_Door
17:00 → Area_FreeArea
18:30 → Area_KTX_Door
```

## Movement contract

Uses `OnnxMovementSO` with:
- `soldier.onnx` policy (21D obs, 2D action — see [[sources/soldier-meta]] + [[claims#c-20260512-08]]).
- `CharacterController.Move` apply per [[decisions/d11-character-controller]].
- Tick at 5Hz batched in `MovementService`.
- Layer setup: `Obstacle` layer mask for 8-ray raycasts.

## Swap to fake

Open `NPC_HocSinh_01.asset` → field `movement` change `MovementOnnx.asset` → `MovementNavMesh.asset`. NPC immediately switches to `NavMeshAgent.SetDestination(target)`.

## Visibility

Capsule placeholder gray (per [[technical/placeholder-scale]] § color code suggestion; designer overrides per prefab — [[decisions/d07-manual-prefab-override]]).

## Backlinks
- [[systems/npc-movement]]
- [[systems/sentis-runtime]]
- [[decisions/d03-strategy-swap-ai]]
- [[decisions/d11-character-controller]]

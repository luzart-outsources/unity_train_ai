---
title: Player Entity
category: entities
tags: [player, character, third-person, arrow]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Player

Third-person 3D character. Capsule placeholder (`0.6 × 1.8 × 0.6` per [[technical/placeholder-scale]]).

## Composition

```
Player (GameObject)
├── CharacterController (kinematic, per [[decisions/d11-character-controller]])
├── Collider trigger (radius 2m, layer "PlayerInteractRange")
├── PlayerController (script)
├── PlayerInteractor (script — trigger detection + InteractPressedMsg)
├── CameraRig (empty)
│   └── Main Camera (local (0, 1.6, -3.5))
├── QuestArrow (child, local (0, 0.05, 0.4))
│   └── ArrowMesh + EmissiveMaterial
└── PlayerVisual (capsule mesh + material)
```

## Movement

- Input System actions: `Move (Vector2)`, `Look (Vector2)`, `Interact (Button)`, `MenuToggle (Button)`.
- `Move.magnitude > 0.1` → player rotates to face camera yaw → CharacterController moves at speed defined in `PlayerConfigSO`.
- `Look` rotates camera rig yaw, pitch clamped `[-20°, 50°]`.

## Quest arrow

- Always visible while `ActiveQuestRSO.current != null`.
- Each frame: `arrow.LookAt(new Vector3(target.x, transform.position.y, target.z))`.
- Bobbing via AnimationCurveSO (y-sin 0.05m), scale pulse 0.95↔1.05.
- Hides on `QuestCompletedMsg`/`QuestMissedMsg`, re-appears on next `QuestActivatedMsg`.

Per [[decisions/d05-third-person-3d]].

## State

Stored in `PlayerStateRSO`:
- `playerName`
- `hocTap` (int)
- `renLuyen` (int)
- `currentScene`
- `lastWorldPos` (saved on `DayEndedMsg`)

## Backlinks
- [[overview]]
- [[systems/quest-system]]
- [[systems/interaction-system]]
- [[decisions/d05-third-person-3d]]
- [[decisions/d11-character-controller]]

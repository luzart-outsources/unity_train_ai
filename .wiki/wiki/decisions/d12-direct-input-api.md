---
title: D-12 Direct InputSystem API (no InputActionReference assets)
category: decisions
tags: [decision, input-system, runtime]
sources: []
created: 2026-05-13
updated: 2026-05-13
---

## D-12: Direct InputSystem API (no InputActionReference assets)

**Date**: 2026-05-13
**Decided by**: User (via playtest report "Input system error")
**Status**: active

### Context

`PlayerController`, `ThirdPersonCameraRig`, `PlayerInteractor` originally used `[SerializeField] InputActionReference moveAction/lookAction/interactAction` fields. This is the canonical Unity-recommended pattern: an `.inputactions` asset defines actions, the editor wires the asset to scene component refs.

Problem during MCP Play test:
- No `.inputactions` asset exists in `Assets/` (would need to be authored + assets imported).
- SceneBuilder programmatic asset generation cannot easily author a Unity `.inputactions` file (it is a special asset type with bindings, control schemes, etc.; not a plain SO).
- Therefore the `InputActionReference` fields were always null at runtime → keys did nothing → user reported the game is broken.

### Options considered

| Option | Pros | Cons |
|---|---|---|
| **A**. Author `.inputactions` asset + wire refs in SceneBuilder | Idiomatic Unity. Rebindable. | Cannot easily generate via Editor scripts; needs hand-authored XML/JSON asset. |
| **B**. Direct API: `Keyboard.current.wKey.isPressed`, `Mouse.current.delta` | Zero scene-wiring needed. Just code. Survives master-builder regen idempotently. | Lose rebinding UI. |
| **C**. Use legacy `Input.GetKey` | Even simpler. | Project uses new InputSystem; mixing causes conflicts. Already rejected previously. |

### Decision

**Option B**: Use `UnityEngine.InputSystem.Keyboard.current` / `Mouse.current` / `Gamepad.current` directly in `Update()`. No scene-side wiring.

### Consequences

- **Good**: SceneBuilder doesn't need to author/wire `.inputactions`. New scene works immediately. Verified in MCP Play test (Keyboard.current alive, 2 devices, Move/Look/E all functional).
- **Tradeoff**: No automatic key-rebind UI. If players want to remap keys, must implement own rebinding screen (Phase 13 polish or beyond).
- **Mobile**: Touch input still works via `Mouse.current` (Unity translates touch to pointer on mobile InputSystem). Joystick Pack (see [[decisions/d14-joystick-pack]]) provides explicit virtual joystick.
- **Files affected**: [[systems/player-controller]], [[systems/camera-rig]], [[systems/interaction-system]].

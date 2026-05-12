---
title: D-14 Use existing Joystick Pack asset for mobile joystick
category: decisions
tags: [decision, ui, mobile, joystick]
sources: []
created: 2026-05-13
updated: 2026-05-13
---

## D-14: Use existing Joystick Pack asset for virtual joystick

**Date**: 2026-05-13
**Decided by**: User ("dùng hệ thống joystickpack trong game đi")
**Status**: planned (not yet integrated)

### Context

Project already has `Assets/Joystick Pack/` imported (Unity Asset Store - 5 prefab variants: FixedJoystick, FloatingJoystick, DynamicJoystick, VariableJoystick + base `Joystick` MonoBehaviour). Types live in `Assembly-CSharp` (no asmdef — global namespace).

Earlier we hand-rolled a tiny `UIJoystickController` (drag handler + Vector2 output). It existed but wasn't wired into `PlayerController.ReadMove()`, so it had no gameplay effect.

GDD §UI: "Joystick di chuyển (góc dưới bên trái)" — explicitly required as part of the gameplay HUD per spec.

### Options considered

| Option | Pros | Cons |
|---|---|---|
| **A**. Keep hand-rolled `UIJoystickController` | Lives in `TrainAI.UI` asmdef. Owned by us. | Reinvents wheel. Less polish (no snap/deadzone/variant). |
| **B**. Use Joystick Pack types directly from `Assembly-CSharp` | Battle-tested, multiple variants. | Cross-asmdef call: `TrainAI.Presentation` (PlayerController) must reference `Assembly-CSharp` or wrap. |
| **C**. Wrap Joystick Pack `Joystick` in a `IJoystickInput` abstraction in `TrainAI.Core` so PlayerController depends only on interface | Clean asmdef direction. Swappable. | One extra adapter class. |

### Decision

**Option C** (when integrating): Define `IJoystickInput { Vector2 Direction { get; } }` in `TrainAI.Core`. Adapter `JoystickPackBridge : MonoBehaviour, IJoystickInput` lives in `TrainAI.UI`, references Joystick Pack's `Joystick` via `Assembly-CSharp` ref. PlayerController.ReadMove falls back to `IJoystickInput.Direction` when keyboard input is zero.

Pending: wire `Assembly-CSharp` reference into `TrainAI.UI.asmdef` (or move Joystick Pack into an `.asmdef`-scoped folder to keep cross-asmdef dependency explicit).

### Consequences

- **Good**: Mobile input handled by polished joystick variants from the pack. PlayerController stays platform-agnostic via interface.
- **Pending work**: implement `JoystickPackBridge`, wire UI ref into 00_Bootstrap UICanvas (FixedJoystick prefab at bottom-left), update `PlayerController.ReadMove` to compose keyboard + joystick.
- **Replaces**: `UIJoystickController` (our hand-rolled version) — can be removed once bridge is wired.

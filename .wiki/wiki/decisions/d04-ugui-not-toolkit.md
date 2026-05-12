---
title: D-04 uGUI not UI Toolkit
category: decisions
tags: [ui]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-04 — uGUI (Canvas + RectTransform + TMP), not UI Toolkit
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
Two UI stacks available: uGUI (mature, Inspector-driven) vs UI Toolkit (newer, UXML/USS).

### Options considered
1. **uGUI** (chosen) — user preference, simpler for popup-heavy game, fits Quiz/Confirm/Dialogue prefab pattern.
2. UI Toolkit — better for complex panels but learning curve, less direct prefab workflow.

### Decision
uGUI. All popups are prefabs gắn `UIScreenBase`. UIRouter instantiates on demand. `OverlayCanvas` in BootstrapScene with `DontDestroyOnLoad`.

### Consequences
- Joystick UI uses native uGUI Image + EventTrigger.
- HUD layout via anchors (top-left score, top-center clock, top-right minimap, bottom-left joystick, bottom-right interact button).
- TextMeshPro for Vietnamese diacritics.

## Backlinks
- [[systems/ui-router]]

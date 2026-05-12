---
title: D-07 Manual prefab override; no PlaceholderRegistrySO
category: decisions
tags: [art, placeholder]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-07 — Per-prefab material override, no central registry
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
Initial design proposed `PlaceholderRegistrySO` (one SO holding shared materials/meshes for player/NPC/building/interactable). User rejected: "Không cần PlaceholderRegistrySO, tôi muốn sửa prefabs thủ công."

### Decision
- Each prefab (Player, NPC capsules, building cubes, interactable cubes) carries its own material assignment.
- Designer overrides material on prefab when real 3D model is ready.
- Section 11.2 color code is a *suggestion* in the spec, not centrally enforced.

### Consequences
- Designer must manually update each prefab when art lands — acceptable since prefab count is small (~10).
- No risk of registry SO becoming a fragile bottleneck.
- Phase 11 PrefabBuilder generates prefabs with placeholder material baked-in; designer can `[Apply Overrides]` per prefab.

## Backlinks
- [[technical/placeholder-scale]]
- [[entities/player]]
- [[entities/npc-commander]]
- [[entities/npc-student]]

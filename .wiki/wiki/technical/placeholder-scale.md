---
title: Placeholder Scale Convention
category: technical
tags: [art, placeholder, cube]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Placeholder Scale Convention

All world objects use Unity primitive meshes (cube, capsule, plane) with the scales below. Material/mesh override is per-prefab manually, per [[decisions/d07-manual-prefab-override]].

## Scale table (meters)

| Object | Mesh | Scale (X, Y, Z) | Note |
|---|---|---|---|
| Player | capsule | 0.6 × 1.8 × 0.6 | human proportion |
| NPC (any) | capsule | 0.6 × 1.8 × 0.6 | |
| Lớp học building | cube | 12 × 5 × 8 | door cube inset |
| KTX building | cube | 15 × 4 × 10 | |
| Nhà ăn building | cube | 14 × 4.5 × 9 | |
| Sân vận động ground | plane | 25 × 0.1 × 15 | trigger area on top |
| Đường (path tile) | plane | 2 × 0.05 × N | tileable, N variable |
| Cửa (interactable) | cube | 1.5 × 2.5 × 0.5 | + sphere trigger r=2m |
| Bàn lớp học | cube | 1.2 × 0.8 × 0.6 | + trigger for interact |
| Giường KTX | cube | 0.9 × 0.4 × 2.0 | + trigger for sleep |
| Bục giảng | cube | 1.5 × 1.0 × 0.8 | static |
| Quest arrow | cone + cylinder | total ~0.5m tall | emissive material |

## Color code (debug suggestion only)

When you instantiate placeholder via `PrefabBuilder`, assign these materials. Override on prefab when real art lands:

```
Player              xanh dương  (#3A6FE0)
NPC chỉ huy         đỏ          (#D63A3A)
NPC học sinh        xám         (#888888)
Interactable cửa    vàng        (#E8C547)
Quest area floor    xanh lá glow (#3AD66B emissive)
Off-limit area      đen mờ       (#222222 alpha 0.5)
Building wall       xám nhạt    (#BBBBBB)
Path                xám trung   (#666666)
```

## Why no shared registry

User decision [[decisions/d07-manual-prefab-override]] — designer prefers Inspector override per prefab over central SO registry.

## Backlinks
- [[decisions/d07-manual-prefab-override]]
- [[entities/player]]
- [[entities/npc-commander]]
- [[entities/npc-student]]

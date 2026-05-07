---
title: Soldier NPC
category: entities
tags: [npc, soldier, movement, ppo]
sources: [raw/gdd/context_v2.md]
created: 2026-05-07
updated: 2026-05-07
---

# Soldier NPC (Lính tự đi)

NPC nền trong doanh trại — lính tự đi từ điểm xuất phát đến điểm đích theo lịch (đi từ ktx đến nhà ăn, từ giảng đường đến sân tập...). Khi đi tự tránh chướng ngại (cây, cột, hộp đồ).

## Visual

Cube có chiều cao ~1.8m (hoặc model lính nếu Quyền có asset). Train trên cube simple — visual upgrade chỉ thay mesh, không ảnh hưởng AI.

## Movement model

Phụ thuộc [[systems/movement-ai]]. Component `MovementAgent.cs` đặt trên GameObject lính:

```
FixedUpdate (~50Hz):
    if (timer > 0.1s):  # decision rate 10Hz
        obs = ComputeObservation()  # 21 floats
        action = onnx.Run(obs)       # 2 floats
        ApplyKinematic(action, dt=0.1s)
```

## Inputs from Unity

| Input | Source |
|---|---|
| 8 raycasts | `Physics.Raycast` lên 2 layer: Obstacle + Target |
| Velocity | track manually (Δposition / dt) hoặc Rigidbody.velocity |
| Target ref | drag `Transform target` vào Inspector |

## Schedule (game logic — Quyền lo)

NPC schedule là external state, không phải AI:

```
{
  "06:00": "depart from KTX → goto courtyard",
  "07:00": "depart from courtyard → goto canteen",
  "11:30": "depart from class → goto canteen",
  ...
}
```

Khi tới giờ kế tiếp, `target` của agent đổi sang vị trí mới → AI policy lo phần "đi tới đó". Không cần retrain.

## Edge cases lo trước

> [!bug] Hành lang quá hẹp
> Train env arena 15-25m, obstacle gap ≥ 2m. Doanh trại thật có hành lang 1.5m → có thể agent kẹt. Mitigation: vòng 2 train với obstacle dày hơn (chưa làm).

> [!warning] Multiple agents cùng đường
> Nếu 2 lính cùng tới 1 điểm cùng lúc, observation không có "thấy nhau" → có thể đụng nhau. Mitigation: ngắn hạn = stagger schedule, dài hạn = thêm raycast cho NPC layer.

## Backlinks

- [[overview]]
- [[systems/movement-ai]]
- [[technical/unity-integration]]

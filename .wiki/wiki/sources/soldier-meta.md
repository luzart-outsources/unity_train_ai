---
title: Source — Soldier Meta
category: sources
tags: [onnx, movement, ppo]
source_path: AI_Training/deliverables/soldier.meta.json
created: 2026-05-12
updated: 2026-05-12
---

# Source — soldier.meta.json

**Path**: `AI_Training/deliverables/soldier.meta.json`
**Type**: Observation + action contract for `soldier.onnx` (PPO, Phase B winner).

## Full contract

```json
{
  "obs_dim": 21,
  "act_dim": 2,
  "opset": 15,
  "input_name": "obs",
  "output_name": "action",
  "obs_layout": [
    "ray_dist[8]",
    "ray_hit_target[8]",
    "vel_forward",
    "vel_lateral",
    "dir_to_target_fwd",
    "dir_to_target_lat",
    "dist_to_target"
  ],
  "action_layout": ["thrust", "turn"],
  "ray_max_dist": 10.0,
  "max_speed": 3.5,
  "max_turn_rad_per_sec": 3.14159265,
  "arena_size": 20.0
}
```

## Implication for runtime

`OnnxMovementSO` (see [[systems/npc-movement]]):
- Cast 8 raycasts on XZ-plane, max 10m, layer mask = "Obstacle".
- For each ray, also test against tag "Target" → `ray_hit_target[i] = 1` if hit; else 0.
- Compute velocity in local frame: `vel_forward = Dot(velocity, forward)/3.5`.
- Normalize `dist_to_target = clamp(dist, 0, 20) / 20`.
- Apply actions: `thrust ∈ [-1,1]` (forward speed), `turn ∈ [-1,1]` (yaw rate × π rad/s).

## Out-of-domain risk

Arena was 20m during training; schedule travel may exceed → policy behavior undefined. Mitigation: chunked target (move via waypoints inside arena radius) or switch to NavMesh for long traversal. See [[open-questions#q-20260512-05]].

## Cited from this source

- [[claims#c-20260512-08]] obs/action layout + parameters

## Backlinks
- [[systems/npc-movement]]
- [[sources/merged-report]]

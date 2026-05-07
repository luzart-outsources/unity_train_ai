---
title: Movement AI (Phase B)
category: systems
tags: [phase-b, ppo, reinforcement-learning, navigation, onnx]
sources: [raw/gdd/context_v2.md, raw/technical/handoff_morning.md, raw/technical/phase_b_runs.csv]
created: 2026-05-07
updated: 2026-05-07
---

# Movement AI — Phase B Soldier Navigation

Lính NPC tự đi từ vị trí hiện tại đến target, vòng tránh các obstacle (cube). Train hoàn toàn Python (PyTorch + Stable-Baselines3 PPO + Gymnasium); Unity chỉ load ONNX qua [[technical/unity-integration|InferenceEngine]] để inference.

## Vì sao KHÔNG dùng ML-Agents toolkit

Quyết định lớn: [[decisions/standalone-ppo-not-ml-agents]]. TL;DR — ML-Agents YÊU CẦU Unity Editor mở suốt quá trình train (5–15h). User muốn "thông đêm tự train", không thể realistic với ML-Agents. Standalone Python PPO chạy độc lập, output ONNX vẫn nạp được vào Unity 6.

## Environment (training)

2D top-down arena, randomize per episode để policy generalize:

| Param | Range (random) | Default cũ |
|---|---|---|
| `arena_size` | 15.0 – 25.0 m | 20.0 |
| `num_obstacles` | 3 – 12 | 6 |
| `obstacle_size` | 1.0 – 2.5 m | 1.5 |
| `agent_radius` | 0.5 (fixed) | 0.5 |
| `target_radius` | 0.7 (fixed) | 0.7 |
| `dt` | 0.1 s (10 Hz) | 0.1 |

> [!info] Random vs Fixed
> v2 dùng fixed 6 obstacles → policy học pattern cụ thể, score cao trên benchmark gốc nhưng có thể yếu ngoài. v3 random hóa → score thấp hơn trên benchmark cũ nhưng generalize tốt hơn. Không thể so sánh trực tiếp 2 score. Xem `deliverables/soldier_v2_fixedenv.onnx` vs `soldier.onnx`.

## Observation contract — 21 floats

Đây là **CONTRACT** giữa training và Unity. Quyền phải tạo raycast trong Unity với cùng layout này.

```
[0..7]   8 ray distances normalized / ray_max_dist (10m)
            8 hướng evenly 360°, index 0 = forward, CCW
[8..15]  8 ray hit-target one-hot (1.0 nếu ray trúng target)
[16]     velocity_forward (agent frame) / max_speed (3.5)
[17]     velocity_lateral (agent frame, +right) / max_speed
[18]     dir-to-target forward (cos angle in agent frame)
[19]     dir-to-target lateral (sin angle)
[20]     distance-to-target / arena_diagonal (~28.28m at arena_max=25)
```

Code reference Unity: `deliverables/MovementAgent.cs::ComputeObservation()`.

## Action — 2 continuous floats

```
[0] thrust  ∈ [-1, 1]   (forward; -0.5..1 effective, reverse runs at 0.5x)
[1] turn    ∈ [-1, 1]   (right positive, scaled by max_turn = π rad/s)
```

Output ONNX qua tanh → đảm bảo Unity luôn nhận trong [-1, 1].

## Reward shaping

```
+1.0     reach target (terminate)
+0.5*Δd  progress (positive nếu gần lại target step này)
-0.0005  per step (chống đứng yên)
-0.1     collision với obstacle (slide, không terminate)
-1.0     out-of-bounds (terminate)
-0.5     timeout 500 steps
```

Reward range typical: −5 (random policy) đến +10 (perfect policy reach target trong ít bước).

## Training history

Lưu ở `phase_b_movement/logs/training_runs.csv`. Tóm tắt qua các phase:

| Run version | Steps | Net | Env | Best mean_reward |
|---|---|---|---|---|
| v2 (fixed env) | 200k × 30 runs | [64,64] | fixed 6 obstacles | **6.126** (iter 8) |
| v3 (random env) | 500k × 5 runs | [128,128] | random 3-12 | 6.050 (iter 2) |
| **v3.1 (current)** | 1M × ongoing | [128,128] | random 3-12 | TBD (loop chạy) |

> [!tip] PPO plateau pattern
> 200k steps → ~6.0 reward (giai đoạn exploration xong). 500k → 6.0–6.1 (refinement chưa rõ rệt). 1M → kỳ vọng 7–9. PPO trên env này có ceiling ~10 (perfect agent + lucky episode).

## ONNX contract (Quyền cần biết để load)

```
Model file        : deliverables/soldier.onnx (v3.1 random env, [128,128])
                    deliverables/soldier_v2_fixedenv.onnx (v2 backup, fixed 6, [64,64])
Meta file         : deliverables/soldier.meta.json
Input  "obs"      : float tensor shape [batch=1, 21]
Output "action"   : float tensor shape [batch=1, 2], tanh-squashed
Decision rate     : 10 Hz (gọi mỗi 0.1s tron Unity FixedUpdate)
```

C# code mẫu: `deliverables/MovementAgent.cs`. Chi tiết: [[technical/unity-integration]].

## Backlinks

- [[overview]]
- [[entities/soldier-npc]] — consumer
- [[technical/training-pipeline]]
- [[technical/unity-integration]]
- [[decisions/standalone-ppo-not-ml-agents]]

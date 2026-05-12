---
title: NPC Movement System
category: systems
tags: [npc, movement, onnx, sentis, strategy]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/soldier.meta.json, AI_Training/deliverables/MovementAgent.cs]
created: 2026-05-12
updated: 2026-05-12
---

# NPC Movement System

`MovementStrategySO` polymorphism + `IMovementService` batch driver + Sentis `Worker` shared from runtime. Per [[decisions/d03-strategy-swap-ai]].

## Strategy SO

```csharp
public abstract class MovementStrategySO : ScriptableObject {
    public abstract IMovementAgent Bind(Transform npc);
}
```

| Concrete | Behavior |
|---|---|
| `OnnxMovementSO` | `soldier.onnx` policy, 21D obs → 2D action |
| `NavMeshMovementSO` | `NavMeshAgent.SetDestination(target)` |
| `WaypointMovementSO` | Lerp between `List<Vector3>` waypoints |
| `IdleMovementSO` | No movement (Đại đội trưởng) |

## OnnxMovementSO contract (per soldier.meta.json)

- **Obs (21D)** normalized:
  - `[0..7]` ray_dist[8] / 10  (8 raycast XZ-plane, max 10m, see [[claims#c-20260512-08]])
  - `[8..15]` ray_hit_target[8] ∈ {0, 1} (tag "Target")
  - `[16]` vel_forward / 3.5
  - `[17]` vel_lateral / 3.5
  - `[18]` dir_to_target_fwd ∈ [-1, 1]
  - `[19]` dir_to_target_lat ∈ [-1, 1]
  - `[20]` clamp(distance, 0, 20) / 20
- **Action (2D)**: `thrust ∈ [-1, 1]`, `turn ∈ [-1, 1]`.
- **Apply** (per [[decisions/d11-character-controller]]):
  ```
  transform.Rotate(0, turn * π * Mathf.Rad2Deg * dt, 0)
  characterController.Move(transform.forward * thrust * 3.5 * dt + gravity * dt)
  ```

## MovementService

```csharp
public class MovementService : IMovementService {
    Dictionary<Transform, IMovementAgent> _agents;
    float _tickAccum = 0; const float TickHz = 5;

    public void RegisterAgent(Transform npc, MovementStrategySO s)
        => _agents[npc] = s.Bind(npc);
    public void SetTarget(Transform npc, Vector3 t)
        => _agents[npc].Target = t;
    public void Tick(float dt) {
        _tickAccum += dt;
        if (_tickAccum < 1f/TickHz) return;
        _tickAccum = 0;
        foreach (var (npc, agent) in _agents)
            agent.Tick(_sentis.SoldierWorker, 1f/TickHz);
    }
}
```

5Hz throttle keeps total inference cost manageable (~10 NPC × 5Hz × 3ms = 150ms/s). Higher-frequency physics interpolation handled by `CharacterController` integration during render frames if needed.

## Schedule-driven targets

[[entities/npc-student]] has a `ScheduleSO` with `List<{time, area}>`. `NPCDirector` (subscribes `TimeTickMsg`):
```
foreach npc in NPCDB:
   entry = npc.schedule.GetEntryAt(day, hour, minute)
   if entry.area != npc.currentTargetArea:
       MovementService.SetTarget(npc.transform, entry.area.worldPos)
```

## Tensor lifecycle (Sentis)

Each tick:
```
using var inT = new Tensor<float>(new TensorShape(1, 21), obs);
worker.Schedule(inT);
using var outT = (Tensor<float>) worker.PeekOutput();
```
`using` enforces disposal — leaking causes GPU memory exhaustion (see [[technical/sentis-runtime-notes]]).

## Performance budget

| Metric | Target |
|---|---|
| Inference cost / NPC | 2–5 ms GPU |
| Raycast cost / NPC / tick | 8 raycasts, <0.1 ms |
| Total NPC budget @ 5Hz | < 30 ms / tick × 5 = 150 ms/s |

## Fallback strategy

If NPC > 30 → switch most to `NavMeshMovementSO`; reserve ONNX for ~5-10 "showcase" NPC. See [[open-questions#q-20260512-05]].

## Backlinks
- [[entities/npc-student]]
- [[systems/sentis-runtime]]
- [[decisions/d03-strategy-swap-ai]]
- [[decisions/d09-sentis-per-model-worker]]
- [[decisions/d11-character-controller]]

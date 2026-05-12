---
title: D-09 Sentis Worker per model; GPUCompute fallback CPU
category: decisions
tags: [sentis, onnx, performance]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/MERGED_REPORT.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-09 — One Sentis `Worker` per ONNX model; backend = GPUCompute, CPU fallback
**Date**: 2026-05-12
**Decided by**: tech-lead (collaborator)
**Status**: active

### Context
Two ONNX models in the game: `intent_classifier.onnx` (LSTM, called rarely per dialogue) and `soldier.onnx` (PPO, called at 5Hz per NPC). They have different I/O shapes — can't share `Worker`.

### Options considered
1. One shared Worker — impossible due to different shapes.
2. **Worker per model** (chosen) — `IntentWorker` + `SoldierWorker` instantiated once in `SentisRuntime`, disposed on quit.
3. Worker per NPC — wasteful, 10× memory.

### Decision
- `SentisRuntime` exposes `IntentWorker` and `SoldierWorker`.
- Backend preference: `BackendType.GPUCompute`. Fallback `BackendType.CPU` if Compute Shaders unsupported. Configured in `GameConfigSO.preferredBackend`.
- Worker initialization ~50-100ms each → done once at Bootstrap, never during gameplay.

### Consequences
- All Strategy SO that need inference receive a `Worker` reference via context, do not create their own.
- Soldier inference must be batched at fixed 5Hz (throttle in `MovementService.Tick`) to fit budget ~150ms/s total.
- Tensor disposal discipline: every inference must `using` its input + output tensors → otherwise GPU memory leak.

## Backlinks
- [[systems/sentis-runtime]]
- [[systems/npc-movement]]
- [[systems/npc-dialogue]]
- [[technical/sentis-runtime-notes]]

---
title: Sentis Runtime Notes
category: technical
tags: [sentis, onnx, performance, gotcha]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/MERGED_REPORT.md]
created: 2026-05-12
updated: 2026-05-12
---

# Sentis Runtime Notes

Gotchas + best practices for `com.unity.ai.inference 2.6.1` (formerly "Sentis", now "Unity AI Inference Engine" in Unity 6).

## API summary

```csharp
using Unity.InferenceEngine;
// formerly: using Unity.Sentis;
ModelAsset asset;                    // imported .onnx
Model model = ModelLoader.Load(asset);
Worker worker = new Worker(model, BackendType.GPUCompute);
using var input = new Tensor<float>(new TensorShape(1, 21), data);
worker.Schedule(input);
using var output = (Tensor<float>) worker.PeekOutput();
float[] result = output.DownloadToArray();
worker.Dispose();
```

## Gotchas

1. **Tensor disposal**: every tensor MUST be `using`-disposed. Leak → GPU memory exhaustion → editor crashes or undefined runtime behavior.
2. **`PeekOutput()` returns shared reference**: don't store; copy to `float[]` if you need persistence. Disposing the worker invalidates peeks.
3. **Backend support**: `GPUCompute` requires Compute Shaders. Android low-end + WebGL fallback to `CPU`. Configurable in `GameConfigSO.preferredBackend`.
4. **Worker init cost** ~50-100ms per Worker → instantiate once at Bootstrap, never mid-game.
5. **Shape dynamism**: LSTM `intent_classifier.onnx` has fixed `(1, 32)` input. For variable seq_len, would need dynamic axis support in export. Current model = pad to 32 always.
6. **Soldier policy is kinematic**: per [[decisions/d11-character-controller]], use `CharacterController`, not Rigidbody — policy was trained on kinematic agent.

## Performance budget

- Intent: <30 ms per inference, runs once per player message — fine.
- Soldier: 2-5 ms per inference per NPC × 5Hz × 10 NPC ≈ 150 ms/s = 2.5 ms / frame at 60 FPS. Fits budget.
- Headroom: if NPC count grows or device is mobile CPU-only, switch most NPCs to `NavMeshMovementSO`.

## Verifying ONNX in Editor

`AI_Training/deliverables/intent_classifier_meta.json` documents I/O. `responses.json` is the response map. The reference scripts `NPCDialogueBrain.cs` and `MovementAgent.cs` (in `AI_Training/deliverables/`) show working examples — read them when wiring `OnnxDialogueSO` and `OnnxMovementSO` for the first time.

## Unity 6 namespace change

> [!info] Unity 6 API
> In Unity 6, the package was renamed from "Sentis" (`Unity.Sentis`) to "AI Inference Engine" (`Unity.InferenceEngine`). The package id `com.unity.ai.inference` 2.6.1 already reflects the new naming. Refactor old `using Unity.Sentis;` to `using Unity.InferenceEngine;` if porting samples.

## Backlinks
- [[systems/sentis-runtime]]
- [[systems/npc-dialogue]]
- [[systems/npc-movement]]
- [[decisions/d09-sentis-per-model-worker]]

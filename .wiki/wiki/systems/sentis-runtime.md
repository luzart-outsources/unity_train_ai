---
title: Sentis Runtime
category: systems
tags: [sentis, onnx, runtime]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/MERGED_REPORT.md]
created: 2026-05-12
updated: 2026-05-12
---

# Sentis Runtime

Shared Worker pool for ONNX inference. Per [[decisions/d09-sentis-per-model-worker]].

## Why shared

Two ONNX models with different I/O shapes (`intent_classifier.onnx` and `soldier.onnx`) cannot share a `Worker` instance. But all consumers of each model should reuse the same Worker — initialization is ~50-100ms and Worker is expensive.

## ISentisRuntime

```csharp
public interface ISentisRuntime : IDisposable {
    Worker IntentWorker { get; }
    Worker SoldierWorker { get; }
}
```

## Implementation

```csharp
public class SentisRuntime : ISentisRuntime {
    public Worker IntentWorker { get; }
    public Worker SoldierWorker { get; }
    public SentisRuntime(ModelAsset i, ModelAsset s, BackendType backend) {
        IntentWorker  = new Worker(ModelLoader.Load(i), backend);
        SoldierWorker = new Worker(ModelLoader.Load(s), backend);
    }
    public void Dispose() { IntentWorker?.Dispose(); SoldierWorker?.Dispose(); }
}
```

## Backend choice

- Preferred: `BackendType.GPUCompute` (Compute Shader). Best perf.
- Fallback: `BackendType.CPU` (works everywhere, slower).
- Configurable in `GameConfigSO.preferredBackend`.

See [[open-questions#q-20260512-06]] — mobile device tier may need CPU.

## Tensor lifecycle

```csharp
using var input = new Tensor<int>(new TensorShape(1, MaxLen), tokenIds);
_worker.Schedule(input);
using var output = (Tensor<float>) _worker.PeekOutput();
// access output[0], output[1]...
```

**Every** input + output tensor MUST be `using`-disposed. Leaking → GPU memory exhaustion (see [[technical/sentis-runtime-notes]]).

## Lifecycle

```
Bootstrap → ServiceLocatorSO.Bootstrap() creates SentisRuntime
Application.quitting → SentisRuntime.Dispose()
```

Never recreated mid-game.

## Backlinks
- [[systems/npc-dialogue]]
- [[systems/npc-movement]]
- [[decisions/d09-sentis-per-model-worker]]
- [[technical/sentis-runtime-notes]]

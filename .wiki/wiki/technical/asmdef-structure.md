---
title: Asmdef Structure
category: technical
tags: [asmdef, compile, modularity]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Asmdef Structure

One-direction acyclic graph. Editor-only assemblies are gated by `defineConstraints: UNITY_EDITOR`.

```
TrainAI.Core              → (no game refs; depends only on Unity engine)
TrainAI.SO.Base           → Core
TrainAI.Sentis            → Core, Unity.AI.Inference, Cysharp.UniTask
TrainAI.SO.Concrete       → SO.Base, Sentis, Unity.AI.Inference
TrainAI.Services          → Core, SO.Base, SO.Concrete, UniTask
TrainAI.Presentation      → Services, SO.Base, Unity.InputSystem
TrainAI.UI                → Services, SO.Base, Unity.TextMeshPro
TrainAI.Editor            → ALL above (defineConstraints UNITY_EDITOR)
```

## Per-asmdef contents

| Asmdef | Contains |
|---|---|
| `TrainAI.Core` | `BroadcastService`, `RuntimeSO` base, utility statics |
| `TrainAI.SO.Base` | `QuestSO`, `InteractionSO`, `MovementStrategySO`, `DialogueStrategySO`, `ScoreRuleSO`, `EndingRuleSO`, `AreaSO`, `NPCSO`, etc. (abstract + data SO) |
| `TrainAI.Sentis` | `SentisRuntime`, `VietnameseTokenizer`, `IntentClassifier`, `OnnxMovementAgent` (engine logic — no SO) |
| `TrainAI.SO.Concrete` | `GotoConfirmQuestSO`, `QuizQuestSO`, ..., `OnnxMovementSO`, `OnnxDialogueSO` (concrete SO referencing Sentis) |
| `TrainAI.Services` | `GameClockService`, `QuestRouter`, `ScoreSystem`, `SceneRouter`, `MovementService`, `DialogueService`, `NPCDirector`, `InteractionRouter`, `SaveService` |
| `TrainAI.Presentation` | `PlayerController`, `PlayerInteractor`, `NpcView`, `ThirdPersonCamera`, `QuestArrowHUD`, `GameLoopDriver`, `BootstrapEntry` |
| `TrainAI.UI` | `UIRouter`, `UIScreenBase`, all popup screens, `BroadcastListenerComponent<T>` |
| `TrainAI.Editor` | `SOGenerator`, `SceneBuilder`, `PrefabBuilder`, `ServiceWizard`, `Validator`, `MasterBuilder`, custom Inspectors |

## Why this order

- `Core` has no game refs → testable standalone.
- `SO.Base` defines abstractions; concrete classes know `Sentis` and live in `SO.Concrete` → keeps base lightweight.
- `Services` depend on SO but not on Presentation — services have no MonoBehaviour, easy to unit test with mock SO.
- `Presentation`/`UI` are leaf assemblies — change UI without recompiling services.
- `Editor` gated by `UNITY_EDITOR` define → not in runtime build.

## Validation (in `Validator.cs`)

- `CompilationPipeline.GetAssemblies()` checks for compile errors.
- Reference cycle detector: walk all `.asmdef` references, fail if any cycle.

## Backlinks
- [[technical/architecture]]
- [[technical/automation-tooling]]

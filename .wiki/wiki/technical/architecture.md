---
title: Architecture
category: technical
tags: [architecture, layers, so]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Architecture

Layered SO-First Modular. Dependencies flow downward only.

```
PRESENTATION (MonoBehaviour, uGUI Canvas, Animator)
  PlayerController, NpcView, UIRouter, QuestArrowHUD, HUDs, BroadcastListenerComponent<T>
        ▼ subscribes / invokes
SERVICE (instantiated by ServiceLocatorSO.Bootstrap())
  IGameClock, IQuestRouter, ISceneRouter, IScoreSystem,
  IInteractionRouter, INPCDirector, IDialogueService,
  IMovementService, IUIRouter, ISaveService, ISentisRuntime
        ▼ runs / reads
STRATEGY SO (polymorphic logic — extension points)
  QuestSO, InteractionSO, MovementStrategySO, DialogueStrategySO,
  ScoreRuleSO, EndingRuleSO, TimeRuleSO
        ▼ reads
DATA SO (pure config — Assets/_Data/)
  DayDB, QuestDB, NPCDB, InteractableDB, QuizDB, AreaDB, SubjectDB,
  ResponseTemplatesSO, TimeConfigSO, GameConfigSO, SceneRefSO
        ▼ ONNX as ModelAsset
RUNTIME STATE SO (RSO — mutable, reset OnEnable)
  PlayerStateRSO, GameClockRSO, ActiveQuestRSO, DayProgressRSO
        ▲ events bubble up via BroadcastService<T>
```

## Composition root

`ServiceLocatorSO` is the **only** SO that holds runtime service instances. A `BootstrapEntry` MonoBehaviour in `00_Bootstrap.unity` carries `[SerializeField] ServiceLocatorSO services` and calls `services.Bootstrap()` in `Awake`.

## Key invariants

- **No singletons** beyond `BroadcastService` (which is purposeful static).
- **SO assets are read-only at runtime**; mutable state goes into RSO.
- **Asmdef tree is acyclic** (see [[technical/asmdef-structure]]).
- **Services don't reach down into MonoBehaviour state directly**; they communicate via `BroadcastService` messages and RSO reads.

## Update loop

`GameLoopDriver` MonoBehaviour (in Bootstrap, `DontDestroyOnLoad`):
```csharp
void Update() {
    var dt = Time.deltaTime;
    if (!_services.Clock.Frozen) _services.Clock.Tick(dt);
    _services.Quests.Tick(dt);
    _services.Movement.Tick(dt);   // 5Hz throttle internally
}
```

## Backlinks
- [[overview]]
- [[technical/asmdef-structure]]
- [[technical/so-taxonomy]]
- [[decisions/d01-so-first-modular]]

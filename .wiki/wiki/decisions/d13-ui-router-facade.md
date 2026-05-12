---
title: D-13 UIRouterFacade late-bind pattern
category: decisions
tags: [decision, ui, architecture, bootstrap]
sources: []
created: 2026-05-13
updated: 2026-05-13
---

## D-13: UIRouterFacade late-bind pattern

**Date**: 2026-05-13
**Decided by**: discovered in MCP Play test (stub UI still firing after UIRouterMono.OverrideUI)
**Status**: active

### Context

Boot order in `00_Bootstrap`:
1. `BootstrapEntry` (`DefaultExecutionOrder=-1000`) Awake → `ServiceLocator.Bootstrap()` instantiates 11 services. `UI = new UIRouter()` (stub - returns true/no-op). `InteractionRouter` ctor caches `_ui = UI` (the stub).
2. `UIRouterMono` (Canvas child) Awake later → `services.OverrideUI(this)` swaps `ServiceLocator.UI` to point at the real Mono.
3. But `InteractionRouter._ui` still points at the stub it cached.

Visible bug:
- Player interacts → `InteractionRouter.HandlePressed` calls `_ui.ShowConfirm(text)` → only `Debug.Log` fires (stub), no popup appears.
- Game state advances correctly (stub returns true) so user only sees that "the UI is missing".

### Options considered

| Option | Pros | Cons |
|---|---|---|
| **A**. Force `UIRouterMono` to Awake before `BootstrapEntry` | No facade overhead. | Fragile: depends on `DefaultExecutionOrder` numbers; UIRouterMono needs to be `-1001` and Bootstrap is `-1000`. Risk of regression. |
| **B**. Services hold `Func<IUIRouter>` getter instead of `IUIRouter` | Lazy lookup, always fresh. | Indirection at every call site. Service signatures become `Func<IUIRouter> uiGetter` everywhere. |
| **C**. `UIRouterFacade` — services hold facade ref, `OverrideUI` swaps `facade.Inner` | Services keep simple `IUIRouter` field. Late-bind via stable facade. Stub default if no Mono present. | One indirection class. |

### Decision

**Option C**: `UIRouterFacade : IUIRouter` lives in `TrainAI.Services`. `ServiceLocatorSO.Bootstrap()` constructs `_uiFacade = new UIRouterFacade { Inner = new UIRouter() /* stub */ }` and assigns `UI = _uiFacade`. `OverrideUI(IUIRouter ui)` swaps `_uiFacade.Inner = ui` instead of replacing the field. All cached `_ui` refs in services automatically route to current backend.

### Consequences

- **Good**: `InteractionRouter`, `SceneRouter`, etc. don't care whether real UI is loaded — facade handles fallback to stub. Scene transitions that destroy + rebuild Canvas don't break service refs. Verified in MCP Play: `locator.UI type=UIRouterFacade inner type=UIRouterMono` and real popups render.
- **Pattern reusable**: Same facade approach could wrap other services that scene-side MonoBehaviours might want to override later (e.g. a SceneSentisRuntime that uploads tensors to GPU only when world scene loads).
- **File**: `Assets/Scripts/TrainAI/Services/UIRouterFacade.cs`.

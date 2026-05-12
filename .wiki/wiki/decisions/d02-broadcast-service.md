---
title: D-02 BroadcastService (typed static pub/sub)
category: decisions
tags: [events, broadcast]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-02 — BroadcastService as event backbone
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
With SO-First architecture (D-01), event channels are needed for cross-system communication. Options: Ryan Hipple-style `GameEventSO` (asset per event) vs. typed `BroadcastService` (static class).

### Options considered
1. **GameEventSO per event** — designer-wireable in Inspector, but ~50+ event assets create asset bloat and confuse navigation.
2. **BroadcastService static pub/sub** (chosen) — typed C# struct messages, zero alloc, no asset per event. Listener subscribes in OnEnable, unsubscribes in OnDisable.
3. **UniRx/R3 Subjects** — overkill; UniTask already installed, no need for reactive primitives.

### Decision
**Option 2**. `BroadcastService<T>` static API: `Subscribe / Unsubscribe / Send / Clear`. Messages are `struct` named `XxxMsg`. `Clear()` called on MainMenu load to prevent stale listeners.

### Consequences
- Lose drag-event-to-Inspector convenience. Mitigated by `BroadcastListenerComponent<T>` for UI-side wiring.
- Static state risk on domain reload — `Clear()` discipline required.
- Easier to refactor: rename event = rename type, IDE catches usages.

## Backlinks
- [[systems/broadcast-service]]
- [[technical/architecture]]

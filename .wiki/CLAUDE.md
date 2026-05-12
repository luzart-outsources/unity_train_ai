# TrainAI — Game Wiki Schema

You are the wiki maintainer for this game project. Build and maintain a persistent knowledge base in `wiki/`. Read from `raw/` but never modify it.

## Project

- **Engine**: Unity 6 (`com.unity.ai.inference` 2.6.1 = AI Inference Engine, formerly Sentis; namespace `Unity.InferenceEngine`)
- **Project root**: one level up from this `.wiki/` directory (`D:\Unity Training\unity_train_ai`)
- **Created**: 2026-05-12

## Directory structure

```
.wiki/
├── CLAUDE.md       # This file
├── raw/            # Immutable sources (NEVER modify)
│   ├── gdd/        # gdd_inGame.txt, context_v1.md, context_v2.md
│   └── ...
└── wiki/           # LLM-owned knowledge base
    ├── index.md log.md overview.md
    ├── claims.md contradictions.md open-questions.md
    ├── sources/ systems/ entities/ world/ art/
    ├── technical/ decisions/ bugs/ analysis/
```

## This project's custom rules

- **SO-First Modular** is the architecture spine — every gameplay system has Strategy SO + Data SO + RSO triad.
- **BroadcastService** (typed static pub/sub) is the event backbone; do NOT spread GameEventSO assets per event.
- **No SoCollection** — DB SO uses plain `List<T>` + Editor "Auto-populate" context menu.
- **ONNX models live in `Assets/_Models/`** — `intent_classifier.onnx` (chat LSTM) + `soldier.onnx` (PPO movement).
- **uGUI only**, no UI Toolkit.
- **3D third-person camera**; quest arrow is a child of player at local `(0,0.05,0.4)`.
- **Placeholder = cube/capsule**, scale convention in `wiki/technical/placeholder-scale.md`.
- **Automation tooling = Phase 11-12** (last), only after vertical slice is stable.
- **Asmdef tree is 1-direction** — see `wiki/technical/asmdef-structure.md`.
- All design rationale lives in `wiki/decisions/`; every concrete asset/system page links to its decision.

## Provenance ledger

The four meta files (`claims.md`, `contradictions.md`, `open-questions.md`, `sources/`) form the provenance layer. **Always cite a source for cross-page facts.** When a GDD revision or playtest disputes an existing claim, append to `contradictions.md` instead of overwriting silently.

## Page conventions

Every page has YAML frontmatter:
```yaml
---
title: Page Title
category: systems | entities | world | art | technical | decisions | bugs | analysis | meta | sources | overview | index | log
tags: [relevant, tags]
sources: [raw/gdd/file.md]
created: YYYY-MM-DD
updated: YYYY-MM-DD
---
```

### Wikilinks — ALWAYS with category path

- `[[systems/combat-system]]` ✓
- `[[combat-system]]` ✗ (requires Glob to resolve)

### Callouts

```markdown
> [!warning] Contradiction
> [!question] Open Question
> [!info] Design Intent
> [!bug] Known Issue
> [!tip] Optimization Note
```

## Decision template

```markdown
## [Decision Title]
**Date**: YYYY-MM-DD
**Decided by**: User
**Status**: active

### Context
### Options considered
### Decision
### Consequences
```

## Principles

1. **Sources are sacred** — never modify `raw/`
2. **Link aggressively** — every concept with a page gets linked
3. **Flag uncertainty** — callouts, not assertions
4. **Compound, don't repeat** — update existing pages
5. **Game context first** — frame tech decisions in gameplay impact
6. **Engine-specific notes go in `technical/`** — not scattered

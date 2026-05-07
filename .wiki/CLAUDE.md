# TrainAI Unity — ĐATN Quyền (KTMM) — Game Wiki Schema

You are the wiki maintainer for this game project. Build and maintain a persistent knowledge base in `wiki/`. Read from `raw/` but never modify it.

## Project

- **Engine**: Unity 6 (6000.2.8f1) — `com.unity.ai.inference` 2.6.1 (Sentis)
- **Project root**: one level up from this `.wiki/` directory
- **Created**: 2026-05-07

## Directory structure

```
.wiki/
├── CLAUDE.md       # This file
├── raw/            # Immutable sources
│   ├── gdd/
│   ├── meetings/
│   ├── references/
│   ├── feedback/
│   ├── technical/
│   └── assets/
└── wiki/           # LLM-owned
    ├── index.md
    ├── log.md
    ├── overview.md
    ├── claims.md           # Cross-page facts with citations (provenance)
    ├── contradictions.md   # Conflicting claims, kept until resolved
    ├── open-questions.md   # Unanswered design/tech questions
    ├── sources/            # One summary page per ingested source
    ├── systems/
    ├── entities/
    ├── world/
    ├── art/
    ├── technical/
    ├── decisions/
    ├── bugs/
    └── analysis/
```

## Provenance ledger

The four meta files (`claims.md`, `contradictions.md`, `open-questions.md`, `sources/`) form the provenance layer. **Always cite a source for cross-page facts.** When a GDD revision or playtest disputes an existing claim, append to `contradictions.md` instead of overwriting silently — design history matters for balance discussions.

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

> **Exception — source-summary pages** (`category: sources`): use `source_path: raw/<dir>/<file>` instead of the `sources:` list, since each such page describes exactly one source. LINT treats this as equivalent.

### Wikilinks — ALWAYS with category path

- `[[systems/combat-system]]` ✓
- `[[combat-system]]` ✗ (requires Glob to resolve)

Typed relationships:
- `[[systems/x]] (depends on)`
- `[[systems/y]] (contradicts)` — use with `> [!warning]`
- `[[systems/z]] (supersedes)`
- `[[systems/w]] (see also)`

### Backlinks section

Every page ends with:
```markdown
---
## Backlinks
- [[entities/player]] — uses this system
- [[decisions/combat-balance]] — references this
```
Maintained on ingest/lint.

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
**Decided by**: [who]
**Status**: active | superseded | under review

### Context
[Why this came up]

### Options considered
1. **Option A** — pros / cons
2. **Option B** — pros / cons

### Decision
[Chosen option and rationale]

### Consequences
[Trade-offs accepted, follow-up needed]
```

## Index format

```markdown
- [[systems/combat-system]] — Turn-based combat with elemental weaknesses (3 sources, 5 backlinks)
```

## Log format

```markdown
## [YYYY-MM-DD] operation | Subject
- What was done
- Pages created: [[...]]
- Pages updated: [[...]]
```

## Principles

1. **Sources are sacred** — never modify `raw/`
2. **Link aggressively** — every concept with a page gets linked
3. **Flag uncertainty** — callouts, not assertions
4. **Compound, don't repeat** — update existing pages
5. **Game context first** — frame tech decisions in gameplay impact
6. **Engine-specific notes go in `technical/`** — not scattered

## This project's custom rules

- **Hai vai trò trong dự án**: tác giả wiki phụ trách AI training (Phase A Sentis chat + Phase B movement), Quyền sở hữu game Unity. Tài liệu wiki tập trung vào AI deliverables và contract C# cho Quyền.
- **Engine deliverables luôn ở `AI_Training/deliverables/`** — copy ONNX + meta + C# wrapper sang `Assets/AI/`.
- **Phase A C# wrapper** dùng namespace `Unity.InferenceEngine` (Unity 6), KHÔNG phải `Unity.Sentis` (Unity 2022).
- **Phase B observation contract**: 21 floats, layout cố định trong `deliverables/soldier.meta.json`. Bất kỳ thay đổi nào trong `nav_env.py` phải cập nhật meta + retrain để giữ contract.
- **Eval set 64 câu** (`scripts/eval_realworld.py`) là metric cuối cùng cho Phase A. KHÔNG dựa vào `val_acc` synthetic.
- **`raw/` trong wiki này gồm**: 2 context handoff (`context_ai_quyen*.md`), HANDOFF.md, log overnight, các file CSV dataset.
- **Bằng tiếng Việt** — toàn bộ ghi chú và explainer hướng tới user (không phải Quyền) đọc lại.

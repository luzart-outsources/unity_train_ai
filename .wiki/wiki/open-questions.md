---
title: Open Questions
category: meta
tags: [provenance, open]
sources: []
created: 2026-05-12
updated: 2026-05-12
---

# Open Questions

## Open

### q-20260512-01 — `world_template.json` 30-day content authoring source
- **Why it matters**: Phase 11 master generator needs full blueprint.
- **Where it surfaced**: [[technical/automation-tooling]]
- **Status**: open

### q-20260512-02 — Quiz content (real questions for each subject)
- **Why it matters**: Phase 12 generation produces placeholders unless real content supplied.
- **Status**: open

### q-20260512-03 — Cutscene video file for scene 02_CutScene
- **Why it matters**: Optional — can fall back to fade-only.
- **Status**: open

### q-20260512-04 — Real-world generalization of `intent_classifier.onnx`
- **Why it matters**: Handoff v2 noted prior overfit; need to verify lstm winner generalizes.
- **Where it surfaced**: [[systems/npc-dialogue]], [[sources/merged-report]]
- **Status**: open

### q-20260512-05 — Soldier ONNX behavior at distances > 20m (arena_size)
- **Why it matters**: Schedule travel may exceed training arena; need chunked target or NavMesh fallback.
- **Where it surfaced**: [[systems/npc-movement]]
- **Status**: open

### q-20260512-06 — Sentis backend preference (CPU vs GPUCompute) per device tier
- **Why it matters**: Mobile low-end needs CPU; sets default in `GameConfigSO`.
- **Status**: open

### q-20260512-07 — `[Required]` attribute for Validator null-check — custom or library?
- **Why it matters**: Phase 11 validator implementation choice.
- **Status**: open

### q-20260512-08 — Audio system (BGM/SFX) — design after user confirms scope
- **Why it matters**: Out of current spec; will need decision SO + audio bus.
- **Status**: open

### q-20260512-09 — Localization (Unity Localization package) — wire from Phase 0 or defer?
- **Why it matters**: `LocalizedString` placeholders exist; wiring from start avoids refactor later.
- **Status**: open

### q-20260512-10 — Verify lstm intent classifier generalizes on real Vietnamese player input
- **Why it matters**: Same as q-04 but specifically for in-Unity QA in Phase 5.
- **Status**: open

## Answered

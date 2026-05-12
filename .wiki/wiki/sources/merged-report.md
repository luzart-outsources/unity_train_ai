---
title: Source — Merged Report (Phase A + B deliverables)
category: sources
tags: [report, ai-training, onnx]
source_path: AI_Training/deliverables/MERGED_REPORT.md
created: 2026-05-12
updated: 2026-05-12
---

# Source — MERGED_REPORT.md

**Path**: `AI_Training/deliverables/MERGED_REPORT.md` (generated 2026-05-07)
**Type**: Final report on Phase A intent classifier + Phase B PPO movement winners.

## Winners

| Phase | Winner | Metric | Iter | Notes |
|---|---|---|---|---|
| A (intent) | m1 lstm, 98.44% val_acc | val_acc | 1 | seed 1000 |
| B (movement) | m2 PPO, reward 6.572 | mean reward | 7 | hp=h3_deepfocus, seed 7006 |

## Canonical deliverables

- `intent_classifier.onnx` ← Phase A m1 winner
- `soldier.onnx` ← Phase B m2 winner

## Hyper-param breakdown (Phase B m2)

| HP variant | Best reward | Iter | Seed |
|---|---|---|---|
| h1 baseline | 6.236 | 1 | 7000 |
| h2 big-explore | 6.263 | 6 | 7005 |
| h3 deep-focus (winner) | 6.572 | 7 | 7006 |
| h4 big-wide | 5.252 | 4 | 7003 |

## Cited from this source

- [[claims#c-20260512-07]] lstm 98.44% val_acc
- [[claims#c-20260512-08]] soldier PPO contract via meta.json

## Backlinks
- [[overview]]
- [[systems/npc-dialogue]]
- [[systems/npc-movement]]
- [[sources/intent-classifier-meta]]
- [[sources/soldier-meta]]

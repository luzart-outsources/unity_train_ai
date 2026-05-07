# MERGED REPORT — End of Day

Generated: 2026-05-07T19:53:20.951296

## Phase A — Intent Classifier

| Machine | Best acc | Arch | Iter | Data seed |
|---|---|---|---|---|
| Máy 1 | 98.44% | lstm | 1 | 1000 |
| Máy 2 | 0.00% | None | 0 | None |

**Winner: m1** (98.44%, lstm)

## Phase B — Movement PPO

| Machine | Best reward | Iter | Seed | HP/tag |
|---|---|---|---|---|
| Máy 1 | 6.569 | 14 | 2013 | v3_iter14_s2013 |
| Máy 2 | 6.572 | 7 | 7006 | m2_iter7_h3_deepfocus_s7006 (hp=h3_deepfocus) |

**Winner: m2** (6.572)

### Máy 2 — per-HP breakdown

| HP | Best reward | Iter | Seed |
|---|---|---|---|
| h1_baseline | 6.236 | 1 | 7000 |
| h2_bigexplore | 6.263 | 6 | 7005 |
| h3_deepfocus | 6.572 | 7 | 7006 |
| h4_bigwide | 5.252 | 4 | 7003 |

## Iteration counts
- Máy 1: 27 iter (36 entries in history)
- Máy 2: 123 iter (9 entries in history)

## Canonical deliverables (after merge)

- `intent_classifier.onnx` ← m1 winner
- `soldier.onnx` ← m2 winner

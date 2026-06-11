# Phase C — Overnight Loop Report
**Started:** 2026-06-11 (late night)
**Wrapped:** 2026-06-12 (~5 AM)
**Winner:** Student v6 — **98.1 %** on 53-case strict hardset
**Smoke test on deployed v6:** 27/27 = **100 %**

## TL;DR

| Version | Data | Arch | Epochs | LR | Strict eval | Note |
|---------|------|------|--------|------|------------|------|
| **v1** | qa_pairs.jsonl (11K)   | 192d/4L/6H 3.4M | 20 | 5e-4 | **94.3 %** | baseline (already shipped) |
| v2     | qa_pairs_v8.jsonl (54K) | same            | 15 | 5e-4 | 86.8 % | **regression** — overconfident on fake areas |
| **v3** | qa_pairs_v9.jsonl (56K) | same            | 12 | 5e-4 | **96.2 %** | +1.9 pp, ORIGINAL_BUG fixed |
| v4     | qa_pairs_v9.jsonl       | 256d/4L/8H 6.3M | 12 | 5e-4 | 96.2 % | wider, no extra win |
| v5     | qa_pairs_v10.jsonl (56K) | 192d/4L/6H     | 12 | 5e-4 | 94.3 % | telex saturation hurt cross-entity discrimination |
| **v6** | qa_pairs_v9.jsonl       | 192d/4L/6H     | 20 | 3e-4 | **98.1 %** | **NEW BEST, DEPLOYED** |
| v7     | qa_pairs_v9.jsonl       | 192d/4L/6H     | 30 | 2e-4 | 96.2 % | longer / slower didn't beat v6 |

## Original user bugs — status

| Complaint | Before | After v6 |
|-----------|--------|----------|
| "khu A1 ở đâu" → AI says khu B (mode collapse) | All 4 fake areas hit FreeArea-related answers | **"Trong doanh trại không có khu A1. Đồng chí muốn hỏi sân vận động, nhà ăn..."** |
| "khu A2 ở đâu" → same wrong A | same | **specific custom OOS answer** |
| "khu A3 ở đâu" → same | same | **specific custom OOS answer** |
| "khu B ở đâu" → same | same | **"Đại đội ta chỉ có 6 khu vực... khu B không có"** |
| "Mấy giờ ăn cơm" → AI returns đi ngủ time | top-3 all slot_8 (đi ngủ) at 0.85 | **"Nghỉ trưa và ăn trưa từ 11:30 đến 14:00"** at score 0.91 |

ALL 5 of the original "AI không tốt" complaints are resolved in v6.

## What worked

1. **5× more data with diverse augmentation (v8 → 54K)** — the foundation. Phase A's
   240K samples were domain-mismatched (military academy on a farming game). v8 was
   built from the actual game entities + 9 augmentation modes including heavy telex,
   slang chains, code-mix with 8 prefixes, double fillers.

2. **Explicit negative samples for fake areas (v9 → +1,148 records)** — the
   breakthrough. Training student to recognize "khu A1/A2/A3/B/..." as OUT_OF_SCOPE
   stopped the embedding space from collapsing those phrases onto the nearest real
   area.

3. **Per-meal specific time templates** — explicit (q, a) pairs for
   "Mấy giờ ăn cơm" → ăn trưa answer disambiguated the meal-vs-sleep confusion
   that v1 had.

4. **Longer training with lower LR (20 ep, 3e-4)** — v6's config. The negatives in
   v9 made over-confidence unlikely, so longer training could squeeze out the telex
   robustness without re-introducing the v2 regression. Sweet spot was 20 epochs —
   v7's 30 epochs / 2e-4 missed it.

## What didn't work

- **Wider student (256d / 8H)** — same accuracy as 192d / 6H but 2× params + 2× FLOPs.
  Data is the ceiling here, not capacity.

- **Telex saturation (v10 dataset)** — densely packing 1.5K extreme-telex variants
  caused cross-entity confusion. "Nhaa aan o ddaau" stopped retrieving nhà ăn because
  many other heavy-telex versions of other queries clustered nearby.

- **Even longer training (30 ep, lr 2e-4)** — diminishing returns. v6's 20 epochs is
  the local optimum.

## Architecture wins

The student that ships to Unity is identical to v1's blueprint (192-dim, 4 layers,
6 heads, 4.16M params) but TRAINED ON DIFFERENT DATA AND SCHEDULE:

```
Architecture (unchanged):
  Embedding(vocab_size = 14K, 192-dim)
  ├── PositionalEmbedding(40)
  ├── 4× TransformerEncoderLayer (heads=6, ffn=512)
  ├── Mean-pool (mask-aware)
  └── Linear(192 → 384) + L2 normalize

Tokenizer (unchanged):
  Lowercase + Vietnamese-aware char filter + greedy longest multi-word match

Training (v6 recipe):
  - Data:    qa_pairs_v9.jsonl   (55,743 records)
  - Loss:    MSE + 0.5 × (1 - cosine) vs teacher embedding
  - Schedule: 20 epochs, batch 64, AdamW lr 3e-4, cosine decay with 10 % warmup
  - Distill from: minilm_ft_v2 (multilingual MiniLM fine-tuned on Vietnamese
                                game Q&A, 95.9 % hardset, 470 MB)
```

## Production deployment

Files mirrored to `Assets/AI/`:

- `Assets/AI/Models/student_encoder.onnx` (15.5 MiB, opset 15, fixed `[1, 40]` shape)
- `Assets/AI/Resources/student_bank.bytes` (81.7 MiB, 55,743 × 384 L2-normalized vectors)
- `Assets/AI/Resources/student_bank.json` (4.3 MiB, answer / intent / entity metadata)
- `Assets/AI/Resources/vocab_phase_c.json` (token → id)
- `Assets/AI/Resources/student_meta.json` (arch hyperparams for the loader)

`EmbeddingChatBrain.cs` thresholds (no change needed — v6 scores stay calibrated):
- `highThreshold = 0.88` → template answer
- `lowThreshold  = 0.72` → low-confidence (caller can RAG)
- below 0.72         → graceful "tôi không hiểu" fallback

## How to verify

```bash
# 1. Python smoke test (no Unity required)
AI_Training/phase_a_sentis/.venv/Scripts/python.exe \
  AI_Training/phase_c_chat/scripts/smoke_test_onnx.py
# Expected: PASS 27/27 = 100.0%, avg latency ~3 ms

# 2. Interactive REPL
AI_Training/phase_a_sentis/.venv/Scripts/python.exe \
  AI_Training/phase_c_chat/scripts/chat_repl.py

# 3. Unity Editor menu
#   AI > 7. Phase C Native — Run Smoke Test (no Play mode)
#   AI > 6. Phase C Native — ONNX Chat Test (interactive)
```

## Iteration journal (git log)

```
loop(chat): ITER 1-2 — v8/v9 datasets + v2 distillation (overconfidence regression)
loop(chat): ITER 2 — student v3 hits 96.2 % (+1.9 pp), deployed to Unity
loop(chat): ITER 3 — v4 wider student (256d/8H, 6.3M params) ties v3 at 96.2 %
loop(chat): ITER 4 — v5 telex-saturated dataset (REGRESSION to 94.3 %)
loop(chat): ITER 5 — student v6 = 98.1 %, deployed (best so far)
loop(chat): ITER 6 — v7 (30 ep, lr 2e-4) = 96.2 %, DID NOT BEAT v6 (98.1 %)
```

## Remaining gap

1 case still fails on the hardset: `"Saan vaan ddoongg o ddaau"` (extreme telex
of "sân vận động ở đâu"). This needs character n-gram or subword tokenization to
recover — out of scope for distillation with whitespace tokenizer. Would require
either:
- BPE / WordPiece training-time tokenizer (and a C# BPE port), or
- Char-level n-gram features added to the student embedding layer.

This case has cosine 0.62 → routes to fallback in production, so user gets the
graceful "Tôi chưa hiểu rõ câu hỏi..." answer instead of a confidently-wrong one.
That's acceptable production behavior for an out-of-typical-input case.

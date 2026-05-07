---
title: Evolution v1 → v3.1 (data scale, eval rigor, loop maturity)
category: analysis
tags: [history, evolution, data, eval]
sources: [raw/gdd/context_v1.md, raw/gdd/context_v2.md, raw/technical/handoff_morning.md, raw/technical/phase_b_runs.csv]
created: 2026-05-07
updated: 2026-05-07
---

# Evolution v1 → v3.1

Lịch sử các phiên bản training Phase A + B. Đọc từ trên xuống để hiểu vì sao đến state hiện tại.

## v1 (đầu tháng 5/2026 — context handoff đầu)

- **Status**: chỉ trên giấy — chưa code, chưa train
- Quyền + user thảo luận scope, data design, AI plan
- Decision: [[decisions/scope-cuts|cắt 8 mini-game]]

## v2 (rạng sáng 7/5/2026 — overnight loop đầu)

### Phase A v2
- Generator: ~50 templates × 30-60 từ/pool → 529 câu
- Vocab: 165 tokens
- FastText 30 epochs → val_acc 1.00 (overfit)
- Real-world test 8 câu: **1/8 = 12.5%** ‼️
- Baseline khủng khiếp — vocab nghèo, model UNK fallthrough

### Phase A v2 fix (cùng đêm)
- Generator richer: 22 templates × 60+ từ/pool → 2000 câu
- Vocab: 769 tokens
- 3 archs trained: FastText 0.96 / LSTM 0.93 / Transformer 0.95 val_acc
- Real-world 16 câu: **FastText 16/16 = 100%, LSTM 15/16, Transformer 14/16**
- Conclude (sai): "FastText canonical"

### Phase B v2 (overnight)
- ~30 PPO runs × 200k steps × env fixed 6 obstacles
- Net [64, 64]
- Best mean_reward: **6.126** (iter 8, lucky early)
- Plateau 5.5-6.1 across iterations
- Bug: [[bugs/loop-spin-near-deadline|493k iter spin gần deadline]]

## v3 (sáng 7/5/2026 — bumped scale, random env)

### Phase A v3
- Generator: 50+ templates × 60+ pools + augmentation → 16,000 câu, vocab 1,975
- Train 3 archs cycle, mỗi run ~1.5min (8.7x speedup nhờ cached tokens)
- Val_acc: ~99% mọi arch
- Real-world 16 câu: vẫn 16/16 = 100% mọi arch (test cũ saturate)

### Phase B v3
- 500k steps/iter × env random 3-12 obstacles
- Net [128, 128] (bigger)
- Best mean_reward sau 5 iters: **6.05** (iter 2)
- Score thấp hơn v2 vì env khó hơn — không thể so sánh trực tiếp
- File: `deliverables/soldier_v2_fixedenv.onnx` backup, `soldier.onnx` v3

## v3.1 (trưa 7/5/2026 — eval rigor + canonical fix)

### Phát hiện FastText fail
Mở rộng test lên 64 câu (slang, telex, compound, OOD):
- FastText: **25%** ‼️
- LSTM: **95.3%** ⭐
- Transformer: **18.8%**

→ Đổi canonical từ FastText sang LSTM ([[decisions/lstm-canonical-not-fasttext]])

### Bump scale + restart loop
- Phase A: 5000 samples/intent = **40,000 total**
- Phase A: cycle ưu tiên LSTM trước
- Phase B: **1M steps/iter** (2x v3, 5x v2)
- Net [128, 128] giữ
- Deadline: 19:00 7/5/2026 (~10h)
- Spin guard fix

### Iter 1 v3.1 — LSTM
- Phase A LSTM real-world: **63/64 = 98.4%** ⭐ (lên từ 95.3% với 16k data)
- Phase B 1M PPO mean_reward: **5.572** (chưa break v3 best 6.05)
- Tag: `v3_iter1_s2000`, deliverable `intent_classifier.onnx` = LSTM
- Time: 09:00 → 09:38 (38 min: 6.5 min Phase A + 31 min PPO 1M)

### Iter 2 v3.1 — FastText
- Phase A FastText real-world: **61/64 = 95.3%** ⭐ (huge jump từ 25% với 16k data)
- Bài học: FastText không "fundamentally broken" — chỉ cần đủ vocab diversity
- Vẫn dưới LSTM 98.4% nên canonical không đổi
- File name conflict: `fasttext_intent.onnx` per-arch overwrite legacy alias (xem [[live-status]])
- Phase B 1M PPO: **mean_reward 6.011** ⭐ (vượt iter 1 = 5.572), canonical `soldier.onnx` updated
- Time: ~10:08 → 10:15 (1M steps ~ 31 min)

### Iter 3 v3.1 — Transformer
- Phase A Transformer real-world: **61/64 = 95.3%** ⭐ (huge jump từ 18.8% với 16k data)
- Confirms hypothesis "data scale > arch complexity": cả 3 archs giờ đều ≥95% với 40k data
- LSTM nhỉnh hơn 3pp do giữ thứ tự từ; FastText/Transformer ngang nhau
- Phase B 1M PPO: chạy đến ~10:55 (sắp xong khi sync)

### Trạng thái sau iter 3 (snapshot 10:53)
| Arch | Real-world acc | Iter |
|---|---|---|
| LSTM ⭐ | 98.44% | 1 |
| FastText | 95.31% | 2 |
| Transformer | 95.31% | 3 |

| Phase B run | reward | Iter |
|---|---|---|
| iter 1 | 5.572 | 1 |
| **iter 2** ⭐ | **6.011** | 2 |
| iter 3 | (đang chạy) | 3 |

### Phase 2-machine plan (in progress)
- Máy 1: tiếp tục loop v3.1 đến 19:00 (~8h nữa, ~13-15 iters)
- Máy 2 (chưa start): HEAVY config — 200k samples + 2M PPO × 4 HP cycle
- Cuối ngày `merge_machine_results.py` auto-pick winner cho canonical
- Chi tiết: [[decisions/two-machine-parallel]]

## End of day v3.1 — máy 1 (2026-05-07 18:59)

Loop `overnight_loop.py` kết thúc tự nhiên ở 18:59:21 (deadline 19:00, có spin guard 30s nên dừng đúng giờ thay vì spin junk như v2 bug). Tổng cộng **27 iter** (~16 effective + 11 spin-guard cuối ngày).

### Final per-arch bests (Phase A) — qua 27 iters

| Arch | Best | At iter | Số lần thử seed |
|---|---|---|---|
| **LSTM** ⭐ | **98.44%** | 1 | 6 (không cải thiện sau iter 1) |
| **FastText** ⭐ | **98.44%** | **17** | 8 (cần exploration để leo) |
| Transformer | 96.88% | 18 | 6 (cải thiện chậm) |

**Bài học**: FastText cần 8× retry seeds để đạt 98.44% (match LSTM). Nếu chỉ chạy iter 1 thì 95.3% — sai lệch 3pp do random seed lottery. Nhiều iter HỮU ÍCH cho FastText/Transformer; vô ích cho LSTM (saturated từ iter 1).

### Phase B history (1M steps mỗi iter)

| Iter | Reward | Time |
|---|---|---|
| 1 | 5.572 | 09:38 |
| 2 | 6.011 | 10:14 |
| 3 | 5.573 | 11:02 |
| 4 | 6.045 | 11:39 |
| 5 | 5.896 | 12:09 |
| 6-7 | n/a | — |
| 8 | 5.962 | 13:50 |
| 9 | 2.442 (outlier) | 14:27 |
| 10 | 5.914 | 14:58 |
| 11 | 4.770 | 15:28 |
| 12 | 6.013 | 16:08 |
| 13 | 6.051 | 16:42 |
| **14** | **6.569** ⭐ | **17:14** |
| 15 | 5.769 | 17:54 |
| 16 | 6.283 | 18:28 |

**Ceiling break ở iter 14** (seed 2013, lucky lottery ticket). Plateau cũ 6.0-6.05 → 6.569 = jump 0.5.

### Final state máy 1
- `intent_classifier.onnx`: LSTM 98.44% (canonical từ iter 1)
- `lstm_intent.onnx`: LSTM 98.44%
- `fasttext_intent.onnx`: **FastText 98.44%** (NEW từ iter 17)
- `transformer_intent.onnx`: Transformer 96.88%
- `soldier.onnx`: **PPO 6.569** (từ iter 14)

### Bài học cuối session
1. **Saturated metric** → wasted compute. LSTM 5/6 lần thử thừa.
2. **Stochastic training** (PPO + non-saturated FastText) → mỗi iter có giá trị, lottery ticket
3. **Spin guard FIX hoạt động** — v2 spin 493k iter, v3.1 spin 11 iter (30s sleep mỗi cái = 5.5 min wasted, acceptable)
4. **Data scale > arch** confirmed: FastText (51K) match LSTM (116K) với data đủ + seed đủ
5. **Single canonical filename** an toàn hơn dual (legacy alias) — phải xử lý collision sau

### Máy 2 — last known 14:05
- Decision lớn: skip Phase A (timeout 40 min mỗi iter). Focus Phase B HP cycle.
- Best Phase B máy 2: **6.236** (iter 1 h1_baseline, 12:41) — thấp hơn máy 1 final 6.569.
- Status sau 14:05 chưa rõ, đợi máy 2 push branch.

## Bài học chính

| Lesson | Source |
|---|---|
| Data quality > model complexity | v1→v2: vocab 165→769 fix overfit |
| Test set phải đủ khó | v3→v3.1: 16-câu → 64-câu mới phân biệt được archs |
| Architecture cycle + auto-pick best là an toàn | v3.1: FastText/LSTM/Transformer đều train, canonical = winner |
| Random env > fixed env cho generalize | v2 (fixed 6) vs v3 (random 3-12) |
| Sleep guard cho deadline loop | bug v2 spin 493k iter |
| Cached pre-tokenize → 8.7× speedup | dataset.py opt cho 16k+ samples |

## Metrics tóm gọn

| Version | Phase A real-world | Phase B mean_reward | Vocab | Iter time |
|---|---|---|---|---|
| v1 | 1/8 = 12.5% | n/a | 165 | n/a |
| v2 | 16/16 = 100% (easy test) | 6.126 (fixed env) | 769 | ~6 min |
| v3 | 16/16 = 100% (easy test) | 6.05 (random env) | 1975 | ~17 min |
| **v3.1** | **63/64 = 98.4%** (hard test) ⭐ | TBD | ~3000 (est) | ~35 min |

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[systems/movement-ai]]
- [[technical/training-pipeline]]
- [[bugs/fasttext-overfit-narrow-test]]
- [[bugs/loop-spin-near-deadline]]

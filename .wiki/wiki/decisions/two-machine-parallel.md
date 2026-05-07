---
title: Chạy 2 máy song song để max throughput
category: decisions
tags: [parallel, multi-machine, throughput, ppo]
sources: []
created: 2026-05-07
updated: 2026-05-07
---

## Chạy 2 máy song song để max throughput

**Date**: 2026-05-07
**Decided by**: User + AI helper
**Status**: active

### Context

User có 2 máy. Loop v3.1 đang chạy trên máy 1 với:
- Phase A 40k samples × 3 archs cycle (cycle ~7-15 min)
- Phase B 1M PPO × 1 iter (~25-30 min)
- Iter total ~35 min, plateau Phase B reward ~6.0

Bottleneck: **Phase B** (chiếm 80% thời gian, plateau cố định). Phase A đã saturated 98.4%.

### Options considered

1. **Máy 2 chạy clone hệt máy 1** — same script, same seed pool
   - Pros: đơn giản, không cần code mới
   - Cons: same seed → cùng kết quả → vô ích

2. **Máy 2 chạy variant với seed offset + HP khác** ⭐ chọn
   - Khác seed pool → search khác region
   - Khác HP (net size, ent coef, steps) → khám phá HP space
   - Output vào folder riêng → no file collision
   - Cuối ngày: chọn best giữa 2 máy

3. **Máy 1 + 2 phối hợp via shared queue (Redis/file)** — overengineering cho ĐATN

### Decision

**Option 2** với phân vai HEAVY rõ ràng — máy 2 dồn lực vào "data factory + HP grid":

| | Máy 1 (overnight_loop.py) | Máy 2 (overnight_loop_machine2.py — HEAVY) |
|---|---|---|
| Vai trò | Iter nhanh, 3 archs cycle | **Heavy lift**: data 5×, HP cycle 4 configs |
| Phase A target | 5,000/intent (40k total) | **25,000/intent (200k total — 5× máy 1)** |
| Phase A archs | LSTM/FastText/Transformer cycle | LSTM only (winner — focus) |
| Phase A epochs | 25 | 35 |
| Phase A seeds | 1000+ | 5000+ |
| Phase B steps | 1M | **2M** |
| Phase B HP | fixed [128,128] ent=0.01 lr=3e-4 | **cycle 4 configs:** ⬇ |
| | | h1_baseline: [128,128] ent=0.01 lr=3e-4 |
| | | h2_bigexplore: [256,128] ent=0.02 lr=3e-4 |
| | | h3_deepfocus: [128,128,64] ent=0.005 lr=1e-4 |
| | | h4_bigwide: [256,256] ent=0.05 lr=5e-4 |
| Phase B seeds | 2000+ | 7000+ |
| Iter time | ~35 min | ~60-70 min (heavy data + 2M PPO) |
| Iters in 12h | ~15-18 | ~10-12 (cycle 4 HPs) |
| Deliverables folder | `deliverables/` | `deliverables_m2/` |
| State / log | `overnight_v3*` | `overnight_v3_m2*` |

### Setup máy 2

```bash
# Clone repo
git clone https://github.com/luzart-outsources/unity_train_ai.git
cd unity_train_ai

# Create venv (máy 2 GPU specs có thể khác)
python -m venv AI_Training/phase_a_sentis/.venv
AI_Training/phase_a_sentis/.venv/Scripts/pip install -r AI_Training/phase_a_sentis/requirements.txt
AI_Training/phase_a_sentis/.venv/Scripts/pip install stable-baselines3==2.4.0 gymnasium==0.29.1

# Run loop (background, tách shell):
nohup AI_Training/phase_a_sentis/.venv/Scripts/python.exe AI_Training/overnight_loop_machine2.py > m2.out 2>&1 &
```

### Sync giữa 2 máy

**No real-time sync needed.** Each máy độc lập. Shared via git push/pull cuối ngày:

```bash
# Trên máy 2, sau khi loop xong:
git checkout -b machine-2-results
git add AI_Training/deliverables_m2/ \
        AI_Training/overnight_v3_m2.log \
        AI_Training/overnight_v3_m2_state.json \
        AI_Training/phase_b_movement/logs/training_runs.csv
git commit -m "machine-2: best PPO X.XX reward, LSTM YY.Y%"
git push origin machine-2-results

# Trên máy 1, merge results + auto-pick winners:
git fetch origin machine-2-results
git merge origin/machine-2-results --no-ff
.venv/Scripts/python AI_Training/merge_machine_results.py
# Tool sẽ:
#   - Đọc cả 2 state files
#   - So sánh best Phase A acc + Phase B reward
#   - Copy winning ONNX vào deliverables/intent_classifier.onnx + soldier.onnx
#   - Ghi MERGED_REPORT.md với full breakdown (tất cả HP × machine)
```

### File lock points (CRITICAL)

Vì 2 máy cùng repo (1 nếu sync git, 2 instance riêng nếu không):
- ✅ **Different state/log files** (`_m1` vs `_m2`) → no collision
- ✅ **Different deliverables folder** (`deliverables/` vs `deliverables_m2/`)
- ✅ **Different intents_v3 files** (`intents_v3.csv` vs `intents_v3_m2.csv`)
- ✅ **Same training_runs.csv** — 2 máy append concurrent. Risk: line interleave. Mitigation: Python `csv.writer` writes flush after each row, OS-level append usually atomic for short lines. Acceptable.
- ⚠️ **Shared models/ folder** — train.py overwrites `<arch>_best.pt`, `vocab.json`, `id2label.json`. Nếu 2 máy cùng train Phase A đồng thời → race condition. Mitigation: máy 2 có Phase A iter time ~3 min, máy 1 có ~3 min. Khả năng overlap thấp. Better: make train.py write to `<arch>_<machine_id>_best.pt`. (TODO)

### Consequences

**Trade-offs accepted**:
- Không real-time sync → có thể trùng nỗ lực ngắn hạn
- 2× điện + bandwidth
- Cuối ngày phải merge thủ công

**Lợi ích**:
- 2× Phase B exploration → nhanh tìm policy hiệu quả
- Diverse HP coverage (bigger net + higher entropy)
- Backup nếu 1 máy crash → còn lại continue

### Backlinks

- [[overview]]
- [[live-status]]
- [[technical/training-pipeline]]
- [[systems/movement-ai]]

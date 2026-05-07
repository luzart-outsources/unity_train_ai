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

**Option 2** với phân vai cụ thể:

| | Máy 1 (overnight_loop.py) | Máy 2 (overnight_loop_machine2.py) |
|---|---|---|
| Phase A archs | LSTM, FastText, Transformer cycle | LSTM only (focus) |
| Phase A samples | 5000/intent | 5000/intent |
| Phase A seeds | 1000+ | 5000+ |
| Phase B steps | 1,000,000 | **2,000,000** (deeper) |
| Phase B net | [128, 128] | **[256, 128]** (bigger) |
| Phase B ent_coef | 0.01 | **0.02** (more explore) |
| Phase B seeds | 2000+ | 7000+ |
| Iter time | ~35 min | ~55 min |
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

# Trên máy 1, merge results:
git fetch origin machine-2-results
git checkout main
git merge origin/machine-2-results --no-ff
# Pick best soldier:
#   Compare deliverables/soldier.onnx vs deliverables_m2/soldier_m2.onnx by reward
#   Copy winner to deliverables/soldier.onnx (canonical)
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

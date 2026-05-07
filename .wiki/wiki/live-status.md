---
title: Live Status
category: meta
tags: [status, live, multi-instance]
sources: [raw/technical/overnight_v3.log]
created: 2026-05-07
updated: 2026-05-07
---

# Live Status — sync giữa 2 máy / 2 Claude instances

> [!info]
> Trang này phản ánh state ở mốc cập nhật cuối. Truy vấn live luôn dùng:
> ```bash
> tail "AI_Training/overnight_v3.log"
> cat  "AI_Training/overnight_v3_state.json"
> ```

## Snapshot lúc 2026-05-07 19:40 — END OF DAY (cả 2 máy completed) ⭐⭐

### Loop info máy 1 — KẾT THÚC tự nhiên ở 18:59:21 (deadline reached)
- Master script: `AI_Training/overnight_loop.py`
- Started: 09:00:00, Ended: 18:59:21 (~10h training)
- Iters completed: **27** (vài iter spin gần deadline đã được spin guard handle ✓)
- Loop process exit clean — `tasklist | grep python` empty

### Final Phase A bests (per-arch, từ 27 iters)
| Arch | Best acc | At iter | Trend |
|---|---|---|---|
| **LSTM** ⭐ | **98.44%** (63/64) | 1 | sat từ đầu, không cải thiện qua 6 lần thử |
| **FastText** ⭐ | **98.44%** (63/64) | **17** | 🚀 95.31% → 96.88% → **98.44%** (match LSTM) |
| Transformer | 96.88% (62/64) | 18 | 95.31% → 96.88% |

**Phát hiện big**: FastText (51K params) match LSTM (116K) sau 6 lần seed thử. Confirm "data scale > arch complexity, given enough seed exploration".

### Final Phase B máy 1 — break plateau!
- **Best mean_reward: 6.569** (iter 14, tag `v3_iter14_s2013`, 17:14:55)
- File: `deliverables/soldier.onnx` (~80KB, [128,128] net, random env)
- **Plateau 6.0 đã break** sau 14 iter (lottery ticket cho seed 2013)
- Range của 14 PPO iter: 2.4 (outlier crash iter 9) đến 6.569 (iter 14)
- Snapshot Phase B history:
  - iter 1=5.572, iter 2=6.011, iter 4=6.045, iter 9=2.442 (outlier),
    iter 12=6.013, iter 13=6.051, **iter 14=6.569** ⭐, iter 15=5.769, iter 16=6.283

### Bài học từ 27 iters
1. **Phase A LSTM saturate ngay iter 1** — 5 lần thử thêm vô ích
2. **Phase A FastText cần exploration** — 8 iter mới đến 98.44%, mỗi seed tạo embedding khác
3. **Phase B PPO = lottery ticket** — 14 iter mới trúng seed 2013 đạt 6.569 (jump 0.5 vs trước)
4. Nhiều iter chỉ có ích khi **metric chưa saturate** hoặc **training stochastic**
5. Saturated → wasted compute (Phase A LSTM iters 2,4 sau iter 1)

### Deliverables (máy 1 final)
```
deliverables/
├── intent_classifier.onnx       LSTM 98.44% canonical (1.2MB)
├── lstm_intent.onnx             LSTM 98.44%
├── fasttext_intent.onnx         FastText 98.44% (NEW — match LSTM, 928KB)
├── transformer_intent.onnx      Transformer 96.88% (1.27MB)
├── soldier.onnx                 ⭐ PPO 6.569 (80KB)
├── soldier_v2_fixedenv.onnx     v2 backup 6.126
├── intent_classifier_meta.json  LSTM vocab
├── responses.json               8 intent × 4 templates
├── NPCDialogueBrain.cs          Unity wrapper Phase A
└── MovementAgent.cs             Unity wrapper Phase B
```

### Máy 2 — last known state từ commit `ff5affc` (14:05)
- Decision lớn 14:03: skip Phase A (200k LSTM CPU > 40 min timeout, 3 lần fail rc=-1)
- Focus 100% Phase B HP cycle 4 configs
- Last known best Phase B: **6.236** (iter 1 h1_baseline, 2M steps, seed 7000, 12:41:45)
- ⚠️ Status sau 14:05 không rõ — state file trong .gitignore, máy 2 chưa push branch riêng
- Deadline máy 2 = 19:00 — đã end (chưa biết best cuối)

### So sánh 2 máy (last known)
| | Máy 1 (FINAL) | Máy 2 (14:05 known) |
|---|---|---|
| Phase A | LSTM/FastText 98.44% | skip (timeout) |
| Phase B | **6.569** | 6.236 |
| Steps/iter | 1M | 2M |
| Effective iters | ~16 | ~3-4 |

→ Máy 1 dẫn ở Phase B nhưng máy 2 có thể có run mới sau 14:05.

### Bước tiếp theo — merge máy 2

Khi máy 2 push xong branch:
```bash
git fetch origin
git checkout -b end-of-day
git merge origin/machine-2-results --no-ff
AI_Training/phase_a_sentis/.venv/Scripts/python AI_Training/merge_machine_results.py
```

`merge_machine_results.py` đọc state file của 2 máy → so sánh → copy ONNX winner vào canonical → ghi `MERGED_REPORT.md`.

---

## Snapshot lúc 2026-05-07 14:05 (cũ — máy 1 còn đang chạy)

### Loop info máy 1 (cũ)
- Master script: `AI_Training/overnight_loop.py`
- Started: 09:00:00, deadline 19:00:00 — đã hoàn tất (state cuối từ user, chưa pull về máy 2)
- Config: `PHASE_A_TARGET=5000`, `PHASE_B_STEPS=1_000_000`, archs cycle `[lstm, fasttext, transformer]`
- Best ghi nhận lúc 14:00 từ user: Phase A LSTM 98.4%, Phase B 6.011

### Loop info máy 2 (HEAVY) — COMPLETED 18:59:10
- Master script: `AI_Training/overnight_loop_machine2.py`
- Started: 11:19:07 → restarted 14:03:27 (skip Phase A) → **clean exit lúc 18:59:10** khi `tl <= 60s`
- Phase B iters thật sự train: **5 iters (iter 3-7) × 2M steps = 10M total PPO steps explored**
- Số "iter" trong state.json (123) bao gồm spin-guard sleep loop sau khi không còn time cho Phase B mới — chỉ 5 iter có Phase B work thật
- Output: `deliverables_m2/soldier_m2.onnx` = h3_deepfocus 6.572 ⭐
- ⚠️ Hardware: máy 2 chỉ Intel UHD 770, KHÔNG GPU NVIDIA → toàn bộ train trên CPU. Phase B đã `device=cpu` sẵn.
- Chi tiết: [[decisions/two-machine-parallel]]

> [!info] Skip Phase A trên máy 2 (decision 14:03)
> Iter 1-3 (Phase A): 200k samples × 35 epochs LSTM CPU > 40 min, hit timeout `PHASE_A_TRAIN_TIMEOUT=2400` mỗi lần → fail rc=-1 mỗi iter, phí 40 phút/iter vô ích. Máy 1 đã có canonical LSTM 98.4% từ iter 1 nên máy 2 không cần Phase A. Set `PHASE_A_ARCHS=[]` + guard `if PHASE_A_ARCHS:` ở loop. Lợi ích: từ ~4 iter → ~7 iter trong cùng deadline, mở rộng HP grid 1.5×.

> [!bug] Generator O(N²) blocker tại HEAVY scale (đã fix)
> Lần launch đầu (11:09) bị stall 7+ phút ở `generate_dataset_v3.py` vì line 746 dùng `len([r for r in rows if r["intent"]==intent])` — quadratic theo N. Fix: counter `kept` O(1). 200k samples: 30+ min → **3 sec**. Chi tiết: [[bugs/generator-on2-quadratic]].

### Phase B máy 2 — FINAL HP grid (per-HP best across 2 seeds)

| HP | Net | Ent | LR | Best Reward | Best Iter | Best Seed |
|---|---|---|---|---|---|---|
| **h3_deepfocus** ⭐ | [128,128,64] | 0.005 | 1e-4 | **6.572** | 7 | 7006 |
| h2_bigexplore | [256,128] | 0.02 | 3e-4 | 6.263 | 6 | 7005 |
| h1_baseline | [128,128] | 0.01 | 3e-4 | 6.236 | 1 | 7000 |
| h4_bigwide | [256,256] | 0.05 | 5e-4 | 5.252 | 4 | 7003 |

**Soldier_m2.onnx final** = h3_deepfocus iter 7 (6.572), vượt máy 1 best 6.011 ~9.3%.

**Bài học HP grid**:
- Conservative HP thắng: low ent (0.005) + low lr (1e-4) + deeper net [128,128,64] = best
- Bigger net hurt khi không kèm careful tuning — h4 [256,256] với ent=0.05 → 5.252 (flop)
- Variance cao: h2 lần 1 = 5.675 → lần 2 = 6.263 (+0.59 chỉ do seed)
- ≥2 seeds/HP cần thiết để đánh giá đáng tin

### Phase A — current bests (all 3 archs trained on v3.1 40k data)
| Arch | acc | iter | data_seed | Trạng thái |
|---|---|---|---|---|
| **LSTM** ⭐ | **98.44%** (63/64) | 1 | 1000 | canonical |
| FastText | 95.31% (61/64) | 2 | 1001 | per-arch best |
| Transformer | 95.31% (61/64) | 3 | 1002 | per-arch best |

> [!info] Phát hiện iter 2-3
> Cả 3 archs trên 40k data đều đạt ~95-98%. Trên 16k data thì spread rộng (25/95.3/18.8). Kết luận lại: **data scale là yếu tố quyết định, không phải arch**. FastText và Transformer không "broken" — chỉ cần đủ vocab diversity. LSTM nhỉnh hơn 3pp do giữ thứ tự từ.

### Phase B — current best
- mean_reward: **6.011** (iter 2, tag v3_iter2_s2001, 1M steps)
- File: `deliverables/soldier.onnx` (~80KB, net [128,128], random env)
- Iter 1 = 5.572, iter 2 = 6.011 ⭐, iter 3 = đang chạy

### Deliverables file map (with filesize sanity check)

| File | Size | Content | Status |
|---|---|---|---|
| `intent_classifier.onnx` | 1.2MB | **LSTM 98.4%** | ⭐ canonical Phase A |
| `intent_classifier_meta.json` | 80KB | LSTM vocab | ⭐ |
| `intent_classifier_winner.txt` | 42 B | "arch=lstm acc=98.4%..." | ⭐ |
| `lstm_intent.onnx` | 1.2MB | LSTM 98.4% | per-arch best |
| `fasttext_intent.onnx` | **942KB** | **FastText 95.3%** (NOT LSTM!) | ⚠️ legacy name, conflict |
| `fasttext_intent_meta.json` | 80KB | FastText vocab | ⚠️ |
| `transformer_intent.onnx` | 991KB | Transformer pre-loop (87.5% trên test cũ, 18.8% test khó) | stale |
| `soldier.onnx` | 80KB | Phase B v3.1 (random env) | ⭐ |
| `soldier.meta.json` | 454 B | obs/action contract | ⭐ |
| `soldier_v2_fixedenv.onnx` | 24KB | v2 backup ([64,64], fixed env, reward 6.126) | backup |
| `responses.json` | 3.3KB | 8 intent × 4 reply mẫu | static |
| `NPCDialogueBrain.cs` | 16KB | Unity wrapper Phase A | static |
| `MovementAgent.cs` | 6.6KB | Unity wrapper Phase B | static |

> [!warning] Filename conflict — quan trọng cho Quyền
> File `fasttext_intent.onnx` HIỆN TẠI chứa FastText weights (size 942KB). File `intent_classifier.onnx` chứa LSTM weights (size 1.2MB). Vì legacy name `fasttext_intent.onnx` xuất hiện trong HANDOFF.md cũ, có 2 cùng địa chỉ.
>
> **Khi nào nó flip-flop**: per-arch FastText train xong → ghi vào `fasttext_intent.onnx` (FastText). OVERALL LSTM win → cũng ghi vào `fasttext_intent.onnx` (LSTM, "legacy alias"). Cái nào ghi sau thắng. **Khắc phục**: Quyền PHẢI dùng `intent_classifier.onnx`, không dùng `fasttext_intent.onnx`.

## Multi-instance protocol

Khi 2 máy chạy 2 Claude instances cùng lúc:

### Master vs Reader
- **Master instance** = instance đang chạy `overnight_loop.py` qua nohup background
  - Sở hữu: tiến trình loop, state file, log file
  - Có quyền: edit code Phase A/B, kill/restart loop
- **Reader instance** = instance khác mở wiki + trợ giúp
  - Đọc-only: log, state, deliverables
  - Có quyền: update wiki content, write analysis/decisions
  - KHÔNG có quyền: kill loop, edit script đang chạy, regenerate dataset (sẽ race condition với Master)

### Sync protocol
1. Master update log + state → file system (atomic write per Python's open()+write())
2. Reader đọc log/state để cập nhật wiki
3. Tránh: 2 instance cùng restart loop → chỉ 1 master tại 1 thời điểm
4. Tránh: 2 instance cùng append vào `overnight_v3.log` → chỉ master ghi log

### File lock points
- `overnight_v3.log` — chỉ master ghi; reader chỉ đọc
- `overnight_v3_state.json` — chỉ master ghi; reader chỉ đọc
- `phase_a_sentis/data/intents_v3.csv` — generator regen mỗi iter, không read concurrent từ instance khác trong khi master đang train
- `phase_b_movement/logs/training_runs.csv` — chỉ master append

## Update log của trang này
- 2026-05-07 09:06 — iter 1 LSTM 98.4% canonical updated
- 2026-05-07 09:42 — iter 2 FastText 95.3% (data scale fix), per-arch update gây conflict tên file legacy
- 2026-05-07 10:01 — wiki sync, page Live Status mới tạo cho multi-instance protocol
- 2026-05-07 10:30 — iter 3 Transformer 95.3% (data scale cứu Transformer khỏi 18.8% → 95.3%)
- 2026-05-07 10:14 — Phase B PPO iter 2 = 6.011 (vượt iter 1 = 5.57), canonical soldier.onnx update
- 2026-05-07 10:43 — push commit `1378e9b`: heavy machine-2 + merge tool
- 2026-05-07 10:53 — wiki sync với current state cho máy 2 trước khi deploy
- 2026-05-07 11:09 — máy 2 launch attempt 1: stall do generator O(N²)
- 2026-05-07 11:18 — fix `generate_dataset_v3.py` (O(N²) → O(N)), 200k samples 30+min → 3sec
- 2026-05-07 11:19 — máy 2 launch attempt 2: success, iter 1 đang chạy
- 2026-05-07 12:41 — máy 2 iter 1 Phase B h1_baseline = 6.236 ⭐ (vượt máy 1 best 6.011)
- 2026-05-07 14:01 — máy 2 iter 2 Phase B h2_bigexplore = 5.675 (worse), Phase A timeout 2× iter
- 2026-05-07 14:03 — máy 2 RESTART với `PHASE_A_ARCHS=[]` skip Phase A, resume HP cycle từ h3
- 2026-05-07 17:48 — máy 2 iter 7 Phase B h3_deepfocus = **6.572** ⭐ (NEW best, vượt h1 baseline 6.236)
- 2026-05-07 18:59 — máy 2 loop CLEAN EXIT (deadline reached). Final best: 6.572 h3_deepfocus
- 2026-05-07 19:38 — wiki sync với final HP grid 4 configs × 2 seeds

## Backlinks

- [[overview]]
- [[technical/training-pipeline]]
- [[claims]]

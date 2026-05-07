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

## Snapshot lúc 2026-05-07 10:53

### Loop info máy 1
- Master script: `AI_Training/overnight_loop.py`
- Started: 09:00:00 (deadline 19:00:00 — còn ~8h)
- Iter hiện tại: **3** (Phase B 1M PPO sắp xong ~10:55)
- Config: `PHASE_A_TARGET=5000`, `PHASE_B_STEPS=1_000_000`, archs cycle `[lstm, fasttext, transformer]`

### Loop info máy 2 (HEAVY)
- Master script: `AI_Training/overnight_loop_machine2.py`
- Status: **chưa chạy** — đợi user clone repo + setup venv
- Cấu hình: 200k samples (5× máy 1), 2M PPO × 4 HP cycle
- Chi tiết: [[decisions/two-machine-parallel]]

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

## Backlinks

- [[overview]]
- [[technical/training-pipeline]]
- [[claims]]

---
title: Log
category: log
created: 2026-05-07
updated: 2026-05-07
---

# TrainAI Unity — Log

Chronological record of all wiki operations.

## [2026-05-07] init | Wiki initialized

- Created `.wiki/` structure inside `D:\OutSources\Unity_AI\TrainAI_Unity\`
- Copied raw sources to `raw/`:
  - `raw/gdd/context_v1.md` — handoff đầu tiên (kế hoạch trên giấy)
  - `raw/gdd/context_v2.md` — handoff v2 (đã có v1 baseline + overfit fix plan)
  - `raw/technical/handoff_morning.md` — HANDOFF.md sáng 7/5
  - `raw/technical/overnight_v2.log` — log v2 (trimmed 250 dòng đầu)
  - `raw/technical/overnight_v3.log` — log v3 (chưa hoàn tất, loop đang chạy)
  - `raw/technical/phase_b_runs.csv` — Phase B PPO run history
- Pages created (15):
  - `overview.md`
  - `systems/sentis-chat.md`, `systems/movement-ai.md`
  - `entities/commander-npc.md`, `entities/soldier-npc.md`
  - `world/scope.md`
  - `technical/training-pipeline.md`, `technical/unity-integration.md`,
    `technical/architecture-comparison.md`, `technical/data-generation.md`
  - `decisions/standalone-ppo-not-ml-agents.md`,
    `decisions/lstm-canonical-not-fasttext.md`,
    `decisions/eval-set-must-be-real.md`,
    `decisions/scope-cuts.md`
  - `bugs/fasttext-overfit-narrow-test.md`,
    `bugs/loop-spin-near-deadline.md`
  - `analysis/evolution-v1-to-v3.1.md`
- Meta updated: `index.md`, `claims.md`, `open-questions.md`
- Ready for `qmd embed`

## [2026-05-07] note | Loop v3.1 đang chạy ngầm

- Background `overnight_loop.py` chạy từ 09:00, deadline 19:00
- Iter 1 vừa xong: LSTM 98.4% real-world (63/64), canonical updated
- Phase B 1M PPO đang chạy ~25 min
- Wiki sẽ cần update sau khi loop xong (best metrics + final deliverables)

## [2026-05-07 10:01] sync | Wiki update sau iter 1 + 2

- Iter 1 hoàn tất: Phase A LSTM 98.4% (63/64), Phase B 1M PPO mean_reward = 5.572
- Iter 2 in progress: Phase A FastText train 40k samples → 95.3% (huge jump từ 25% với 16k!)
- New: [[live-status]] để 2 Claude instance sync với nhau
- Updated: claims.md (thêm c-20260507-11), analysis/evolution-v1-to-v3.1.md
- Phát hiện: file name collision `fasttext_intent.onnx` — per-arch fasttext write vs OVERALL legacy alias write cùng filename. Documented in [[live-status]].
- Recommend Quyền: dùng `deliverables/intent_classifier.onnx` (canonical, đảm bảo LSTM), KHÔNG dùng `fasttext_intent.onnx` (có thể flip).

## [2026-05-07 10:30] note | Iter 3 — Transformer cũng 95.3%

- Iter 2 Phase B PPO: 6.011 ⭐ (vượt iter 1 = 5.572), canonical soldier.onnx updated
- Iter 3 Phase A Transformer: 95.3% (jump từ 18.8% với 16k → 95.3% với 40k)
- Cả 3 archs giờ đều ≥95% với 40k data → confirms data > arch
- claims.md: thêm c-20260507-13, 14
- evolution.md: thêm iter 2/3 details + snapshot table

## [2026-05-07 10:43] commit | Heavy machine-2 + merge tool

- New: `AI_Training/overnight_loop_machine2.py` HEAVY config (200k samples, 4 HP cycle, 2M PPO)
- New: `AI_Training/merge_machine_results.py` end-of-day winner picker
- Wiki: [[decisions/two-machine-parallel]] updated với heavy config table
- claims.md: thêm c-20260507-15
- Pushed: commit `1378e9b` lên GitHub. Máy 2 chỉ cần git clone + run.

## [2026-05-07 10:53] sync | Pre-deploy máy 2 wiki refresh

- live-status.md: refresh snapshot iter 3 progress
- overview.md: bảng best metrics + multi-instance note
- claims.md: 4 claims mới (c-20260507-13, 14, 15)
- evolution.md: iter 2/3 detail + snapshot tables
- Sẵn sàng cho user deploy máy 2 (đợi máy 1 iter 3 PPO xong ~10:55)

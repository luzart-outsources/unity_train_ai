---
title: Project Overview
category: overview
tags: [unity, ai, sentis, ppo, intent-classification, vietnamese]
sources: [raw/context_v1.md, raw/context_v2.md, raw/HANDOFF.md]
created: 2026-05-07
updated: 2026-05-07
---

# TrainAI Unity — Project Overview

## Bối cảnh

ĐATN của **Nguyễn Mạnh Quyền** (CT060236, Học viện Kỹ thuật Mật mã, 1/2026 – 5/2026): *"Xây dựng trò chơi giáo dục mô phỏng học kỳ quân đội sử dụng Unity + AI"*. Game mô phỏng trải nghiệm sinh viên KTMM khi học quân đội, với 2 hệ AI:

1. **Sentis chat NPC** — sĩ quan chỉ huy hiểu tiếng Việt, phân loại intent, phản hồi
2. **Movement AI NPC** — lính NPC tự đi từ điểm A đến B, tránh chướng ngại vật

User của wiki này phụ trách **TRAIN 2 model AI** (việc của tôi). Quyền lo phần Unity scene + code game.

## Game

- **Engine**: Unity 6 (`6000.2.8f1`) + `com.unity.ai.inference` 2.6.1 (Sentis renamed)
- **Genre**: Educational simulation, life-sim
- **Platform**: PC (đủ cho ĐATN demo)
- **Team size**: 1 (Quyền) + 1 (AI helper)
- **Deadline**: cuối tháng 5/2026

## Core pillars

1. **Vòng lặp ngày demo** (ưu tiên 7 ngày, không bắt buộc 30): tập sáng → đi học (fade) → buổi tối Player Choice → ngủ
2. **3 chỉ số Adaptive**: Kỷ luật / Thể lực / Kiến thức
3. **2 endings**: Pass / Fail dựa trên chỉ số tích lũy
4. **AI hỗ trợ immersion**: chỉ huy nói tiếng Việt + lính tự di chuyển

## Current state — END OF DAY (2026-05-07 19:00, cả 2 máy completed)

AI training v3.1 hoàn tất ở 19:00 — máy 1 stop 18:59, máy 2 stop 18:59 (cùng deadline).

### Final winners (combined cả 2 máy)

| Phase | Winner | Best | At | Source |
|---|---|---|---|---|
| **Phase A LSTM** ⭐ | máy 1 | 63/64 = **98.44%** | iter 1, seed 1000 | `lstm_intent.onnx` (= `intent_classifier.onnx` canonical) |
| **Phase A FastText** ⭐ | máy 1 | 63/64 = **98.44%** | iter 17, seed 1016 | `fasttext_intent.onnx` — match LSTM sau 8× seed |
| Phase A Transformer | máy 1 | 62/64 = 96.88% | iter 18 | `transformer_intent.onnx` |
| **Phase B PPO** ⭐⭐ WINNER | máy 2 | reward **6.572** | h3_deepfocus iter 7, seed 7006 | `deliverables_m2/soldier_m2.onnx` ⭐ |
| Phase B máy 1 runner-up | máy 1 | reward 6.569 | iter 14, seed 2013 | `soldier.onnx` (chỉ thua máy 2 = 0.003!) |
| Phase B v2 backup | n/a | reward 6.126 | fixed env | `soldier_v2_fixedenv.onnx` |

→ **Phân vai theo strength**: Máy 1 nhanh → giành Phase A (3 archs cycle 27 iters). Máy 2 HEAVY → giành Phase B (HP cycle 4 configs × 2 seeds, 10M PPO steps tổng).

### Máy 2 HP grid breakdown (Phase B)
| HP | Net | Ent | LR | Best Reward | Verdict |
|---|---|---|---|---|---|
| **h3_deepfocus** ⭐ | [128,128,64] | 0.005 | 1e-4 | **6.572** | conservative thắng |
| h2_bigexplore | [256,128] | 0.02 | 3e-4 | 6.263 | tốt vừa |
| h1_baseline | [128,128] | 0.01 | 3e-4 | 6.236 | baseline |
| h4_bigwide | [256,256] | 0.05 | 5e-4 | 5.252 | flop — bigger net + high ent hurt |

→ **Bài học HP**: conservative beats brute. Deeper net + low ent + low lr > bigger net + high ent.

Chi tiết tức thời: [[live-status]].

Chi tiết per-system: [[systems/sentis-chat]], [[systems/movement-ai]].

> [!info] 2-machine parallel — COMPLETED
> Final canonical sau merge: `intent_classifier.onnx` = LSTM 98.44% (máy 1), `soldier.onnx` cần update từ `soldier_m2.onnx` (máy 2). Chạy `AI_Training/merge_machine_results.py` để auto-pick winner. Setup: [[decisions/two-machine-parallel]].

## Key systems

- [[systems/sentis-chat]] — NPC chỉ huy: tiếng Việt → 8 intent → response template
- [[systems/movement-ai]] — Lính NPC: nav 2D với obstacles, output ONNX cho Unity

## Key entities

- [[entities/commander-npc]] — Sĩ quan chỉ huy (consumer của Sentis chat)
- [[entities/soldier-npc]] — Lính NPC (consumer của Movement AI)

## Key technical

- [[technical/training-pipeline]] — generate_dataset_v3 → train → eval → export ONNX
- [[technical/unity-integration]] — Cách Quyền load model qua InferenceEngine
- [[technical/architecture-comparison]] — FastText vs LSTM vs Transformer trên test khó

## Key decisions

- [[decisions/standalone-ppo-not-ml-agents]] — Train Phase B bằng Python thay ML-Agents
- [[decisions/lstm-canonical-not-fasttext]] — Đổi canonical từ FastText sang LSTM
- [[decisions/eval-set-must-be-real]] — Test 16 câu cũ misleading, đổi 64 câu khó

## Open questions

- Quyền có chốt 8 intent chính thức không? (đang tạm dùng list trong Sentis chat)
- Bao giờ test thực tế trong Unity scene? (chưa)
- Deploy Phase B model trên scene doanh trại thật vs training arena? (chưa)

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

## Current state (FINAL snapshot — 2026-05-07 19:38, máy 2 loop completed)

AI training v3.1 overnight loop hoàn tất 19:00 7/5/2026 (máy 1 + máy 2 song song HEAVY):

| Phase | Best | Source |
|---|---|---|
| **Phase A LSTM** ⭐ | 63/64 = **98.4%** (iter 1) | máy 1 — canonical `intent_classifier.onnx` |
| Phase A FastText | 61/64 = 95.3% (iter 2) | máy 1 — per-arch best |
| Phase A Transformer | 61/64 = 95.3% (iter 3) | máy 1 — per-arch best |
| **Phase B v3.1 FINAL** ⭐ | mean_reward **6.572** (h3_deepfocus, 2M steps, seed 7006) | máy 2 — `soldier_m2.onnx` |
| Phase B máy 1 best | mean_reward 6.011 (iter 2, 1M steps) | máy 1 — `soldier.onnx` |
| Phase B v2 backup | mean_reward 6.126 (fixed env, ref only) | `soldier_v2_fixedenv.onnx` |

Cả 3 archs đạt ~95-98% với 40k data → **data scale > arch complexity**.

Máy 2 HP grid winner = h3_deepfocus (deeper net + low ent + low lr) → **conservative HP > brute capacity** trên scale 2M PPO.

Chi tiết tức thời: [[live-status]].

Chi tiết per-system: [[systems/sentis-chat]], [[systems/movement-ai]].

> [!info] 2-machine parallel — COMPLETED
> Máy 1 lo Phase A (GPU advantage), máy 2 lo Phase B HP exploration (HEAVY: 2M PPO × 4 HP × 2 seeds). Final winners: máy 1 LSTM 98.4% Phase A, máy 2 h3_deepfocus 6.572 Phase B. Phân vai theo strength → mỗi máy dominates đúng phase. Setup: [[decisions/two-machine-parallel]]. Merge tool: `AI_Training/merge_machine_results.py`.

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

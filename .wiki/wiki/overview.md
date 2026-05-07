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

## Current state (live snapshot — last updated 2026-05-07 10:53)

AI training v3.1 overnight loop running on máy 1 (started 09:00, deadline 19:00 7/5 — còn ~8h):

| Phase | Best | Status |
|---|---|---|
| **Phase A LSTM** ⭐ | 63/64 = **98.4%** (iter 1) | canonical `intent_classifier.onnx` |
| Phase A FastText | 61/64 = 95.3% (iter 2) | per-arch best |
| Phase A Transformer | 61/64 = 95.3% (iter 3) | per-arch best |
| **Phase B v3.1** | mean_reward **6.011** (iter 2, 1M steps) | `soldier.onnx` 80KB |
| Phase B v2 backup | mean_reward 6.126 (fixed env, ref only) | `soldier_v2_fixedenv.onnx` |

Cả 3 archs đạt ~95-98% với 40k data → **data scale > arch complexity**.

Chi tiết tức thời: [[live-status]].

Chi tiết per-system: [[systems/sentis-chat]], [[systems/movement-ai]].

> [!info] 2-machine parallel
> Plan chạy 2 máy parallel: máy 1 (đang chạy) iteration nhanh + 3 archs cycle, máy 2 (chưa start) **HEAVY**: 200k data, 2M PPO × 4 HP cycle. Cuối ngày `merge_machine_results.py` auto-pick winner. Setup chi tiết: [[decisions/two-machine-parallel]].

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

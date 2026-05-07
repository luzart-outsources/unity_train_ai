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

## Current state — END OF DAY MÁY 1 (2026-05-07 19:36)

Loop `overnight_loop.py` máy 1 kết thúc tự nhiên ở 18:59 (deadline reached). 27 iters total, ~10h training.

### Final bests máy 1

| Phase | Best | Iter | File |
|---|---|---|---|
| **Phase A LSTM** ⭐ | 63/64 = **98.44%** | 1 | `intent_classifier.onnx` (canonical) + `lstm_intent.onnx` |
| **Phase A FastText** ⭐ | 63/64 = **98.44%** | **17** | `fasttext_intent.onnx` — match LSTM sau 8× seed |
| Phase A Transformer | 62/64 = 96.88% | 18 | `transformer_intent.onnx` |
| **Phase B PPO** ⭐ | reward **6.569** | **14** | `soldier.onnx` — break plateau 6.0! |
| Phase B v2 backup | reward 6.126 | (fixed env) | `soldier_v2_fixedenv.onnx` |

→ **Cả 3 archs Phase A đều ≥96.9%**, LSTM/FastText ngang nhau 98.44%. Confirm "data scale > arch given enough seed".

### Máy 2 — last known từ commit `ff5affc` (14:05)
- Decision: skip Phase A (timeout 40 min). Focus 100% Phase B HP cycle.
- Best Phase B máy 2: **6.236** (iter 1 h1_baseline, 12:41)
- Status sau 14:05 không rõ — đợi push branch.

Chi tiết tức thời: [[live-status]].

Chi tiết per-system: [[systems/sentis-chat]], [[systems/movement-ai]].

> [!info] Bước tiếp theo
> Khi máy 2 push xong branch `machine-2-results`, chạy `merge_machine_results.py` để auto-pick winner cuối cùng giữa 2 máy.

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

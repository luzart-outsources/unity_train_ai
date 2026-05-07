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

## Current state — Phase A v2 deployed (2026-05-07 23:00)

Sau khi training v3.1 đóng deadline 19:00, user feedback "AI ngu, hỏi khác 1 tý dính,
hỏi khu A trả lời khu B". Build hard test 216 câu (10 category) → V1 LSTM chỉ 38%
(vs 98.4% test 64 câu cũ — synthetic-friendly). Triển khai Phase A v2 trong 3h
buổi tối. Chi tiết: [[decisions/phase-a-v2-iteration]].

### Final winners

| Phase | Model | Hard test 216 / Best | Source |
|---|---|---|---|
| **Phase A v1** (giữ để A/B compare) | LSTM v3 (116K params) | 38.0% (98.4% test cũ) | `intent_classifier.onnx` |
| **Phase A v2** ⭐ (deployed) | LSTM v5 (707K params) | **96.8%** (209/216) | `intent_classifier_v2.onnx` |
| **Phase A v2 EntityExtractor** | rule-based, 715 phrases | (orthogonal — fix "khu A → khu B5") | `EntityExtractor.cs` + `slot_vocab.json` |
| **Phase B PPO** ⭐⭐ | máy 2, h3_deepfocus | reward **6.572** | `deliverables_m2/soldier_m2.onnx` |
| Phase B máy 1 runner-up | máy 1, iter 14 seed 2013 | reward 6.569 | `soldier.onnx` |
| Phase B v2 backup | fixed env | reward 6.126 | `soldier_v2_fixedenv.onnx` |

→ **V2 stack**: 1 model (LSTM v5, 96.8%) + 1 rule-based slot extractor (715 phrases) + entity-aware response templates. V1 giữ nguyên để compare. Menu Unity: **AI/4. Phase A — Compare V1 vs V2**.

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
- [[systems/entity-extractor]] — Phase A v2: rule-based slot extraction, fix entity-aware responses
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
- [[decisions/phase-a-v2-iteration]] — Eval-driven iteration v3→v4→v5 đẩy 38%→96.8%

## Open questions

- Quyền có chốt 8 intent chính thức không? (đang tạm dùng list trong Sentis chat)
- Bao giờ test thực tế trong Unity scene? (chưa)
- Deploy Phase B model trên scene doanh trại thật vs training arena? (chưa)

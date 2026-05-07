---
title: Index
category: index
created: 2026-05-07
updated: 2026-05-07
---

# TrainAI Unity — Wiki Index

Master catalog. Read first to find relevant pages.

**Entry format**: `[[category/page]] — description (N sources, M backlinks)`

## Overview
- [[overview]] — Project Overview, status hiện tại, key systems & decisions
- [[live-status]] — ⭐ Live snapshot loop + multi-instance protocol (2 máy)

## Systems
- [[systems/sentis-chat]] — Phase A NPC chat tiếng Việt: 8 intent classifier, ONNX deploy
- [[systems/movement-ai]] — Phase B lính tự đi: PPO, env random, ONNX deploy

## Entities
- [[entities/commander-npc]] — Sĩ quan chỉ huy (consumer Sentis chat)
- [[entities/soldier-npc]] — Lính nền tự đi (consumer Movement AI)

## World
- [[world/scope]] — Game loop, stats Adaptive, 2 endings, scope cắt 8 mini-game

## Technical
- [[technical/training-pipeline]] — overnight_loop.py orchestration end-to-end
- [[technical/data-generation]] — Vietnamese template generator v3 (60+ pools)
- [[technical/architecture-comparison]] — FastText vs LSTM vs Transformer trên test khó
- [[technical/unity-integration]] — Cách load ONNX qua InferenceEngine 2.6 (Unity 6)

## Decisions
- [[decisions/standalone-ppo-not-ml-agents]] — Train Phase B Python thay ML-Agents Unity
- [[decisions/lstm-canonical-not-fasttext]] — Đổi canonical Phase A: LSTM thay FastText
- [[decisions/eval-set-must-be-real]] — Test 16 câu cũ misleading, đổi 64 câu khó
- [[decisions/scope-cuts]] — Cắt 8 mini-game
- [[decisions/two-machine-parallel]] — Chạy 2 máy song song (config + sync protocol)

## Bugs
- [[bugs/fasttext-overfit-narrow-test]] — FastText "100%" lừa dối trên test 16 câu (root: mean-pool)
- [[bugs/loop-spin-near-deadline]] — Loop spin 493k iter gần deadline (root: thiếu sleep guard)

## Analysis
- [[analysis/evolution-v1-to-v3.1]] — Lịch sử v1→v3.1 (data scale, eval rigor, loop maturity)

## Provenance (meta)
- [[claims]] — Cross-page facts với citation
- [[contradictions]] — Conflicting claims chưa resolve
- [[open-questions]] — Câu hỏi chưa trả lời

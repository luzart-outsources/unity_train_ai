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
- [[systems/sentis-chat]] — Phase A NPC chat: 8 intent classifier (v1 38% / v2 96.8% trên hard test 216 câu)
- [[systems/entity-extractor]] — Phase A v2 slot-filling: rule-based, fix "khu A → trả lời khu B5"
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
- [[decisions/phase-a-v2-iteration]] — Eval-driven iteration v3→v4→v5 (38%→96.8%)
- [[decisions/scope-cuts]] — Cắt 8 mini-game
- [[decisions/two-machine-parallel]] — Chạy 2 máy song song (config + sync protocol)

## Bugs
- [[bugs/fasttext-overfit-narrow-test]] — FastText "100%" lừa dối trên test 16 câu (root: mean-pool)
- [[bugs/loop-spin-near-deadline]] — Loop spin 493k iter gần deadline (root: thiếu sleep guard)
- [[bugs/generator-on2-quadratic]] — Generator O(N²) blocker máy 2 HEAVY (30+ min → 3s sau fix)
- [[bugs/unity-integration-bugs]] — 6 bugs Phase A/B integration (coord, tokenization, Input System, UI)

## Analysis
- [[analysis/evolution-v1-to-v3.1]] — Lịch sử v1→v3.1 (data scale, eval rigor, loop maturity)

## Sources
- [[sources/phase-a-v2-report]] — báo cáo iteration v3→v4→v5→v6 với numbers cụ thể

## Provenance (meta)
- [[claims]] — Cross-page facts với citation
- [[contradictions]] — Conflicting claims chưa resolve
- [[open-questions]] — Câu hỏi chưa trả lời

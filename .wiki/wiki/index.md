---
title: Index
category: index
created: 2026-05-07
updated: 2026-05-11
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
- [[technical/datn-architecture]] — Kiến trúc template farming đã import (legacy reference)
- [[technical/gdd-ingame-tech-design]] — ⭐ Technical design SO-driven cho GDD học kỳ quân đội (Phần I — gameplay)
- [[technical/gdd-ingame-luzart-integration]] — ⭐ Phần II — tích hợp NinjaUI + Tween + Select + Attributes

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

## NinjaUI Framework (Luzart)

User-owned UI framework riêng (không thuộc DATN). Async-first, lane-based.

- [[systems/ninjaui-framework]] — UIManager + lane stack + async lifecycle
- [[decisions/remove-addressables-add-unitask]] — bỏ Addressables, dùng direct prefab + UniTask (2026-05-11)

## DATN Game (imported 2026-05-11)

Code game farming life-sim của Quyền clone từ `manhquyenkma/DATN`. ~22 system, Stardew template. Wiki các system + architecture:

- [[technical/datn-architecture]] — singleton + ITimeTracker + SO data + GameBlackboard
- [[systems/datn-time-weather]] — đồng hồ + 4 mùa + weather probability
- [[systems/datn-farming]] — crop lifecycle Soil→Farmland→Watered→Harvest
- [[systems/datn-inventory-tools]] — 8+8 slot, ItemData/EquipmentData/SeedData SO
- [[systems/datn-scene-locations]] — 10 scene + transition + lock
- [[systems/datn-dialogue-cutscene]] — DialogueManager + Cutscene SO + SoCollection
- [[systems/datn-npc-festivals]] — NPC schedule + festival event ngày
- [[systems/datn-animals-economy]] — chicken/egg + Shop + ShippingBin + PlayerStats
- [[systems/datn-blackboard-save]] — GameBlackboard + BinaryFormatter save
- [[decisions/import-datn-game-base]] — quyết định clone DATN làm base

## Sources
- [[sources/phase-a-v2-report]] — báo cáo iteration v3→v4→v5→v6 với numbers cụ thể
- [[sources/datn-game-repo]] — DATN GitHub repo + structure import

## Provenance (meta)
- [[claims]] — Cross-page facts với citation
- [[contradictions]] — Conflicting claims chưa resolve
- [[open-questions]] — Câu hỏi chưa trả lời

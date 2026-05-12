---
title: TrainAI — Project Overview
category: overview
tags: [overview, gdd, military-academy]
sources: [raw/gdd/gdd_inGame.txt, raw/gdd/context_v2.md]
created: 2026-05-12
updated: 2026-05-12
---

# TrainAI — Project Overview

Game mô phỏng học kỳ quân sự (đề tài ĐATN của Nguyễn Mạnh Quyền). Người chơi sống qua 30 ngày trong doanh trại, làm nhiệm vụ theo thời khóa biểu, học các môn quốc phòng qua quiz, chat với NPC chỉ huy (intent classifier ONNX), nhìn NPC học sinh tự đi theo schedule (PPO movement ONNX).

## Game

- **Engine**: Unity 6 + `com.unity.ai.inference` 2.6.1 (AI Inference Engine, formerly Sentis, namespace `Unity.InferenceEngine`) + UniTask + Input System
- **Genre**: Life-sim / educational (single player)
- **Platform**: PC (mobile-friendly, joystick HUD support)
- **Perspective**: 3D third-person, quest arrow under player feet

## Core pillars

1. **Day-cycle simulation** — 30 ngày, mỗi ngày một bộ quest theo timeline cố định, 1h game = 3p thực.
2. **AI tích hợp** — NPC chỉ huy chat bằng intent classifier ONNX; NPC học sinh tự đi bằng PPO ONNX.
3. **SO-First Modular** — mọi logic config qua ScriptableObject; thêm quest/NPC/môn = tạo asset, không sửa core.
4. **Strategy swappable** — AI thật ↔ fake (NavMesh) đổi qua 1 field SO, dùng cho QA và demo.
5. **Automation-buildable** — sau khi system stable, 1 button master generate toàn bộ 30 ngày từ JSON template.

## Current state

- Spec design đã viết: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md](../../docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md)
- ONNX models đã có trong `AI_Training/deliverables/`: `intent_classifier.onnx` (LSTM, 98.44% val_acc), `soldier.onnx` (PPO, reward 6.572)
- Repository vừa wipe sạch cũ — implementation chưa bắt đầu

## Key systems

- [[systems/quest-system]] — 5 quest types, polymorphic SO
- [[systems/interaction-system]] — interactable trigger + InteractionSO
- [[systems/npc-movement]] — `IMovementService` + Strategy SO swap
- [[systems/npc-dialogue]] — intent classifier + response filler
- [[systems/score-system]] — học tập + rèn luyện, rule SO
- [[systems/scene-flow]] — additive load, sub-scene
- [[systems/ui-router]] — uGUI popup catalog
- [[systems/save-load]] — JSON DTO
- [[systems/broadcast-service]] — typed pub/sub
- [[systems/sentis-runtime]] — shared ONNX worker

## Key entities

- [[entities/player]] — third-person capsule, quest arrow
- [[entities/npc-commander]] — idle + OnnxDialogue
- [[entities/npc-student]] — OnnxMovement + schedule

## Key technical pages

- [[technical/architecture]] — layered, dependency direction
- [[technical/asmdef-structure]] — 1-direction asmdef
- [[technical/so-taxonomy]] — base/concrete/data/RSO/DB
- [[technical/automation-tooling]] — Editor master builder
- [[technical/placeholder-scale]] — cube convention

## Decisions (locked)

See [[decisions/d01-so-first-modular]], [[decisions/d02-broadcast-service]], [[decisions/d03-strategy-swap-ai]], [[decisions/d04-ugui-not-toolkit]], [[decisions/d05-third-person-3d]], [[decisions/d06-no-socollection]], [[decisions/d07-manual-prefab-override]], [[decisions/d08-automation-last]], [[decisions/d09-sentis-per-model-worker]], [[decisions/d10-json-save]], [[decisions/d11-character-controller]].

## Open questions

See [[open-questions]].

---
title: Wiki Index
category: index
tags: [index]
sources: []
created: 2026-05-12
updated: 2026-05-12
---

# TrainAI — Wiki Index

Master catalog. Read first to find relevant pages.

## Overview
- [[overview]] — Project overview, pillars, current state

## Provenance ledger
- [[claims]] — cross-page facts with citations
- [[contradictions]] — conflicting claims kept until resolved
- [[open-questions]] — unanswered design/tech questions

## Sources
- [[sources/gdd-ingame]] — full GDD (raw/gdd/gdd_inGame.txt)
- [[sources/context-v2]] — handoff context AI training
- [[sources/merged-report]] — Phase A/B deliverables
- [[sources/intent-classifier-meta]] — chat ONNX I/O
- [[sources/soldier-meta]] — movement ONNX I/O
- [[sources/responses-json]] — intent → response template

## Systems
- [[systems/quest-system]] — polymorphic QuestSO + 5 types
- [[systems/interaction-system]] — InteractionSO + router
- [[systems/npc-movement]] — Strategy swap + Sentis
- [[systems/npc-dialogue]] — tokenizer + intent + filler
- [[systems/score-system]] — ScoreRuleSO chain
- [[systems/scene-flow]] — additive load + sub-scene
- [[systems/ui-router]] — uGUI popup catalog
- [[systems/save-load]] — JSON DTO + migrator
- [[systems/broadcast-service]] — typed static pub/sub
- [[systems/sentis-runtime]] — shared ONNX worker

## Entities
- [[entities/player]] — third-person + arrow
- [[entities/npc-commander]] — chat NPC
- [[entities/npc-student]] — movement NPC

## Technical
- [[technical/architecture]] — layered architecture
- [[technical/asmdef-structure]] — 1-direction asmdef
- [[technical/so-taxonomy]] — SO/RSO/DB conventions
- [[technical/automation-tooling]] — Editor master builder
- [[technical/placeholder-scale]] — cube placeholder scale
- [[technical/sentis-runtime-notes]] — Sentis Worker gotchas

## Decisions
- [[decisions/d01-so-first-modular]]
- [[decisions/d02-broadcast-service]]
- [[decisions/d03-strategy-swap-ai]]
- [[decisions/d04-ugui-not-toolkit]]
- [[decisions/d05-third-person-3d]]
- [[decisions/d06-no-socollection]]
- [[decisions/d07-manual-prefab-override]]
- [[decisions/d08-automation-last]]
- [[decisions/d09-sentis-per-model-worker]]
- [[decisions/d10-json-save]]
- [[decisions/d11-character-controller]]
- [[decisions/d12-direct-input-api]]
- [[decisions/d13-ui-router-facade]]
- [[decisions/d14-joystick-pack]]

## Bugs (playtest reports)
- [[bugs/playtest-2026-05-13]] — 6 critical gameplay bugs found via MCP Play test

## Log
- [[log]]

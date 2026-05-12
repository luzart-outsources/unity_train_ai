---
title: Claims Ledger
category: meta
tags: [provenance]
sources: []
created: 2026-05-12
updated: 2026-05-12
---

# Claims Ledger

Cross-page factual claims with citations. Each claim has stable ID `c-YYYYMMDD-NN`.

## Active claims (newest first within year)

### c-20260512-01 — Game lasts 30 days, skipping Sat+Sun at end of week
- **Sources**: `raw/gdd/gdd_inGame.txt:67`
- **Status**: active
- **First seen**: 2026-05-12

### c-20260512-02 — 1h game = 3p real time
- **Sources**: `raw/gdd/gdd_inGame.txt:59`
- **Status**: active

### c-20260512-03 — Starting score: 100 rèn luyện, 0 học tập; max học tập = 480
- **Sources**: `raw/gdd/gdd_inGame.txt:46,58`
- **Status**: active

### c-20260512-04 — Idle ≥15min in-game without reaching active quest area → −5 rèn luyện
- **Sources**: `raw/gdd/gdd_inGame.txt:62`
- **Status**: active

### c-20260512-05 — Ending grades: Xuất sắc (≥432/480 và ≥90/100), Tốt (288–431 và 60–90), Trung bình (dưới)
- **Sources**: `raw/gdd/gdd_inGame.txt:47-54`
- **Status**: active

### c-20260512-06 — Quiz: 10 questions × 4 answers × 15s/câu
- **Sources**: `raw/gdd/gdd_inGame.txt:28-29,71-72`
- **Status**: active

### c-20260512-07 — Intent classifier: LSTM, max_len=32, 8 classes, 98.44% val_acc (real-world eval)
- **Sources**: `AI_Training/deliverables/MERGED_REPORT.md`, `intent_classifier_meta.json`
- **Status**: active
- **Notes**: handoff v2 reported overfit with prior version; new lstm winner claims better generalization. To verify in Phase 5 (see [[open-questions]] q-20260512-10).

### c-20260512-08 — Soldier movement: PPO, obs_dim=21 (8 ray_dist + 8 ray_hit + vel(2) + dir(2) + dist), act_dim=2 (thrust+turn), max_speed=3.5 m/s, ray_max=10m, arena=20m
- **Sources**: `AI_Training/deliverables/soldier.meta.json`
- **Status**: active

### c-20260512-09 — 8 intents: HOI_LICH, HOI_GIO_AN, HOI_VI_TRI, HOI_KIEN_THUC, BAO_CAO, XIN_PHEP, TAM_BIET, OUT_OF_SCOPE
- **Sources**: `raw/gdd/context_v2.md:72-80`, `responses.json` keys
- **Status**: active

### c-20260512-10 — Response templates use placeholders {scheduled_today}, {meal_time}, {place}, {topic}, etc., filled at runtime
- **Sources**: `AI_Training/deliverables/responses.json`
- **Status**: active

### c-20260512-11 — User-required: "config cực kỳ sâu, mọi thứ SO, code SOLID, mở rộng nhanh nhất"
- **Sources**: brainstorm transcript 2026-05-12
- **Status**: active

### c-20260512-12 — Event backbone = `BroadcastService` (typed static pub/sub), NOT GameEventSO
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-13 — No `SoCollection`; databases use plain `List<T>` + Editor "Auto-populate"
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-14 — No UI Toolkit; uGUI is the UI stack
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-15 — No `PlaceholderRegistrySO`; placeholder materials per-prefab manually
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-16 — Automation tooling = last phase, after vertical slice stable
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-17 — Game = 3D third-person; quest arrow child of player at local (0, 0.05, 0.4)
- **Sources**: brainstorm transcript
- **Status**: active

### c-20260512-18 — `com.unity.ai.inference 2.6.1` (Unity 6 AI Inference Engine, formerly Sentis) is installed
- **Sources**: `Packages/manifest.json`
- **Status**: active
- **Notes**: Namespace is `Unity.InferenceEngine` in Unity 6 (was `Unity.Sentis`).

### c-20260512-21 — Project targets Unity 6 (not Unity 2022.3)
- **Sources**: user clarification 2026-05-12
- **Status**: active
- **Notes**: Overrides context_v2.md mention of 2022.3.62f2. ONNX opset 15 models are forward-compatible.

### c-20260512-19 — UniTask installed for async/await
- **Sources**: `Packages/manifest.json`
- **Status**: active

### c-20260512-20 — New Input System installed (legacy Input removed per commit 5f88404)
- **Sources**: `Packages/manifest.json`
- **Status**: active

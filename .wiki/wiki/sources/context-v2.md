---
title: Source — Context Handoff v2
category: sources
tags: [handoff, ai-training]
source_path: raw/gdd/context_v2.md
created: 2026-05-12
updated: 2026-05-12
---

# Source — Context Handoff v2

**Path**: `.wiki/raw/gdd/context_v2.md`
**Type**: Project handoff document detailing AI training environment + status as of 2026-05-07. Vietnamese.

## Key facts extracted

- **Role split**: Quyền (author) does the Unity game; "tôi" (collaborator) does the AI training only.
- **Two models**: Phase A intent classifier (Sentis, Vietnamese) and Phase B movement (ML-Agents PPO → ONNX).
- **8 intents** (HOI_LICH, HOI_GIO_AN, HOI_VI_TRI, HOI_KIEN_THUC, BAO_CAO, XIN_PHEP, TAM_BIET, OUT_OF_SCOPE) — see [[claims#c-20260512-09]].
- **Bug**: Phase A FastText 100% val_acc but 1/8 real-world acc = vocab too small, overfit on synthetic. Later resolved by LSTM with expanded data (98.44% in MERGED_REPORT).
- **Phase B planned** but as of v2 not started; later completed (soldier PPO reward 6.572).
- **Scope cuts already made**: 8 minigames cut; classroom = fade+stat; 30 days preferred but 7 days acceptable demo. Current design opts for full 30 per user re-confirmation.

## Cited from this source

- [[claims#c-20260512-09]] 8 intents list

## Status notes (outdated by 2026-05-12)

- Path mentions `D:\GithubUnity\UnityTrainAI\` — actual current path is `D:\Unity Training\unity_train_ai\`.
- Mentions Unity 2022.3.62f2 — **current project uses Unity 6** per user clarification 2026-05-12. Models trained for ONNX opset 15 are forward-compatible.

## Backlinks
- [[overview]]
- [[open-questions#q-20260512-04]]

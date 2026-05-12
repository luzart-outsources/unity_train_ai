---
title: NPC — Đại đội trưởng (Commander)
category: entities
tags: [npc, dialogue, onnx]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# NPC Đại đội trưởng (Commander)

Static NPC. Greets player on interact ("Chào [Tên], có chuyện gì vậy?" per GDD line 88) and answers questions via intent classifier.

## NPCSO config (sample)

```yaml
id: DaiDoiTruong
displayName: "Đại đội trưởng"
spawnPos: (0, 0, 5)
movement: IdleMovementSO
dialogue: OnnxDialogueSO (confidenceThreshold: 0.5, fallback: OUT_OF_SCOPE)
schedule: null
```

## Interaction

- `InteractableSO_DaiDoiTruong` ↔ `OpenDialogueInteractionSO{npc: NPC_DaiDoiTruong}`.
- Player presses E → `UIRouter.ShowDialogue(NPC_DaiDoiTruong)`.
- Header: `"Chào " + PlayerStateRSO.playerName + ", có chuyện gì vậy?"`.
- Each Send → `DialogueService.Reply(npc, input)` (see [[systems/npc-dialogue]]).

## Why Idle

Commander has no schedule — stationary at parade square. `IdleMovementSO` keeps NPC in place but registered with `MovementService` for uniformity.

## Backlinks
- [[systems/npc-dialogue]]
- [[systems/interaction-system]]
- [[decisions/d03-strategy-swap-ai]]

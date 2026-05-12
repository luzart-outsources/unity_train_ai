---
title: Source — GDD inGame
category: sources
tags: [gdd]
source_path: raw/gdd/gdd_inGame.txt
created: 2026-05-12
updated: 2026-05-12
---

# Source — GDD inGame

**Path**: `.wiki/raw/gdd/gdd_inGame.txt` (120 lines, Vietnamese)
**Type**: Full Game Design Document — UI screens, gameplay loop, day timeline, scoring rules, scene list, quests, NPC behavior.

## Key sections

- **UI screens** (line 1-43): MainMenu, CutScene, CreateChar, InGame HUD, Loading, Quiz, Confirm, Dialogue, Ending.
- **Game mechanics** (line 44-73): 100 rèn luyện start, 0-480 học tập, 1h = 3p, 15min idle penalty, 30-day cycle skipping Sat+Sun.
- **Scenes** (line 75-90): World (doanh trại), Lớp học, Ký túc xá, Nhà ăn — with interactable details.
- **Daily quest timeline** (line 105-114): exercise 5:00 → cleanup 6:00 → meal 7:00 → study 7:30 → break 11:30 → study 14:00 → free 17:00 → sleep 18:30.
- **Advanced** (line 117): minigame replacements possible (rhythm, archery) — out of v1 scope but architecture supports via `QuestSO` subclass.

## Cited from this source

- [[claims#c-20260512-01]] 30 days, skip weekend
- [[claims#c-20260512-02]] 1h = 3p
- [[claims#c-20260512-03]] starting scores
- [[claims#c-20260512-04]] idle penalty
- [[claims#c-20260512-05]] ending grades
- [[claims#c-20260512-06]] quiz format

## Backlinks
- [[overview]]
- [[systems/quest-system]]
- [[systems/score-system]]
- [[systems/scene-flow]]
- [[systems/ui-router]]

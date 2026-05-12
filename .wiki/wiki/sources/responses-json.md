---
title: Source — responses.json
category: sources
tags: [dialogue, templates]
source_path: AI_Training/deliverables/responses.json
created: 2026-05-12
updated: 2026-05-12
---

# Source — responses.json

**Path**: `AI_Training/deliverables/responses.json`
**Type**: Intent-to-response template map. Used by `ResponseFiller` in [[systems/npc-dialogue]].

## Structure

```json
{
  "_README": "Phase A intent → response mapping. ...",
  "HOI_LICH":      ["Hôm nay đại đội ta có {scheduled_today}. ...", ...],
  "HOI_GIO_AN":    ["Sáng 6h, trưa 11h30, tối 18h. ...", ...],
  "HOI_VI_TRI":    ["{place} ở phía {direction} ...", ...],
  "HOI_KIEN_THUC": ["Về {topic}, ...", ...],
  "BAO_CAO":       ["Tốt. Tôi đã ghi nhận. ...", ...],
  "XIN_PHEP":      ["Lý do của đồng chí ...", ...],
  "TAM_BIET":      ["Thôi đồng chí đi đi. ...", ...],
  "OUT_OF_SCOPE":  ["Tôi không hiểu rõ. ...", ...]   // expected
}
```

## Placeholders

| Token | Filler source |
|---|---|
| `{scheduled_today}` | `QuestRouter.GetTodaySummary()` |
| `{meal_time}` | `TimeConfigSO` formatted |
| `{minutes_to_meal}` | `GameClock.MinutesUntil(mealHour)` |
| `{place}`, `{direction}`, `{distance}`, `{block}` | `AreaDB.NearestTo(player)` + heuristic |
| `{topic}`, `{chapter}`, `{summary}` | parsed from input text (fallback `"điều đó"`) |
| `{hocTap}`, `{renLuyen}` | `PlayerStateRSO` |

## Designer freedom

User can edit `responses.json` directly to add lines or new intents. Adding a new intent also requires retraining the classifier (see [[systems/npc-dialogue]] § Extension recipe in spec).

## Cited from this source

- [[claims#c-20260512-10]] placeholder format

## Backlinks
- [[systems/npc-dialogue]]
- [[sources/intent-classifier-meta]]

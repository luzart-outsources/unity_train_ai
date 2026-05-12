---
title: Save/Load System
category: systems
tags: [save, persistence, json]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Save / Load

JSON DTO at `Application.persistentDataPath/save_slot{N}.json`. Per [[decisions/d10-json-save]] — never persist SO assets.

## ISaveService

```csharp
public interface ISaveService {
    void Save(int slot = 1);
    bool TryLoad(int slot, out SaveDTO dto);
}
```

## Format

```json
{
  "version": 1,
  "savedAt": "2026-05-12T15:30:00",
  "playerName": "Quyen",
  "day": 7,
  "weekday": "Monday",
  "hour": 14,
  "minute": 30,
  "hocTap": 142,
  "renLuyen": 85,
  "currentScene": "10_World",
  "playerPos": {"x": 12.4, "y": 0, "z": 8.1},
  "completedQuestIds": ["Q_TheDuc_d1","Q_DonVS_d1"],
  "missedQuestIds": ["Q_LichSu_d2"]
}
```

Quest reference by `QuestSO.id` string, not asset GUID — robust to asset renames.

## Save triggers

- Auto on `DayEndedMsg`.
- Optional Save & Quit from pause menu (out of current scope, hook ready).

## Load flow

```
MainMenu Continue button
  → SaveService.TryLoad(1, out dto)
  → on success:
       PlayerStateRSO.Restore(dto)
       GameClockRSO.Restore(dto)
       DayProgressRSO.Restore(dto)
       await SceneRouter.LoadAdditive(SceneRef_World, "Đang khôi phục...")
       Player.transform.position = dto.playerPos
       QuestRouter.RestoreCompletedFor(dto.day, dto.completedQuestIds, dto.missedQuestIds)
```

## Versioning

`SaveMigrator.Migrate(int fromVersion, int toVersion, JObject)` chain. Bump `version` field on schema change.

## Backlinks
- [[decisions/d10-json-save]]
- [[systems/scene-flow]]

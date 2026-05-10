---
title: NPC & Festivals (DATN)
category: systems
tags: [datn, npc, schedule, festival, characterdata, socollection]
sources: [Assets/Scripts/Characters/, Assets/Scripts/Festivals/]
created: 2026-05-11
updated: 2026-05-11
---

# NPC & Festivals — DATN

NPC có schedule daily (đi tới location nào lúc nào), spawn/despawn theo location của player. Festival overrride schedule, đẩy NPC tới TownSquare event ngày.

## NPC core

### CharacterData (ScriptableObject)

```csharp
class CharacterData : ScriptableObject {
    string characterName;
    Sprite portrait;
    Location initialLocation;
    DialogueLine[] defaultDialogue;
    NPCScheduleData schedule;
    GameObject characterPrefab;       // model
}
```

Designer tạo asset cho mỗi NPC trong `Resources/Characters/`.

### NPCScheduleData (ScriptableObject)

```csharp
class NPCScheduleData : ScriptableObject {
    ScheduleEvent[] schedule;
}
class ScheduleEvent {
    GameTimestamp time;       // chỉ hour + minute
    Location targetLocation;
    Vector3 targetPosition;
    string animationState;    // idle, working, sleeping
}
```

Mỗi NPC có 1 array event, sort theo time. NPCManager check tick để cập nhật `npcLocations`.

### NPCManager (singleton, ITimeTracker)

Mỗi tick:
1. Foreach NPC → tìm `ScheduleEvent` mới nhất ≤ current time
2. Update `npcLocations[npc] = scheduleEvent.targetLocation`

Khi `onLocationLoad`:
1. Foreach NPC có `npcLocations[npc] == newLocation`
2. Spawn `InteractableCharacter` từ `characterPrefab` ở `targetPosition`
3. Set animation state

NPC ở location khác: KHÔNG spawn (tiết kiệm CPU/memory). Player chỉ thấy NPC đang ở location của mình.

### InteractableCharacter

Component runtime. Subscribe `PlayerInteraction` raycast:
- E → start `DialogueManager.StartDialogue(characterData.defaultDialogue)`
- Có thể trigger `Cutscene` nếu condition match

### CharacterMovement / CharacterRenderer

Helper component cho động tác walk-to-target trong cutscene (`ActorAction`).

### RelationshipStats

Per-NPC quan hệ player. Tăng khi nói chuyện / tặng quà:
```csharp
class RelationshipStats { string npcId; int points; int level; }
```

Saved in `RelationshipSaveState[]`. Có thể gate dialogue branch.

## Festivals

### FestivalData (ScriptableObject)

```csharp
class FestivalData : ScriptableObject {
    string festivalName;
    GameTimestamp date;       // (year=any, season, day)
    SoCollection<FestivalLocation> locationConfiguration;
    Location[] lockedLocations;
}
class FestivalLocation {
    Location location;
    SoCollection<FestivalNPCBehaviour> npcs;
}
class FestivalNPCBehaviour {
    CharacterData character;
    Vector3 position;
    string animationState;
    SoCollection<CutsceneAction> onInteract;
}
```

→ Mọi list polymorphic dùng `SoCollection<T>` (xem [[technical/datn-architecture]]).

### FestivalManager (singleton, ITimeTracker)

Tick logic:
- Mỗi tick check current `GameTimestamp` vs all `FestivalData.date`
- 06:01 ngày festival: `festivalInProgress = true`, set `lockedLocations` qua `LocationLockManager`
- 06:00 sáng hôm sau (hoặc 24:00): `festivalInProgress = false`, unlock

### FestivalNPCHandler

Khi `onLocationLoad` + festival in progress:
- Override default `NPCManager` spawn
- Spawn NPC theo `FestivalLocation.npcs` config (tất cả NPC kéo về TownSquare)
- Mỗi NPC có `onInteract` cutscene custom (vd "Mua bánh chưng" thay default dialogue)

### TownSquareRenderer

Prefab decoration đặc biệt (cờ, sân khấu, lồng đèn). Active khi festival match scene.

## Tương tác với Cutscene

`FestivalNPCBehaviour.onInteract` là `SoCollection<CutsceneAction>` — nghĩa là mỗi NPC trong festival có cutscene riêng khi interact. Dùng [[systems/datn-dialogue-cutscene|CutsceneManager]] để chạy.

## Cho ĐATN quân đội

NPC schedule rất relevant — chính là cách [[entities/soldier-npc|lính nền tự đi]] có schedule (06:00 tập, 11:30 ăn, 21:30 ngủ). Quyền có thể:
1. Tạo `CharacterData` cho ~5-10 NPC (lính, đại đội trưởng, chính trị viên, đầu bếp)
2. Mỗi NPC có `NPCScheduleData` reflect timetable quân đội
3. Lính tự đi qua [[systems/movement-ai|Phase B PPO model]] thay vì hardcoded `Vector3.MoveTowards` — nhưng integration là Quyền tự lo

Festival → "ngày Quốc khánh / cuộc thi quân sự / lễ tốt nghiệp" — recurring event ngày X làm thay đổi schedule.

## Liên quan

- [[systems/datn-time-weather]] — schedule + festival check theo timestamp
- [[systems/datn-scene-locations]] — onLocationLoad spawn NPC
- [[systems/datn-dialogue-cutscene]] — interact + cutscene
- [[systems/datn-blackboard-save]] — relationship + unlock state
- [[systems/movement-ai]] (Phase B) — có thể wire cho NPC actor

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

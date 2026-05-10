---
title: DATN Game Architecture
category: technical
tags: [datn, architecture, unity, patterns, singletons, scriptable-objects]
sources: [Assets/Scripts/]
created: 2026-05-11
updated: 2026-05-11
---

# DATN Architecture — Cross-cutting patterns

Game DATN dùng kiến trúc Unity-classic với 7-9 singleton manager + ScriptableObject content + `ITimeTracker` observer + UnityEvent broadcast. Đây là page tổng quan; mỗi system có page riêng trong `systems/`.

## Singleton manager (DontDestroyOnLoad)

| Singleton | Vai trò | File |
|---|---|---|
| `TimeManager` | Đồng hồ in-game, broadcast clock tick | `Time/TimeManager.cs` |
| `GameStateManager` | Master state, blackboard, sleep, day reset | `Game State/GameStateManager.cs` |
| `SceneTransitionManager` | Fade + scene load + onLocationLoad event | `Scene Transition/SceneTransitionManager.cs` |
| `InventoryManager` | Tool slot + item slot + hand item | `Inventory/InventoryManager.cs` |
| `LandManager` | Tất cả ô đất + crop reference | `Farming/LandManager.cs` |
| `NPCManager` | Spawn/despawn NPC theo location | `Characters/NPCManager.cs` |
| `WeatherManager` | Today/tomorrow weather, season | `Weather/WeatherManager.cs` |
| `FestivalManager` | Event lịch theo ngày | `Festivals/FestivalManager.cs` |
| `UIManager` | Fade UI, modal prompt, panel | `UI/UIManager.cs` |
| `DialogueManager` | Queue dialogue line + display | `Dialogue/DialogueManager.cs` |
| `CutsceneManager` | Load cutscene, run actions | `Cutscene/CutsceneManager.cs` |
| `LightManager` | Bật/tắt light theo giờ | `Light/LightManager.cs` |
| `AnimalSpawnManager` | Spawn animal theo location | `Animals/AnimalSpawnManager.cs` |
| `IncubationManager` | Trứng ấp daily | `Animals/IncubationManager.cs` |
| `LLMNetworkManager` | Gọi LLM API (experimental) | `AI/LLMNetworkManager.cs` |
| `LocationLockManager` | Khoá location khi festival | `Scene Transition/LocationLockManager.cs` |

→ ~16 singleton. God-object cluster nằm ở `GameStateManager` + `GameBlackboard`.

## ITimeTracker observer

Pattern trung tâm của game. `TimeManager` gọi `ClockUpdate(GameTimestamp)` cho mọi listener đăng ký. Listener:

```csharp
public interface ITimeTracker {
    void ClockUpdate(GameTimestamp timestamp);
}
```

Implement bởi: `GameStateManager`, `WeatherManager`, `FestivalManager`, `LightManager`, `NPCManager`, `IncubationManager`, `CropBehaviour` (qua LandManager). Mỗi tick:
1. `TimeManager` advance phút
2. Phát sinh `ClockUpdate` event
3. Listener tự decide có react không (filter by hour/minute/day)

> [!tip] Coupling thấp
> System mới chỉ cần implement `ITimeTracker.ClockUpdate` + đăng ký với TimeManager. Không cần biết các listener khác. Đây là pattern Quyền nên giữ nếu thêm system AI (vd lính tự đi theo schedule).

## UnityEvent broadcast (loose coupling)

Cross-cutting events:

| Event | Publisher | Subscriber tiêu biểu |
|---|---|---|
| `onLocationLoad` | `SceneTransitionManager` | NPCManager, AnimalSpawn, FestivalManager, UI |
| `onIntervalUpdate` (15p tick) | `GameStateManager` | crop save, animal mood |
| `onDialogueEnd` (callback) | `DialogueManager` | Cutscene action, Shop open |

## ScriptableObject data-driven

Game content KHÔNG hardcode. ScriptableObject types:

- `ItemData` (base) → `EquipmentData`, `SeedData`, `EdibleItemData`, etc.
- `CharacterData` — NPC name, dialogue, location ban đầu, renderer
- `NPCScheduleData` — array `ScheduleEvent` (time → location)
- `AnimalData` — prefab, location to spawn, daysToMature
- `WeatherData` — probability table per season
- `FestivalData` — date, locations, NPC config (dùng `SoCollection<FestivalLocation>`)
- `Cutscene` — array `CutsceneAction` + condition (dùng `SoCollection<CutsceneAction>`)
- `BlackboardEntryData`, `DialogueLine`

→ Quyền tạo content trong Unity Editor (Right-click → Create → DATN/...) chứ không sửa code.

Resources loading: nhiều manager dùng `Resources.LoadAll<T>("path")` để auto-discover SO assets — không cần drag-drop từng file.

## GameBlackboard — god-object

`GameBlackboard` là `Dictionary<string, object>` được serialize qua `BlackboardSerialiserRegistry`. Mọi state transient + history đi qua đây:
- `"ItemsShipped"` — list item đã bán qua ShippingBin
- `"Cutscene_<id>_played"` — cutscene đã chạy chưa
- `"Character_<name>_unlocked"` — NPC đã unlock
- Custom condition cho dialogue/cutscene

→ Tốt cho velocity prototyping, **xấu** cho refactor — magic string khắp code. Nếu Quyền mở rộng nhiều, nên migrate sang typed enum/class.

Chi tiết: [[systems/datn-blackboard-save]].

## Save/Load — binary serialize

`SaveManager` dùng `BinaryFormatter` ghi atomic vào `Application.persistentDataPath/Save.save`. Root object `GameSaveState` aggregates:
- `InventorySaveState`
- `LandSaveState[]` + `CropSaveState[]`
- `PlayerSaveState`
- `WeatherSaveState`
- `RelationshipSaveState[]`
- `AnimalRelationshipState[]`
- `GameBlackboard` (serialised)

> [!warning] BinaryFormatter deprecated
> .NET khuyến cáo bỏ `BinaryFormatter` (security risk + sẽ remove). Quyền nên migrate sang JSON (Newtonsoft hoặc System.Text.Json) trước khi nộp đồ án — examiner có thể hỏi vì code Stardew template hơi cũ.

## Static save state tuple bridge

Vì save chỉ chạy lúc ngủ, các manager dùng **static field** để bridge state qua scene unload mà không lưu file:
- `LandManager.farmData` — `(LandSaveState[], CropSaveState[])`
- `ShippingBin.itemsToShip` — list xuyên scene

Khi reload scene, manager đọc static field và rehydrate. Chỉ khi player ngủ thì binary save mới chạy.

## Animation event hooks

Tool use không drive bởi update loop mà bởi animation event:
- `AnimEventsTools.cs` — gắn lên player animator, mỗi frame impact gọi back C#
- `ParentedAnimationEvent.cs` — bubble event từ child clip lên parent (vd anim cây bị chặt)

Stamina trừ lúc bắt đầu animation, không phải khi xong.

## SoCollection<T>

Generic ScriptableObject collection từ `NullTale/SoCollection` (GitHub package). Cho phép field `public SoCollection<CutsceneAction> action;` mà Unity vẫn serialise đúng, không cần wrapper class.

Dùng ở:
- `Cutscene.cs` — list action
- `FestivalNPCBehaviour.cs` — list `CutsceneAction onInteract`
- `FestivalLocation.cs` — list `FestivalNPCBehaviour npcs`
- `FestivalData.cs` — list `FestivalLocation locationConfiguration`

## Risk / debt notes

- **BinaryFormatter** sắp deprecate (đã nói trên)
- **Magic string** trong GameBlackboard — khó refactor
- **Hardcoded LLM API key** trong `LLMNetworkManager.cs` — security
- **Singleton hell**: 16 singleton, khó test unit
- **DontDestroyOnLoad chain**: nếu init order sai khi load Title scene, có thể null-ref

> [!info] Scope ĐATN
> Quyền không cần fix hết debt — đề tài 5 tháng, demo 7 ngày là đủ. Tài liệu này để biết, không phải checklist must-fix.

---
## Backlinks
- [[sources/datn-game-repo]]
- [[index]]
- [[overview]]

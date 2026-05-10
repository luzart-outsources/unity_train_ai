---
title: Blackboard & Save (DATN)
category: systems
tags: [datn, gamestate, blackboard, save, binaryformatter, persistence]
sources: [Assets/Scripts/Game State/, Assets/Scripts/Save/]
created: 2026-05-11
updated: 2026-05-11
---

# Blackboard & Save — DATN

`GameStateManager` là master singleton kiểm soát day cycle, sleep, và `GameBlackboard` — key-value store god-object cho mọi state transient. `SaveManager` flush atomic xuống file binary lúc Sleep.

## GameStateManager (singleton, ITimeTracker)

Field chính:
- `GameBlackboard blackboard` — saved
- `GameBlackboard sceneItemsBoard` — runtime only, không save
- `UnityEvent onIntervalUpdate` — fire mỗi 15 phút
- Animator + Light state cho ngày/đêm

Flow tick:
1. Mỗi tick từ TimeManager, check minute % 15 == 0
2. Fire `onIntervalUpdate` (subscribe: crop save, animal mood, NPC state save)
3. Nếu hour == 18 (`hourToShip`) → trigger ShippingBin tally
4. Nếu hour == 06:01 → check rain → `RainOnLand()` water tất cả Land
5. Nếu hour ≥ 02 (quá khuya) → auto-sleep với health/stamina debuff

`Sleep()`:
1. Save game qua `SaveManager.Save(GameSaveState)`
2. Skip time tới 06:00 ngày tiếp theo
3. Reset stamina, health partial regen
4. New day → animal age++, weather rotate, NPC re-schedule
5. Fade UI day-summary screen

`PersistentInstantiate(prefab, location, position)`:
Spawn item vào `sceneItemsBoard` để khi reload scene vẫn thấy (vd trứng gà rớt, item NPC đặt). Lưu trong scene-bound dict, không file save.

## GameBlackboard

Dictionary key-value:
```csharp
class GameBlackboard {
    Dictionary<string, object> data;
    void SetValue<T>(string key, T value);
    T GetValue<T>(string key);
    List<T> GetOrInitList<T>(string key);
    bool HasValue(string key);
}
```

Sample key:
- `"ItemsShipped"` — `List<string>` item id
- `"Cutscene_meet_captain_played"` — bool
- `"Character_baker_unlocked"` — bool
- `"FirstDay"` — bool
- `"LocationLock_Forest"` — bool

→ Magic string khắp code. Không type-safe nhưng cực linh hoạt.

### Serialize

Dict `<string, object>` không serialise mặc định bằng BinaryFormatter cho mọi T. DATN có `BlackboardSerialiserRegistry`:
```csharp
interface IBlackboardSerialiser { object Read(BinaryReader); void Write(BinaryWriter, object); }
class BoolSerialiser : IBlackboardSerialiser { ... }
class StringSerialiser : IBlackboardSerialiser { ... }
class StringListSerialiser : IBlackboardSerialiser { ... }
```

Mỗi key được biết type qua `BlackboardEntryData` SO. Nếu thêm type mới (vd `int`), Quyền phải:
1. Tạo `IntSerialiser : IBlackboardSerialiser`
2. Đăng ký vào `BlackboardSerialiserRegistry.Init()`
3. Tạo `BlackboardEntryData` SO với type=Int

### BlackboardCondition

Wrapper cho gate logic:
```csharp
class BlackboardCondition {
    string key;
    Operator op;       // ==, !=, >, <, contains
    object value;
}
```

Eval: `condition.Eval(blackboard)` → bool. Dùng bởi:
- `Cutscene.conditions[]` — gate cutscene
- `DialogueLine.conditions[]` — branching
- `FestivalData` — ẩn

→ `using static BlackboardCondition` ở nhiều file, expose Operator enum.

## SaveManager (static)

Methods:
```csharp
static void Save(GameSaveState state);
static GameSaveState Load();
static bool HasSave();
```

Implementation:
```csharp
BinaryFormatter bf = new();
FileStream fs = File.Create(Application.persistentDataPath + "/Save.save");
bf.Serialize(fs, state);
fs.Close();
```

Path: `%AppData%/LocalLow/<Company>/<Product>/Save.save` trên Windows.

## GameSaveState (root)

```csharp
[Serializable]
class GameSaveState {
    InventorySaveState inventory;
    LandSaveState[] lands;
    CropSaveState[] crops;
    PlayerSaveState player;
    WeatherSaveState weather;
    RelationshipSaveState[] relationships;
    AnimalRelationshipState[] animals;
    byte[] blackboardSerialised;     // qua registry
    GameTimestamp currentTime;
    Location currentLocation;
}
```

## Static intermediate state (cross-scene bridge)

Một số manager dùng static field để hold state ngoài save file, bridge giữa các lần load scene:
- `LandManager.farmData` — `(LandSaveState[], CropSaveState[])` của Farm
- `ShippingBin.itemsToShip` — list item drop vào bin
- `PlayerStats.money/stamina/health`
- `AnimalStats.animalRelationships`

Lúc Sleep, các static này được sync vào `GameSaveState` rồi flush xuống file.

## Risk: BinaryFormatter

> [!warning] .NET 9+ remove BinaryFormatter
> Microsoft đã mark deprecated, sẽ remove. Quyền nên migrate sớm:
> - Option A: `Newtonsoft.Json` (JSON, dễ debug, có Unity package)
> - Option B: `System.Text.Json` (built-in)
> - Option C: Custom binary writer (như BlackboardSerialiserRegistry mở rộng)
>
> Nếu giữ BinaryFormatter cho ĐATN demo OK, nhưng examiner có thể hỏi.

## Cho ĐATN quân đội

Blackboard rất hợp:
- `"DayNumber"` — ngày thứ N của 7-day demo
- `"DisciplinePoints"`, `"FitnessPoints"`, `"KnowledgePoints"` — 3 stat Adaptive
- `"MissionCompleted_<id>"` — track nhiệm vụ
- `"EveningChoice_Day3"` — Player Choice buổi tối
- `"FinalEnding"` — Pass/Fail tính cuối

Save system chỉ cần serialize 3 stat + ngày + history → đơn giản. Quyền có thể bỏ farming/animal save state, chỉ giữ Player + Blackboard.

## Liên quan

- [[systems/datn-time-weather]] — interval update + day reset
- [[systems/datn-farming]] — LandSaveState + CropSaveState
- [[systems/datn-inventory-tools]] — InventorySaveState
- [[systems/datn-animals-economy]] — AnimalRelationshipState
- [[systems/datn-dialogue-cutscene]] — BlackboardCondition gate cutscene
- [[technical/datn-architecture]] — overall pattern

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

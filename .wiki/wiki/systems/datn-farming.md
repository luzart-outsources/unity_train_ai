---
title: Farming (DATN)
category: systems
tags: [datn, farming, crops, land, lifecycle]
sources: [Assets/Scripts/Farming/]
created: 2026-05-11
updated: 2026-05-11
---

# Farming — DATN

Cốt gameplay loop của Stardew template: cày đất → gieo hạt → tưới → chờ → thu. Sau khi import vào game ĐATN của Quyền, system này có thể không relevant (theme quân đội), nhưng code có sẵn nếu Quyền giữ ý tưởng "trồng rau ở doanh trại".

## Trạng thái crop

Enum `CropState`:
```
Seed → Seedling → Harvestable → Wilted (nếu hạn ≥ 48h)
```

Track qua:
- `growth` (in-game phút tích lũy khi được tưới)
- `maxGrowth` (yêu cầu để Harvestable, lấy từ `SeedData.daysToGrow * 24 * 60`)
- `health` (giảm khi không tưới; chết nếu < 0)

## CropBehaviour

Component gắn lên prefab cây. Tham chiếu `Land` parent (ô đất). State tick:
- ITimeTracker: mỗi tick check
  - Đã tưới? `growth++`
  - Hết hạn 48h? Wilted
  - `growth >= maxGrowth`? Switch sang Harvestable mesh
- `Harvest()`: drop item theo `SeedData.cropToYield`, destroy crop. Nếu regrowable thì gắn `RegrowableHarvestBehaviour` thay vì destroy.

## RegrowableHarvestBehaviour

Cho cây đa-hái (vd dâu). Sau hái, không destroy, set `growth = regrowTime`, đợi tiếp.

## LandManager (singleton)

- Maintain list `Land` từ scene Farm.
- Save/load: `LandSaveState[]` + `CropSaveState[]` tuple. Bridge qua scene reload bằng static field.
- Khi load lại scene Farm, recreate Land + Crop từ save.
- Phối hợp `ObstacleGenerator` lúc new game.

## Land

GameObject ô đất 1×1. State enum `LandStatus`:
- `Soil` — chưa cày
- `Farmland` — cày rồi (Hoe tool)
- `Watered` — tưới rồi (WateringCan)

Material thay theo state. Khi player click ô đất với tool đúng → switch state.

## Tool tương tác

Phụ thuộc [[systems/datn-inventory-tools|EquipmentData ToolType]]:
- `Hoe`: Soil → Farmland
- `WateringCan`: Farmland → Watered (cũng water Crop nếu có)
- `Axe`: chặt cây dại (không trong farming core, ở `Tools/`)

`AnimEventsTools.cs` fire callback ở frame impact của animation tool.

## ObstacleGenerator

Spawn rock/log trên Farm khi new game. Player phải clear bằng tool đúng (cuốc đá, rìu cây).

## Save state

```csharp
class LandSaveState { Vector3 position; LandStatus status; }
class CropSaveState { Vector3 position; CropState state; int growth; int health; SeedData seedData; }
```

Bridge static `LandManager.farmData = (lands[], crops[])` lúc unload scene.

## Tác động Weather

[[systems/datn-time-weather|Rain]] auto-water tất cả Land trên scene Farm — không cần player tưới hôm đó.

## Cho ĐATN quân đội

Nếu Quyền giữ farming, có thể reskin "trồng rau cải thiện bữa ăn doanh trại" — cộng stat **Thể lực** thay vì tiền. Hoặc cắt hoàn toàn để focus 7-day loop. Code có thể được giữ làm reference cho việc "interactable object có state lifecycle".

## Liên quan

- [[systems/datn-time-weather]] — growth tick + rain auto-water
- [[systems/datn-inventory-tools]] — Hoe/WateringCan tool + SeedData
- [[systems/datn-blackboard-save]] — persistence

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

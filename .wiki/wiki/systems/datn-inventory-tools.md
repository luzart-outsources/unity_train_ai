---
title: Inventory & Tools (DATN)
category: systems
tags: [datn, inventory, tools, scriptableobject, equipment]
sources: [Assets/Scripts/Inventory/, Assets/Scripts/Tools/]
created: 2026-05-11
updated: 2026-05-11
---

# Inventory & Tools — DATN

8 slot tool + 8 slot item (thường), mỗi slot có quantity. Tool trang bị 1 trong 8, hand-rendered theo `EquipmentData`. Toàn bộ item là `ItemData` ScriptableObject — designer tạo qua Editor menu.

## InventoryManager (singleton)

Field chính:
```csharp
ItemSlotData[] toolSlots;    // 8 slot tool
ItemSlotData[] itemSlots;    // 8 slot item
ItemSlotData equippedTool;   // tool đang ở tay
ItemSlotData equippedItem;
Transform handPoint;          // anchor render model trên tay
```

Method tiêu biểu:
- `EquipHandSlot(int idx)` — swap slot ở tay
- `ConsumeItem(ItemData)` — trừ 1 quantity (vd ăn)
- `ShopToInventory(ItemData, qty)` — Shop gọi
- `GetItemFromString(string id)` — lookup từ Resources cache

## ItemSlotData

```csharp
class ItemSlotData {
    ItemData itemData;
    int quantity;
}
```

Wrapper slot. Designer không tạo trực tiếp; runtime-only.

## ItemData (ScriptableObject hierarchy)

Base:
```csharp
class ItemData : ScriptableObject {
    string id;
    string itemName;
    Sprite thumbnail;
    GameObject gameModel;
    int cost;
    [TextArea] string description;
}
```

Subclass:
- **`EquipmentData`** — thêm `ToolType toolType` (Hoe, WateringCan, Axe, FishingRod, ...). Là tool trang bị được.
- **`SeedData`** — thêm `int daysToGrow`, `ItemData cropToYield`, `bool regrowable`. Dùng bởi [[systems/datn-farming|CropBehaviour]].
- **`EdibleItemData`** — thêm `int healthRestore`, `int staminaRestore`.

→ `using static EquipmentData` xuất hiện ở nhiều file khác để xài ToolType enum như namespace.

## Resources auto-load

Mọi `ItemData` để trong `Assets/Resources/Items/` để lookup runtime:
```csharp
ItemData[] all = Resources.LoadAll<ItemData>("Items");
itemDictionary = all.ToDictionary(x => x.id);
```
→ `GetItemFromString("hoe")` không cần drag-drop reference.

## Tool use flow

Player input (E hoặc click) → [[systems/datn-scene-locations|PlayerToolController]]:
1. Check `equippedTool` có phải `EquipmentData` không
2. Lock player movement
3. Trigger animation theo `ToolType`
4. `AnimEventsTools.cs` (gắn animator) fire `OnToolImpact()` ở frame X
5. Impact callback:
   - Hoe → `LandManager` switch `Soil → Farmland` ô raycast
   - WateringCan → `Land.Water()`
   - Axe → chặt object Tag `"Tree"`
6. `PlayerStats.UseStamina(amount)` trừ stamina
7. Animation xong → unlock player

## Hand item rendering

`EquipmentData.gameModel` (prefab tool) được instantiate vào `handPoint` mỗi lần `EquipHandSlot`. Cũ destroy, mới spawn.

## Save state

```csharp
class InventorySaveState {
    string[] toolSlotIds; int[] toolQuantities;
    string[] itemSlotIds; int[] itemQuantities;
    string equippedToolId; string equippedItemId;
}
```
Lookup qua `GetItemFromString` lúc load.

## Cho ĐATN quân đội

Có thể reskin:
- **ItemData → "đồ quân nhu"** (mũ, balo, súng tập)
- **ToolType → "quân cụ"** (cuốc tăng gia, súng AK demo)
- **SeedData → "rau tăng gia"** (vẫn farming nếu giữ)
- 8+8 slot quá nhiều cho game 7 ngày → có thể giảm còn 4+4

System inventory rất hợp tái dùng. Quyền chỉ cần tạo SO mới, không sửa code.

## Liên quan

- [[systems/datn-farming]] — SeedData drive crop
- [[systems/datn-animals-economy]] — Shop dùng ItemData.cost
- [[systems/datn-blackboard-save]] — InventorySaveState persisted

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

---
title: Animals & Economy (DATN)
category: systems
tags: [datn, animals, chicken, shop, economy, money]
sources: [Assets/Scripts/Animals/, Assets/Scripts/Buying and Selling/]
created: 2026-05-11
updated: 2026-05-11
---

# Animals & Economy — DATN

Hai system gộp vì tightly coupled: animal sản xuất item (trứng) → ShippingBin → tiền → Shop mua hạt giống/đồ khác. Vòng kinh tế cơ bản farming sim.

## Animals

### AnimalData (ScriptableObject)

```csharp
class AnimalData : ScriptableObject {
    string animalName;
    GameObject animalPrefab;
    Location locationToSpawn;     // ChickenCoop
    int daysToMature;              // 7 ngày → adult
    ItemData produceItem;          // trứng
    int produceMoodThreshold;      // mood ≥ 30 mới đẻ
}
```

### AnimalSpawnManager (singleton)

`onLocationLoad`:
1. Filter `AnimalData` có `locationToSpawn == currentLocation`
2. Foreach saved animal → spawn prefab tại random position trong floor collider
3. Hydrate `AnimalRelationshipState` (mood, age, lastProduceDay)

### AnimalBehaviour (base)

Component runtime. Mỗi sáng (qua GameStateManager):
- Tăng age++
- Check `lastProduceDay != today` → có thể produce hôm nay

### ChickenBehaviour : AnimalBehaviour

Override produce logic:
1. age >= `daysToMature` (adult)
2. mood > 30
3. chưa đẻ hôm nay
4. → drop egg item (`AnimalData.produceItem`) qua `GameStateManager.PersistentInstantiate()` trong ChickenCoop

Player nhặt trứng → vào inventory → bán qua [[#shippingbin]].

### AnimalStats (static)

Global list `animalRelationships`. Method:
- `Feed(Animal)` — mood +N
- `Pet(Animal)` — relationship +1
- `GetAnimal(id)` — lookup

### IncubationManager (singleton, ITimeTracker)

Trứng đặt vào `Incubator` (object trong ChickenCoop). Mỗi ngày tick: incubate days++. Đến `daysToHatch` → hatch baby chick.

### Feedbox

Object trong ChickenCoop. Player đặt thức ăn vào → animal đến ăn → mood + (nếu chưa ăn hôm nay).

## Economy

### PlayerStats (static)

```csharp
static int money;
static int stamina;
static int health;
static void Spend(int amount);
static void Earn(int amount);
static void UseStamina(int amount);
```

→ State global, không attach component. Save qua `PlayerSaveState`.

### Shop

Component gắn vào NPC merchant.

```csharp
class Shop : InteractableObject {
    CharacterData ownerNPC;       // chủ shop, phải có mặt
    ItemData[] itemsForSale;
}
```

Player E → Shop:
1. Check NPC chủ shop có ở gần không (Physics.OverlapSphere)
2. Nếu không → "Chủ shop không có ở đây"
3. Nếu có → trigger dialogue ngắn ("Hôm nay bạn cần gì?")
4. Open ShopUI panel với listing

ShopUI list `itemsForSale`. Click item:
- `PlayerStats.Spend(item.cost)` (fail nếu thiếu tiền)
- `InventoryManager.ShopToInventory(item, 1)`

### ShippingBin

Box ở Farm. Player drop item vào → list `itemsToShip` (static, persist qua scene).

`GameStateManager` lúc 18:00 (`hourToShip`) hoặc lúc Sleep:
1. `TallyItems()` — sum `item.cost` của mọi item trong list
2. `PlayerStats.Earn(total)`
3. Track lịch sử trong `GameBlackboard["ItemsShipped"]` (list<string>)
4. Clear list

## Vòng kinh tế

```
Sáng: gà đẻ trứng → Player nhặt
        ↓
Trưa: bán trứng cho ShippingBin (drop in)
        ↓
18:00 / Sleep: ShippingBin tally → +money
        ↓
Sáng hôm sau: tới Shop → mua hạt giống
        ↓
Gieo hạt → tưới → Harvest → loop
```

## Save state

```csharp
class PlayerSaveState { int money, stamina, health, x, y, z; }
class AnimalRelationshipState { string id; int age, mood, relationship; bool produceToday; int lastProduceDay; }
```

## Cho ĐATN quân đội

Phần Economy có thể hoặc không cần — game ĐATN không nhấn mạnh tiền. Tuỳ Quyền:

- **Bỏ Economy** — phù hợp nếu game chỉ là 7-day military training (focus stat thay vì money). Code vẫn để đó, chỉ không expose UI.
- **Reskin Economy → "Điểm rèn luyện"** — Earn khi hoàn thành nhiệm vụ, Spend ở canteen mua đồ ăn buff. ShippingBin/Shop → "kho lương / quân nhu".

Animals tương tự — chickens không hợp doanh trại. Có thể cắt hoàn toàn hoặc reskin "chăn nuôi tăng gia" nếu muốn.

> [!warning] BinaryFormatter dòng PlayerSaveState
> Quyền nhớ migrate khỏi BinaryFormatter (Microsoft đang remove). Save tiền/stat sang JSON dễ hơn vì PlayerSaveState chỉ có scalar.

## Liên quan

- [[systems/datn-time-weather]] — daily egg + 18:00 shipping
- [[systems/datn-inventory-tools]] — Spend/Earn liên kết item.cost
- [[systems/datn-blackboard-save]] — ItemsShipped lịch sử

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

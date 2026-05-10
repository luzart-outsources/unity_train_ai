---
title: Scene Transition & Locations (DATN)
category: systems
tags: [datn, scene, location, transition, fade]
sources: [Assets/Scripts/Scene Transition/, Assets/Scripts/Player/]
created: 2026-05-11
updated: 2026-05-11
---

# Scene Transition & Locations — DATN

Game multi-scene (10 scene rời nhau), chuyển scene qua trigger collider có ID location đích. Manager fade UI, disable CharacterController, load scene async, place player ở entry point đúng, re-enable.

## 10 location enum

```
Farm, PlayerHome, ChickenCoop, Town, TownSquare, Inn, Forest, YodelRanch, Title, ...
```

Mỗi entry là 1 file `.unity` riêng trong `Assets/Scenes/`.

## SceneTransitionManager (DontDestroyOnLoad singleton)

Field:
- `Location currentLocation`
- `bool indoor` — quan trọng cho NPC behavior
- `UnityEvent onLocationLoad` — broadcast cross-system

Method:
1. `SwitchLocation(Location target, Vector3 entryPos)`
2. Disable `PlayerController.CharacterController` (tránh tracking sai)
3. `UIManager.FadeOutScreen()` coroutine
4. `SceneManager.LoadSceneAsync(target.ToString())`
5. Set player transform = entry point của scene mới
6. Re-enable CharacterController
7. `FadeInScreen()`
8. Fire `onLocationLoad` → mọi subscriber re-init

Subscriber tiêu biểu:
- `NPCManager` — spawn NPC theo schedule + location
- `AnimalSpawnManager` — spawn animal nếu là Farm/ChickenCoop
- `FestivalManager` — render festival décor nếu hôm nay festival
- `LocationLockManager` — kiểm tra location có bị khoá không
- `LandManager` — rehydrate Land + Crop từ static `farmData`

## LocationEntryPoint

Component gắn vào trigger collider ở rìa map. Khi player vào trigger:
- Read field `Location targetLocation` + `Vector3 spawnAtTarget`
- Gọi `SceneTransitionManager.SwitchLocation`

Ngoài-trời tới ngoài-trời (Farm ↔ Forest) chỉ đi qua mép map. Vào nhà (PlayerHome) thường có cửa với entry point indoor.

## LocationManager (helper static)

Lookup metadata location: tên hiển thị, scene file path, indoor flag. Edit qua `LocationManagerEditor.cs` (custom inspector).

## LocationLockManager

Khi festival, một số location bị lock. Khoá lưu trong `GameBlackboard` nên persist qua save:
```csharp
SetLocationLock(Location.Forest, true);   // không cho vào Forest hôm nay
```

`SceneTransitionManager` check trước khi load — nếu lock thì show prompt "Hôm nay đang có lễ hội ở Town Square, không vào Forest được".

## Player flow + interaction

`PlayerController` không thuộc Scene Transition nhưng phụ thuộc:
- `CharacterController` Unity, manual move.
- `PlayerInteraction.cs` raycast forward → `InteractableObject` subclass (Bed, Crop, Shop, ShippingBin, Bed, NPC).
- Tool use ([[systems/datn-inventory-tools|PlayerToolController]]) lock movement trong animation.

DontDestroyOnLoad: `PlayerController` thường không persist (mỗi scene có Player riêng). State đi qua `PlayerStats` (static) — money, stamina, health.

## Indoor vs outdoor

Field `SceneTransitionManager.CurrentlyIndoor()` được dùng bởi:
- `WeatherEffectController` — không render mưa indoor
- `LightManager` — đèn indoor logic khác
- `NPCManager` — NPC indoor không cần schedule animation

## Scene cho ĐATN quân đội

Quyền có thể giữ ~3-5 scene: `Doanh_Trai`, `San_Tap`, `Lop_Hoc`, `Nha_An`, `Title`. Đỡ hơn 10 scene farming. Scene Transition system rất nhẹ + reuse được nguyên.

## Liên quan

- [[systems/datn-time-weather]] — TimeManager pause hay không khi load scene là quyết định riêng
- [[systems/datn-npc-festivals]] — onLocationLoad là event chính NPC re-spawn
- [[systems/datn-blackboard-save]] — LocationLock persist qua blackboard

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

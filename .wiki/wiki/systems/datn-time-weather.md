---
title: Time & Weather (DATN)
category: systems
tags: [datn, time, weather, season, observer]
sources: [Assets/Scripts/Time/, Assets/Scripts/Weather/]
created: 2026-05-11
updated: 2026-05-11
---

# Time & Weather — DATN

Đồng hồ in-game chạy phút-by-phút, broadcast cho mọi system qua `ITimeTracker`. Weather phụ thuộc Time (mỗi 6:01 AM tính lại weather hôm sau, mỗi rain auto-water cây).

## Time

### TimeManager (singleton)

- Coroutine `Tick()` chạy mỗi `1f / timeScale` giây → advance 1 in-game phút.
- `GameTimestamp` struct: `(year, season, day, hour, minute)` — 4 mùa × 30 ngày × 24 giờ.
- `DayPhase` enum: Morning (05:00–11:30), Noon, Afternoon, Evening, Night (21:00–05:00).
- Có flag `TimeTicking` để pause khi dialogue/cutscene/menu mở.
- Debug shortcut: `Shift + ]` trong [[systems/datn-scene-locations|PlayerController]] skip nguyên ngày.

### ITimeTracker

Listener đăng ký `RegisterTracker(ITimeTracker)`. Mỗi tick `ClockUpdate(GameTimestamp)` gọi tất cả. Listener filter điều kiện trong callback.

Listener tiêu biểu (xem [[technical/datn-architecture]]):
- `GameStateManager` → trigger `onIntervalUpdate` mỗi 15 phút
- `WeatherManager` → roll weather lúc 06:01
- `LightManager` → toggle light tag "Lights"
- `NPCManager` → check schedule
- `FestivalManager` → check festival start
- `IncubationManager` → trứng tick mỗi sáng

## Weather

### WeatherManager (singleton)

State 2 trường:
- `WeatherToday`
- `WeatherTomorrow`

Lúc 06:01 mỗi ngày: `WeatherToday = WeatherTomorrow`, roll lại `WeatherTomorrow` từ `WeatherData` của season hiện tại. Logic này cho phép UI hiện forecast "ngày mai".

### WeatherData (ScriptableObject)

Probability table per season:
```csharp
WeatherProbability[] springWeather;   // [{Sunny,50},{Rainy,30},{Cloudy,20}]
WeatherProbability[] summerWeather;
WeatherProbability[] fallWeather;
WeatherProbability[] winterWeather;
```

`WeatherProbability = (WeatherType, weight)`. Weighted random pick.

### Hiệu ứng game

| WeatherType | Tác động |
|---|---|
| Rainy | `GameStateManager.RainOnLand()` lúc 06:01 → tự water tất cả Land không cần player |
| Snowy | (winter only) chặn farming? — cần check code thêm |
| Sunny/Cloudy | không tác động cơ học |
| `WeatherEffectController` | Render particle (mưa/tuyết) trên scene |

> [!info] Rain auto-water = cơ chế ưu ái farming
> Nếu mưa, player không cần bưng bình tưới — system tự gọi `Land.Water()` cho mọi ô. Đây là why mưa lại "tốt cho cây" trong Stardew template.

## Save/Load

`WeatherSaveState`: lưu today, tomorrow, season. Re-roll khi load nếu thiếu data.

## Suy nghĩ scope ĐATN của Quyền

Game ĐATN của Quyền (GDD) là *học kỳ quân đội* nên Time/Weather có thể cần:
- 7 ngày demo (không 4 mùa × 30 ngày như Stardew template)
- Schedule quân đội (06:00 thể dục, 11:30 ăn trưa, 21:30 ngủ) — map vào `DayPhase` đã sẵn
- Weather có thể chỉ Sunny/Rainy, không cần Snow

→ **Có thể giữ nguyên Time + đơn giản hoá Weather**. ITimeTracker là backbone giúp dễ thêm system AI lính tự di chuyển theo schedule (Phase B) — chỉ cần `Soldier.cs` implement `ITimeTracker.ClockUpdate` rồi check hour.

## Liên quan

- [[systems/datn-farming]] — crop growth pinned vào timestamp
- [[systems/datn-npc-festivals]] — NPC schedule + festival event ăn theo time
- [[systems/datn-animals-economy]] — egg laying check daily
- [[systems/movement-ai]] (Phase B) — có thể consume time để map schedule lính

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

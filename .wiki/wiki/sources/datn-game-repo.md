---
title: DATN Game Repo (manhquyenkma/DATN)
category: sources
tags: [datn, quyen, farming-sim, stardew-clone, import]
source_path: Assets/Scripts/
created: 2026-05-11
updated: 2026-05-11
---

# DATN Game Repo — Source Summary

Repo GitHub `manhquyenkma/DATN` (1 commit, không README, ~700MB) là code game farming life-sim của Quyền — về cấu trúc gần như Stardew-Valley clone (crops, animals, festivals, NPCs có schedule, weather, save/load, cutscene). Import ngày 2026-05-11 thay thế Assets/Scenes + InputSystem cũ; giữ nguyên `Assets/AI/` (Phase A v1/v2 brain).

## Path

- **GitHub**: https://github.com/manhquyenkma/DATN
- **Imported vào**: `Assets/` (gốc project), 698MB sau khi xoá file URP-only
- **Clone gốc** (nếu cần re-pull): `D:/OutSources/Unity_AI/_DATN_clone/`

## Nội dung repo

| Folder gốc | Sau import | Ghi chú |
|---|---|---|
| `Assets/Scripts/` (50+ subfolder) | `Assets/Scripts/` | Toàn bộ code gameplay, **22 system** lớn |
| `Assets/Imported Asset/` | giữ nguyên | POLYGON Farm + Kenney + Meshtint asset, ~600MB binary |
| `Assets/Prefabs/` | giữ nguyên | Prefab game |
| `Assets/Scenes/` (10 scene) | thay scene cũ | Farm, Forest, Inn, PlayerHome, Title, Town, TownSquare, YodelRanch, ChickenCoop |
| `Assets/Settings/` (URP) | **đã xoá** | URP pipeline asset, không dùng |
| `Assets/UniversalRenderPipelineGlobalSettings.asset` | **đã xoá** | URP global, không dùng |
| `Assets/DefaultVolumeProfile.asset` | **đã xoá** | URP volume, không dùng |

## Package thay đổi vào `Packages/manifest.json`

Thêm 1 dòng (DATN có nhưng project user chưa có):
```
"www.nulltale.socollection": "https://github.com/NullTale/SoCollection.git"
```
Cần thiết vì 5 file (Cutscene, Festivals/*) dùng `using SoCollection;` + generic `SoCollection<T>`.

Các package KHÔNG add (DATN dùng nhưng user không cần):
- `com.unity.render-pipelines.universal` — user dùng Built-in
- `com.unity.postprocessing` — kèm URP
- `com.unity.ai.assistant`, `com.coplaydev.unity-mcp` — dev tool, không phải gameplay

## Cảnh báo nội dung

> [!warning] Theme mismatch — code farming, GDD nói quân đội
> Repo này là farming life-sim (crops/chickens/festivals), trong khi đề cương ĐATN viết "mô phỏng học kỳ quân đội". Quyền dường như dùng farming code làm template rồi sẽ reskin về quân sự. Chi tiết: [[contradictions]] mục 2026-05-11.

> [!warning] License POLYGON Farm
> Folder `Assets/Imported Asset/` chứa POLYGON Farm + một số free pack (Kenney, Meshtint). POLYGON Farm là asset Synty trả phí. Repo upload public → có rủi ro vi phạm license Synty EULA. Nếu Quyền release public/commercial cần mua trên Asset Store.

> [!info] Hardcoded API key
> `Assets/Scripts/AI/LLMNetworkManager.cs` có chứa key Groq/FreeLLM hardcoded — security issue cần Quyền move sang env hoặc xoá trước khi public.

## System index

22 system map detail xem [[technical/datn-architecture]]. Page riêng cho từng cluster:

- [[systems/datn-time-weather]] — đồng hồ in-game + thời tiết theo mùa
- [[systems/datn-farming]] — crop lifecycle (gieo → tưới → thu)
- [[systems/datn-inventory-tools]] — slot inventory + tool equipment + ItemData SO
- [[systems/datn-scene-locations]] — multi-scene transition + LocationManager
- [[systems/datn-dialogue-cutscene]] — DialogueManager + Cutscene SO + SoCollection
- [[systems/datn-npc-festivals]] — NPC schedule + Festival event-driven
- [[systems/datn-animals-economy]] — chicken/egg + Shop + ShippingBin + PlayerStats
- [[systems/datn-blackboard-save]] — GameBlackboard god-object + binary save

## Decision liên quan

- [[decisions/import-datn-game-base]] — quyết định clone DATN làm base game

---
## Backlinks
- [[index]]
- [[overview]]
- [[contradictions]]

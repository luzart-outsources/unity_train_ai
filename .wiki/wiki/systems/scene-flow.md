---
title: Scene Flow System
category: systems
tags: [scene, additive, sceneloading]
sources: [raw/gdd/gdd_inGame.txt, docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Scene Flow System

Additive loading on a persistent Bootstrap. Sub-scenes (Lớp/KTX/Nhà ăn) stack on top of World without unloading it.

## Scene list

```
00_Bootstrap        persistent — never unload (ServiceLocator + Sentis + OverlayCanvas)
01_MainMenu         additive on Bootstrap — NewGame / Continue / Exit
02_CutScene         optional video, additive then unload
03_CreateChar       additive, captures playerName
10_World            doanh trại, additive on top of Bootstrap
11_LopHoc           additive on top of World (sub-scene)
12_NhaAn            sub-scene
13_KyTucXa          sub-scene
99_Ending           additive — single ending screen
```

## SceneRefSO

```csharp
public class SceneRefSO : ScriptableObject {
    public string sceneName;
    public LoadMode mode;   // Single or Additive
}
```

## ISceneRouter

```csharp
public interface ISceneRouter {
    UniTask LoadAdditive(SceneRefSO scene, string transitionText = null);
    UniTask UnloadAdditive(SceneRefSO scene);
    UniTask LoadSingle(SceneRefSO scene);
}
```

`transitionText` triggers `UIRouter.ShowLoading(text, 3s)` per GDD line 24-25.

## Sub-scene activation flow

```
Player enters classroom door zone → InteractPressedMsg
  → InteractionRouter executes SceneTransitionInteractionSO
  → await SceneRouter.LoadAdditive(SceneRef_LopHoc, "Đang vào lớp học...")
  → World scene paused: GameClock.Freeze() per GDD line 63
  → Quiz runs inside sub-scene
  → On exit: SceneRouter.UnloadAdditive(SceneRef_LopHoc)
  → GameClock.Resume()
```

## Day rollover scene flow

`SleepQuestSO.Complete()` → `UIRouter.ShowLoading("Sang ngày hôm sau...", 3s)`, no scene change (KTX sub-scene unloads). New day starts immediately.

## Ending

Day 30 last quest done → `SceneRouter.LoadAdditive(SceneRef_Ending, "Đang xếp loại...")` → unload World.

## Backlinks
- [[systems/ui-router]]
- [[systems/quest-system]]

---
title: Automation Tooling
category: technical
tags: [editor, automation, generator, validator]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# Automation Tooling

Editor scripts in `Assets/Editor/Automation/`. Phase 11-12 deliverable per [[decisions/d08-automation-last]].

## Goal

"Anh đi ngủ, dậy bấm 1 nút, mọi thứ tự động hoàn thành" — `Tools → Build Game → Generate All (Master)` runs the full pipeline:

1. Generate 30 days of data SO from `world_template.json`
2. Build prefabs (Player, NPC variants, Quest arrow)
3. Build 6+ scenes (Bootstrap, MainMenu, CutScene, CreateChar, World, sub-scenes, Ending)
4. Wire `ServiceLocatorSO` with all refs
5. Trigger recompile + wait
6. Validate everything (null refs, asmdef, compile errors, scenes in BuildSettings)
7. Open Bootstrap scene; emit `Library/automation_report.md`

## Source of truth

`Assets/Editor/Automation/world_template.json` — single blueprint for whole game (see [[open-questions#q-20260512-01]]):

```jsonc
{
  "days": [
    { "day": 1, "weekday": "Mon", "quests": [
        {"type":"GotoConfirm","area":"SanVanDong","start":"05:00","deadline":"05:15"},
        {"type":"Quiz","area":"LopHoc_Door","start":"07:30","deadline":"11:30","quizSet":"LichSu_d1"},
        ...
    ]}
  ],
  "npcs": [...],
  "areas": [...],
  "subjects": [...]
}
```

Modify JSON → re-run Master → assets regenerate (idempotent).

## Tool catalog

| File | Class | Method | Purpose |
|---|---|---|---|
| `SOGenerator.cs` | `SOGenerator` | `GenerateAll()` | Parse `world_template.json`, create/update Area/Subject/Quiz/Interactable/NPC/Quest/Day SO |
| `StrategyGenerator.cs` | `StrategyGenerator` | `GenerateAll()` | Create Onnx/NavMesh/Canned strategy SO presets |
| `PrefabBuilder.cs` | `PrefabBuilder` | `BuildAll()` | Build Player/NPC/Arrow/HUD prefabs |
| `SceneBuilder.cs` | `SceneBuilder` | `BuildAll()` | Generate Bootstrap, MainMenu, CutScene, CreateChar, World, sub-scenes, Ending scenes; spawn cube placeholders |
| `ServiceWizard.cs` | `ServiceWizard` | `BuildServiceLocator()` | Auto-find DBs + RSOs + ModelAssets, wire into ServiceLocatorSO |
| `Validator.cs` | `Validator` | `ValidateAll()` | Null-check `[Required]` fields in every SO; compile error check; asmdef cycle check; scenes in BuildSettings; ModelAsset non-null; responses.json valid + has 8 intent keys |
| `MasterBuilder.cs` | `MasterBuilder` | `BuildEverything()` | Orchestrate 1→7 in order, log + write `automation_report.md` |

## Menu

```
Tools/
├── Build Game/
│   ├── 1. Generate Data SO          (SOGenerator.GenerateAll)
│   ├── 2. Generate Strategy SO      (StrategyGenerator.GenerateAll)
│   ├── 3. Build Service Locator     (ServiceWizard.BuildServiceLocator)
│   ├── 4. Build Prefabs             (PrefabBuilder.BuildAll)
│   ├── 5. Build Scenes              (SceneBuilder.BuildAll)
│   ├── 6. Validate Everything       (Validator.ValidateAll)
│   └── ⚡ Generate All (Master)       (MasterBuilder.BuildEverything)
└── Debug/
    ├── Reset Save
    ├── Skip to Day X
    └── Force Quest Complete
```

## Idempotency pattern

```csharp
static T CreateOrUpdate<T>(string path) where T : ScriptableObject {
    var x = AssetDatabase.LoadAssetAtPath<T>(path);
    if (x == null) {
        x = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(x, path);
    }
    return x;
}
```

Re-running Master never creates duplicates; only updates fields.

## Compile + validation

```csharp
CompilationPipeline.RequestScriptCompilation();
await UntilAssemblyReloadFinished();
var errors = CompilationPipeline.GetAssemblies()
    .SelectMany(a => CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName(a.name).ChechErrors());
if (errors.Any()) FAIL;
```

(Pseudocode — Unity 6 API method names may differ; `AssemblyReloadEvents.afterAssemblyReload` is the supported hook.)

## Output

`Library/automation_report.md`:
```
# Automation Report — 2026-05-13 02:30
- [✅] Generated 30 Day SO
- [✅] Generated 187 Quest SO
- [✅] Generated 24 QuizSet SO + 240 QuizQuestion SO
- [✅] Generated 15 NPC SO
- [✅] Generated 23 Interactable SO
- [✅] Wired ServiceLocatorSO
- [✅] Built 7 scenes
- [✅] Built 5 prefabs
- [✅] Compile clean
- [✅] No null required refs
- [✅] No asmdef cycles
- [✅] All scenes in BuildSettings
- [✅] responses.json parsed, 8 intents present
- [✅] ModelAsset refs non-null

PASS — ready to enter Play mode in 00_Bootstrap.unity
```

## Backlinks
- [[decisions/d08-automation-last]]
- [[technical/asmdef-structure]]
- [[open-questions#q-20260512-01]]

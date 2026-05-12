---
title: SO Taxonomy
category: technical
tags: [so, rso, db, naming]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

# SO Taxonomy

## Suffix conventions

| Type | Suffix | Mutability | Persisted |
|---|---|---|---|
| Data SO | `SO` | bất biến lúc Play | asset on disk |
| Strategy SO (polymorphic) | `SO` | bất biến lúc Play | asset on disk |
| Runtime state | `RSO` | mutable, reset on `OnEnable` | NOT in asset, persisted via `SaveService` JSON |
| Database (list) | `DB` | bất biến (list of refs) | asset on disk |

## Base abstract classes

(See [[systems/quest-system]] etc. for individual systems.)

- `QuestSO`, `InteractionSO`, `MovementStrategySO`, `DialogueStrategySO`, `ScoreRuleSO`, `EndingRuleSO` — abstract polymorphic.
- `RuntimeSO` — abstract base for RSO; `void OnEnable() => Reset();`.
- Standard data SO inherit `ScriptableObject` directly.

## Database pattern (per [[decisions/d06-no-socollection]])

```csharp
[CreateAssetMenu(menuName="Game/DB/Quest")]
public class QuestDB : ScriptableObject {
    public List<QuestSO> all;
    public QuestSO ById(string id) => all.FirstOrDefault(q => q.id == id);
#if UNITY_EDITOR
    [ContextMenu("Auto-populate from /Assets/_Data/Quests/")]
    void AutoPopulate() {
        all = AssetDatabase
            .FindAssets("t:QuestSO", new[] { "Assets/_Data/Quests" })
            .Select(g => AssetDatabase.LoadAssetAtPath<QuestSO>(AssetDatabase.GUIDToAssetPath(g)))
            .ToList();
        EditorUtility.SetDirty(this);
    }
#endif
}
```

Same pattern for `NPCDB`, `InteractableDB`, `QuizDB`, `AreaDB`, `DayDB`, `SubjectDB`.

## Runtime SO (RSO)

```csharp
public abstract class RuntimeSO : ScriptableObject {
    void OnEnable() => Reset();
    public abstract void Reset();
}
public class PlayerStateRSO : RuntimeSO {
    public string playerName;
    public int hocTap, renLuyen;
    public string currentScene;
    public Vector3 lastWorldPos;
    public override void Reset() {
        playerName = ""; hocTap = 0; renLuyen = 100;
        currentScene = "10_World"; lastWorldPos = Vector3.zero;
    }
}
```

`OnEnable` runs every time Editor enters Play mode → state starts clean. Save/Load explicitly restores fields via JSON DTO (see [[systems/save-load]]).

## Strategy SO instantiation gotcha

`ScriptableObject` has no constructor. Pre-init in `OnEnable` if needed. For per-instance runtime state, create a plain C# class (`IQuestRuntime`, `IMovementAgent`) — see [[systems/quest-system]] § Base.

## Asset folder layout

```
Assets/_Data/
├── Days/           # Day01.asset .. Day30.asset (DaySO)
├── Quests/         # GotoConfirm_TheDuc_d1.asset, Quiz_LichSu_d1.asset, ...
├── NPCs/           # NPC_DaiDoiTruong.asset, NPC_HocSinh_01.asset, ...
├── Interactables/  # Interactable_SanVanDong.asset, ...
├── Quizzes/        # QuizSet_LichSu_1.asset, QuizQuestion_*.asset, ...
├── Strategies/     # MovementOnnx.asset, DialogueOnnx.asset, NavMesh.asset, ...
├── Areas/          # Area_SanVanDong.asset, Area_LopHoc_Door.asset, ...
├── Scenes/         # SceneRef_World.asset, SceneRef_LopHoc.asset, ...
├── Rules/          # LatePenaltyRule.asset, QuizScoreRule.asset, ...
├── Config/         # GameConfig.asset, TimeConfig.asset, PlayerControls.inputactions
└── Runtime/        # PlayerStateRSO.asset, GameClockRSO.asset, ...
```

## Backlinks
- [[technical/architecture]]
- [[decisions/d01-so-first-modular]]
- [[decisions/d06-no-socollection]]
- [[decisions/d10-json-save]]

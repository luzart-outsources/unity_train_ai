# Phase 3 — Data SO + Databases (blueprint)

> Blueprint = task outline + acceptance. Expand to bite-sized in a fresh session when starting this phase, OR follow tasks here as ~15-30 min units.

**Goal:** Complete all Data SO types and DB SO with `Auto-populate` Editor context menu.

**Spec refs:** §3.4-3.5, [[technical/so-taxonomy]], [[decisions/d06-no-socollection]].

---

## Tasks

### 3.1 — Define remaining Data SO types
- [ ] `DaySO` (dayIndex, weekday, `List<QuestSO> quests`) — `Assets/Scripts/SO/Base/DaySO.cs`
- [ ] `NPCSO` (id, displayName, portrait, spawnPos, `MovementStrategySO movement`, `DialogueStrategySO dialogue`, `ScheduleSO schedule`) — `NPCSO.cs`
- [ ] `ScheduleSO` (`List<ScheduleEntry> entries` where `ScheduleEntry{int hour, int minute, AreaSO target}`) + `GetEntryAt(int day, int hour, int minute) -> ScheduleEntry` — `ScheduleSO.cs`
- [ ] `InteractableSO` (area, `InteractionSO onInteract`, optional `Condition gate`) — `InteractableSO.cs`
- [ ] `QuizSetSO`, `QuizQuestionSO`, `SubjectSO` per spec — `QuizSetSO.cs` etc.
- [ ] `ResponseTemplatesSO` wrapping `TextAsset responsesJson` + `Dictionary<IntentId,List<string>> Load()` — `ResponseTemplatesSO.cs`
- [ ] `TimeConfigSO` (`gameHourPerRealMinute=1/3f, idlePenaltyMinutes=15`) — `TimeConfigSO.cs`
- [ ] `GameConfigSO` (`totalDays=30, skipWeekend=true, startRenLuyen=100, maxHocTap=480, BackendType preferredBackend`) — `GameConfigSO.cs`
- [ ] `IntentId` enum: `HOI_LICH, HOI_GIO_AN, HOI_VI_TRI, HOI_KIEN_THUC, BAO_CAO, XIN_PHEP, TAM_BIET, OUT_OF_SCOPE` — `IntentId.cs` in `TrainAI.Core`

### 3.2 — Remaining RSO
- [ ] `GameClockRSO` (day, hour, minute, weekday, frozen)
- [ ] `ActiveQuestRSO` (current QuestSO ref, runtimeInstance plain class ref, deadlineRemainingSec)
- [ ] `DayProgressRSO` (List<string> completedToday, missedToday)

### 3.3 — Database SO with `Auto-populate`
- [ ] `QuestDB`, `NPCDB`, `InteractableDB`, `QuizDB`, `AreaDB`, `DayDB`, `SubjectDB` — each:
  ```csharp
  public class XDB : ScriptableObject {
      public List<XSO> all;
  #if UNITY_EDITOR
      [ContextMenu("Auto-populate from /Assets/_Data/Xs/")]
      void AutoPopulate() {
          all = UnityEditor.AssetDatabase
              .FindAssets("t:XSO", new[] {"Assets/_Data/Xs"})
              .Select(g => UnityEditor.AssetDatabase.LoadAssetAtPath<XSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)))
              .ToList();
          UnityEditor.EditorUtility.SetDirty(this);
      }
  #endif
  }
  ```

### 3.4 — Edit-mode tests
- [ ] `ScheduleSOTests.GetEntryAt_ReturnsLastEntryAtOrBefore(hour, minute)`.
- [ ] `DayDBTests.AutoPopulate_FindsAllAssetsInFolder` (uses test fixture folder).
- [ ] `ResponseTemplatesSOTests.Load_Parses8Intents` against a small sample JSON.

### 3.5 — Sample assets
- [ ] Create `_Data/Config/GameConfig.asset`, `TimeConfig.asset`.
- [ ] Create `_Data/Runtime/GameClockRSO.asset`, `ActiveQuestRSO.asset`, `DayProgressRSO.asset`.
- [ ] Drop `intent_classifier.onnx`, `soldier.onnx`, `responses.json` from `AI_Training/deliverables/` into `Assets/_Models/`. Verify Unity imports `.onnx` as `ModelAsset`.

---

## Acceptance

- [ ] Every SO type from spec §3.4 + §3.5 exists with `CreateAssetMenu`.
- [ ] Every DB SO has working `Auto-populate` (call manually in Editor, see list filled).
- [ ] 3+ new EditMode tests pass.
- [ ] `_Models/` contains 3 model + responses.json assets.
- [ ] ServiceLocatorSO.asset has all data SO + DB SO drag-and-drop slots wired (manual, not auto yet).

Proceed to `phase-04-services.md`.

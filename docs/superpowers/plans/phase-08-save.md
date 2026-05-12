# Phase 8 — Save / Load (blueprint)

**Goal:** JSON DTO save at `Application.persistentDataPath/save_slot{N}.json` with versioned migrator.

**Spec refs:** §8, [[systems/save-load]], [[decisions/d10-json-save]].

---

## Tasks

### 8.1 — `SaveDTO` POCO
- [ ] `Assets/Scripts/Services/Save/SaveDTO.cs` matching JSON schema in spec §8.1.
- [ ] Use `[Serializable]` for `JsonUtility` compatibility (or Newtonsoft if needed for `Dictionary`).

### 8.2 — `SaveService` impl
- [ ] `Save(slot)`: build DTO from `PlayerStateRSO + GameClockRSO + DayProgressRSO + transform.position`. Write JSON.
- [ ] `TryLoad(slot, out dto)`: parse file or return false.
- [ ] `Restore(dto)` static helper: sets RSO fields, returns target scene name to load.

### 8.3 — Auto-save on day end
- [ ] `SaveService.Subscribe<DayEndedMsg>(_ => Save(1))` in constructor.

### 8.4 — Continue flow
- [ ] `UIMainMenu.OnContinueClicked()` → `SaveService.TryLoad(1, out dto)` → restore RSOs → `SceneRouter.LoadAdditive(SceneRef_World, "Đang khôi phục...")` → set player position.

### 8.5 — Versioning
- [ ] `SaveMigrator.Migrate(int from, int to, JObject)` chain. Initially trivial (version 1).
- [ ] Add unit test for migrator framework.

### 8.6 — Tests
- [ ] EditMode round-trip test: build sample DTO → write to temp path → read back → assert fields match.

---

## Acceptance

- [ ] Round-trip JSON save/load test PASS.
- [ ] Day rollover auto-saves to slot 1.
- [ ] Continue from MainMenu restores state correctly (manual PlayMode test).

Proceed to `phase-09-scenes.md`.

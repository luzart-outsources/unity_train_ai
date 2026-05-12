# Phase 9 — Scene 10_World + sub-scenes (manual prototype) (blueprint)

**Goal:** Manually build 1 working version of every scene per spec §6.1, with cube placeholders, area triggers, and 1 NPC each. Sufficient to play Day 1 end-to-end.

**Spec refs:** §6, §11, [[systems/scene-flow]], [[technical/placeholder-scale]].

---

## Tasks

### 9.1 — `00_Bootstrap.unity`
- [ ] Empty scene + `BootstrapEntry` MonoBehaviour with `[SerializeField] ServiceLocatorSO services` → `Awake` calls `services.Bootstrap()`.
- [ ] `OverlayCanvas` (UIRouter parent).
- [ ] `GameLoopDriver` MonoBehaviour: `Update` calls `services.Clock.Tick`, `Quests.Tick`, `Movement.Tick`.
- [ ] `DontDestroyOnLoad` on all of the above.
- [ ] On `Awake`, after Bootstrap, additive-load `01_MainMenu`.

### 9.2 — `01_MainMenu.unity`
- [ ] Background quad with placeholder texture.
- [ ] Spawn `UIMainMenu.prefab` on the OverlayCanvas (added via UIRouter or directly).

### 9.3 — `02_CutScene.unity`
- [ ] VideoPlayer component (optional placeholder file or skip). Play → wait → load `03_CreateChar`.

### 9.4 — `03_CreateChar.unity`
- [ ] Background + `UICreateChar.prefab` → on confirm load `10_World`.

### 9.5 — `10_World.unity`
- [ ] Ground plane (50 × 50).
- [ ] Buildings: cube `Lop_Hoc` (12 × 5 × 8) at `(5, 0, -8)`, `KTX` (15 × 4 × 10) at `(-5, 0, -8)`, `Nha_An` (14 × 4.5 × 9) at `(10, 0, -8)`.
- [ ] Area floors with trigger collider: `SanVanDong` (25 × 0.1 × 15 plane at (-10, 0, 0)) tagged "Quest Area", same for `DonVeSinh`.
- [ ] Door cubes (1.5 × 2.5 × 0.5) inset at each building face, each with `InteractableMarker` + `SphereCollider isTrigger r=2`.
- [ ] Player spawn at `(0, 0, 0)`.
- [ ] NPC_DaiDoiTruong spawn at `(0, 0, 5)`, dragged `NPC_DaiDoiTruong.asset` reference on `NpcView`.
- [ ] NPC_HocSinh_01 spawn at `(-3, 0, 2)` with `NPC_HocSinh_01.asset`.

### 9.6 — `11_LopHoc.unity`
- [ ] Cube building interior. 4 desks (cubes 1.2 × 0.8 × 0.6) with trigger + `InteractableMarker` → `Interact_LopHoc_Desk_Quiz`.
- [ ] Bục giảng cube. Bảng đen quad.

### 9.7 — `12_NhaAn.unity`
- [ ] Cube hall with bàn ăn cubes + trigger → `Interact_NhaAn_Confirm`.
- [ ] 1 NPC static decoration.

### 9.8 — `13_KyTucXa.unity`
- [ ] 4 cube bunk beds (0.9 × 0.4 × 2.0) → trigger → `Interact_KTX_Sleep` (SleepQuestSO trigger).
- [ ] 1 NPC mobile (idle wander inside KTX) + 1 NPC static.

### 9.9 — `99_Ending.unity`
- [ ] Spawn `UIEnding.prefab` directly. Read from `PlayerStateRSO` + `EndingRuleSO.Evaluate`.

### 9.10 — Build Settings
- [ ] Add all 7 scenes to `File → Build Settings`. Index 0 = `00_Bootstrap`.

---

## Acceptance

- [ ] Open `00_Bootstrap.unity`, press Play → MainMenu shows → NewGame → CreateChar → World loads.
- [ ] Walk to SanVanDong → quest arrow points there → press E → Confirm popup → time skips → next quest activates.
- [ ] Walk to LopHoc door → press E → sub-scene loads → quiz UI shows.
- [ ] Walk to KTX → bed → confirm → UILoading 3s → day 2 starts.
- [ ] All scenes in Build Settings.

Proceed to `phase-10-vertical-slice.md`.

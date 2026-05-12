# Phase 10 — Vertical slice (1 day end-to-end) (blueprint)

**Goal:** Author 1 complete day of content (Day 1) using the systems built in Phase 0-9. Play it from MainMenu to "Sang ngày hôm sau" without crash or visible bug.

This is the critical milestone gate before automation (Phase 11-12).

---

## Tasks

### 10.1 — Author Day 1 assets (manual SO creation, no generator yet)
- [ ] `Day01.asset` (DaySO, dayIndex=1, weekday=Monday).
- [ ] 8 QuestSO for Day 1 timeline (5:00 TheDuc, 6:00 DonVS, 7:00 AnSang, 7:30 LichSu_d1 Quiz, 11:30 FreeRoam, 14:00 GDQP_d1 Quiz, 17:00 FreeRoam, 18:30 Sleep).
- [ ] 2 QuizSetSO (LichSu_d1, GDQP_d1) with 10 placeholder questions each.
- [ ] Drag QuestSO list into `Day01.quests`.
- [ ] `DayDB.AutoPopulate()`.

### 10.2 — Author NPC schedule for HocSinh_01
- [ ] `Schedule_HocSinh_01.asset` matching Day 1 timeline.
- [ ] Drag into `NPC_HocSinh_01.schedule`.

### 10.3 — Wire ServiceLocatorSO with all DB + RSO + ModelAsset + responses.json refs
- [ ] Open `ServiceLocator.asset` Inspector. Manually drag each field.
- [ ] Verify `Bootstrap()` constructs without exception.

### 10.4 — Play test Day 1
- [ ] Press Play in `00_Bootstrap`.
- [ ] Walk through entire Day 1 timeline:
  - 5:00 TheDuc → confirm → success
  - 6:00 DonVS → confirm
  - 7:00 NhaAn → enter → confirm → exit
  - 7:30 LopHoc → enter → quiz (random correct/wrong) → exit
  - 11:30 free roam
  - 14:00 LopHoc → quiz
  - 17:00 free roam → chat NPC_DaiDoiTruong, verify intent classifier responds
  - 18:30 KTX → sleep → next day
- [ ] Verify `Day02` starts (if Day 02 not authored, expect graceful error or no quests).

### 10.5 — Bug fix pass
- [ ] Address every issue found in 10.4. Common: null ref in NpcContext, missing scene in build settings, tensor disposal warning, missing material on placeholder, NPC stuck in obstacle.

### 10.6 — Documentation
- [ ] Update `.wiki/wiki/log.md` with the slice playthrough result and any new bugs filed in `.wiki/wiki/bugs/`.

---

## Acceptance

- [ ] Day 1 playable start-to-finish, no console errors.
- [ ] All 8 quests activate by time and complete on interaction.
- [ ] NPC chat returns coherent Vietnamese reply.
- [ ] NPC student moves between areas during the day.
- [ ] Score updates correctly on miss/quiz.
- [ ] Save written on day end.
- [ ] No tensor leak warnings.

Proceed to `phase-11-automation.md`.

# Phase 12 — Master Generate All → 30-day content (blueprint)

**Goal:** Populate `world_template.json` with 30 full days, 12+ NPCs with realistic schedules, complete quiz content per subject, run Master, validate.

---

## Tasks

### 12.1 — Author `world_template.json` for 30 days
- [ ] Use spec timeline template (5:00, 6:00, 7:00, 7:30, 11:30, 14:00, 17:00, 18:30) for each day. Skip-weekend logic handled at runtime (DayDB doesn't include weekends — only weekdays).
- [ ] 24 study sessions total (12 Lịch sử + 12 GDQP); assign to morning/afternoon slots.
- [ ] Author optionally via LLM prompt (see [[open-questions#q-20260512-01]]) or hand-write.

### 12.2 — Author 24 QuizSets × 10 questions
- [ ] Real content for Lịch sử + GD Quốc phòng (or placeholder dummy if real not available — fall back to "Câu hỏi 1, 2, 3..." with answer index 0 always correct for dev).
- [ ] Each `QuizSet_<Subject>_<Lesson>.asset` referenced from corresponding `QuizQuestSO`.

### 12.3 — Author NPC schedules
- [ ] 10+ HocSinh NPCs with distinct color/name. Schedule mirrors player timeline.
- [ ] 2-3 static decoration NPCs in KTX, Nhà ăn (no schedule, no AI).

### 12.4 — Run `MasterBuilder`
- [ ] `Tools/Build Game/⚡ Generate All (Master)`. Expect: 30 DaySO, 240 QuizQuestionSO, 24 QuizSetSO, 10-15 NPCSO, 180+ QuestSO, 20+ AreaSO, 6+ InteractableSO assets. All wired into DBs. ServiceLocator validated.

### 12.5 — Full 30-day playthrough test
- [ ] Open `00_Bootstrap`. Use `Tools/Debug/Skip to Day X` to spot-check days 1, 7, 15, 22, 30.
- [ ] On day 30 + last quest → `UIEnding` shows with computed grade.

### 12.6 — Bug fixes from playthrough
- [ ] Document any issues in `.wiki/wiki/bugs/`. Fix.

---

## Acceptance

- [ ] 30 DaySO exist with proper quest lists.
- [ ] Master playthrough reaches Day 30 + Ending without crash.
- [ ] Score-grade transitions verified (manipulate via debug menu to test all 3 grades).
- [ ] `automation_report.md` PASS.

Proceed to `phase-13-polish.md`.

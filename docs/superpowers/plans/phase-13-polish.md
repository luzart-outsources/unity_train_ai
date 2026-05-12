# Phase 13 — Polish + ending + build (blueprint)

**Goal:** Final pass: balance scores, refine UI feel, write README, produce a final standalone build artefact.

---

## Tasks

### 13.1 — Score balance pass
- [ ] Adjust `latePenalty`, `maxPointsPerLesson`, ending thresholds based on test playthrough feel.
- [ ] Decision: if Phase 12 reveals players too rarely hit Xuất sắc, lower threshold or increase quiz max.

### 13.2 — UI polish
- [ ] Tween fades for popups (`DOTween` optional; alpha lerp suffices).
- [ ] Quest arrow bobbing curve tuned.
- [ ] HUD layout responsive to aspect ratio.
- [ ] Add SFX hooks (if Audio system added — see [[open-questions#q-20260512-08]]).

### 13.3 — Endgame polish
- [ ] `UIEnding` visual: confetti or simple animation. Big text + grade + 2 scores.
- [ ] After OK → return to MainMenu, delete save (or keep with completion flag).

### 13.4 — Bug bash
- [ ] Run final test for known issues: tensor leaks (Profiler), NPC stuck in obstacles, save corruption on quit during transition.

### 13.5 — README + handoff
- [ ] Update `README.md` with build instructions + Editor menu cheatsheet.
- [ ] Update `.wiki/wiki/overview.md` Current State.

### 13.6 — Standalone build
- [ ] Switch to Windows Standalone target. Build. Verify `.exe` runs end-to-end.
- [ ] Confirm ONNX files included in StreamingAssets / Resources or embedded (Sentis ModelAsset bakes into Resources).

### 13.7 — Final commit + tag
- [ ] `git tag v1.0` after final commit.

---

## Acceptance

- [ ] Build artefact (Windows .exe) plays a full day without crash.
- [ ] All 3 ending grades reachable in test scenarios.
- [ ] README explains: clone → open Unity 6 → Tools/Build Game/Generate All → Play.
- [ ] `git tag v1.0` exists.

🎉 **Project complete.**

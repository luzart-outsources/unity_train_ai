# Phase 7 — UI (uGUI popups + HUDs) (blueprint)

**Goal:** Implement every popup + HUD from spec §6.3 using uGUI Canvas + TextMeshPro. `UIRouter` instantiates prefabs on demand.

**Spec refs:** §6, [[systems/ui-router]], [[decisions/d04-ugui-not-toolkit]].

---

## Tasks

### 7.1 — `OverlayCanvas` in Bootstrap scene
- [ ] Canvas with `Render Mode = Screen Space Overlay`, `Sort Order = 1000`, `DontDestroyOnLoad`. Add EventSystem.
- [ ] `UIRouter` MonoBehaviour holds `transform` reference to this Canvas.

### 7.2 — `UIScreenBase` abstract MonoBehaviour
- [ ] `OpenAsync()`, `CloseAsync()`, optional `OnInit(...)`. Uses UniTask.

### 7.3 — Per-popup prefab + script
- [ ] `UIMainMenu.prefab` + `UIMainMenu.cs` — NewGame/Continue/Exit buttons.
- [ ] `UICreateChar.prefab` + script — InputField + Confirm button → set `PlayerStateRSO.playerName`.
- [ ] `UILoading.prefab` + script — full black, TMP text, auto-close after `seconds`.
- [ ] `UIConfirm.prefab` + script — TMP text + OK button → resolves UniTask<bool>.
- [ ] `UIQuiz.prefab` + script — 1 TMP question text + 4 answer buttons + 15s countdown timer + Continue button. Tracks correct/total.
- [ ] `UIDialogue.prefab` + script — TMP scrollable history + InputField + Send button. Calls `DialogueService.Reply`.
- [ ] `UIInteractPrompt.prefab` + script — bottom-right button, mờ/sáng tween.
- [ ] `UIQuestHUD.prefab` + script — Title + countdown bound to `ActiveQuestRSO`.
- [ ] `UIClockHUD.prefab` + script — formatted day/weekday/HH:MM bound to `TimeTickMsg`.
- [ ] `UIScoreHUD.prefab` + script — portrait + scores bound to `ScoreChangedMsg`.
- [ ] `UIMiniMap.prefab` + script — RawImage with camera render of overhead Map.
- [ ] `UIJoystick.prefab` + script — 2D virtual joystick driving Input System's `Move` value.
- [ ] `UIEnding.prefab` + script — TMP title + scores + grade.
- [ ] `UIExpel.prefab` + script — UIConfirm variant → on OK loads MainMenu.

### 7.4 — `UIRouter` implementation
- [ ] Replace Phase 4 stub. Each `ShowX` instantiates corresponding prefab as child of `OverlayCanvas`, calls `OpenAsync`, awaits close, destroys instance.

### 7.5 — `BroadcastListenerComponent<T>` (UnityEvent bridge)
- [ ] Generic MonoBehaviour to bridge `BroadcastService<T>` to UnityEvent for Inspector wiring (Animator triggers, AudioPlayer).

### 7.6 — Manual scene test
- [ ] In a scratch scene, attach test buttons to `UIRouter.ShowConfirm("Test")`, `ShowQuiz(sampleQuizSet)`, `ShowDialogue(NPC_DaiDoiTruong)`. Verify each opens, accepts input, closes.

---

## Acceptance

- [ ] Every popup in spec §6.3 has a working prefab + script.
- [ ] `UIRouter` instantiates/destroys popups cleanly (no leaks across multiple opens).
- [ ] HUD elements update on broadcast messages.
- [ ] Manual test in scratch scene confirms all UIs render and dismiss.

Proceed to `phase-08-save.md`.

# Phase 6 — Player + NPC prefab + camera + quest arrow (blueprint)

**Goal:** Build the `Player.prefab`, `NPC.prefab`, `QuestArrow.prefab` with correct components, scripts, and Input wiring. Camera rig follows player. Arrow points to active quest area.

**Spec refs:** §10, [[entities/player]], [[decisions/d05-third-person-3d]], [[decisions/d11-character-controller]].

---

## Tasks

### 6.1 — `PlayerController` (CharacterController-based)
- [ ] `Assets/Scripts/Presentation/PlayerController.cs`
- [ ] Input System bindings: `Move`, `Look`, `Interact`, `MenuToggle`. Generate C# class from `.inputactions`.
- [ ] Move via `CharacterController.Move(forward * input.y + right * input.x) * speed * dt + gravity`.
- [ ] Rotate player to face camera yaw when `move.magnitude > 0.1`.

### 6.2 — `ThirdPersonCamera` + `ThirdPersonCameraConfigSO`
- [ ] CameraRig as child or sibling of Player; orbit around player with yaw/pitch from Look input.
- [ ] `Vector3.SmoothDamp` for follow. Pitch clamp [-20°, 50°].
- [ ] `ThirdPersonCameraConfigSO` Inspector fields: distance, height, yawSpeed, smoothTime, invertY.

### 6.3 — `PlayerInteractor` (trigger detection + facing check)
- [ ] Sphere trigger (radius 2m) on Player. `OnTriggerEnter/Exit` broadcasts `InteractZoneEnteredMsg/ExitedMsg`.
- [ ] `Update`: if zone present and `Vector3.Dot(forward, toZone) > 0.5` → enable UIInteractPrompt button (via UIRouter).
- [ ] `OnInteract` action callback → broadcast `InteractPressedMsg`.

### 6.4 — `QuestArrow3D.prefab` + `QuestArrowHUD`
- [ ] Cone + cylinder mesh, emissive yellow material. Total height ~0.5m.
- [ ] Gắn child Player at local `(0, 0.05, 0.4)`.
- [ ] `QuestArrowHUD` subscribes `QuestActivatedMsg/CompletedMsg/MissedMsg`. Each frame `arrow.LookAt(targetXZ)`. Bobbing via `AnimationCurveSO` (Y-sin 0.05m).

### 6.5 — `Player.prefab`
- [ ] Empty GameObject "Player".
- [ ] Children: Visual (capsule, 0.6 × 1.8 × 0.6), CharacterController, PlayerController, PlayerInteractor, QuestArrow (child prefab), CameraRig + Main Camera.
- [ ] Layer = "Player", tag = "Player".

### 6.6 — `NPC.prefab` variants
- [ ] `NPC_Static.prefab` (capsule + collider, no controller — Đại đội trưởng style).
- [ ] `NPC_Mobile.prefab` (capsule + CharacterController + `NpcView` script wiring `NPCSO.movementStrategy.Bind(transform)`).
- [ ] `NpcView` subscribes its NPC to `MovementService.RegisterAgent` on `Awake` (when ServiceLocator available).

### 6.7 — Test
- [ ] Manual playtest: open a scratch scene, drop Player.prefab + NPC.prefab + cube target. Walk player with WASD. Camera follows. Arrow points to cube. NPC stands or moves toward set target.

---

## Acceptance

- [ ] Player walks smoothly with WASD; camera follows; mouse rotates view.
- [ ] Arrow correctly indicates direction to any GameObject set as quest area.
- [ ] NPC_Static stays put. NPC_Mobile follows assigned target.
- [ ] Trigger zone toggles UIInteractPrompt indicator (uses stub UIRouter).

Proceed to `phase-07-ui.md`.

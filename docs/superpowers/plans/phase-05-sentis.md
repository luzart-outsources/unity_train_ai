# Phase 5 — Sentis integration (real)

> Blueprint. Each task ~30-60 min.

**Goal:** Replace stub `SentisRuntime` with real Workers. Implement `VietnameseTokenizer`, `IntentClassifier`, `ResponseFiller`, `OnnxMovementAgent`. Validate `intent_classifier.onnx` on real Vietnamese strings and `soldier.onnx` in an isolated test arena.

**Spec refs:** §5.1-5.5, [[systems/sentis-runtime]], [[systems/npc-dialogue]], [[systems/npc-movement]], [[technical/sentis-runtime-notes]].

---

## Tasks

### 5.1 — `SentisRuntime` concrete
- [ ] Replace stub. Load both models. Create `Worker(model, BackendType.GPUCompute)`. Fallback CPU if Compute unsupported (check via `SystemInfo.supportsComputeShaders`).
- [ ] `Dispose()` cleans both workers.
- [ ] PlayMode test: Bootstrap in test scene, verify both Workers non-null after `ServiceLocatorSO.Bootstrap()`.

### 5.2 — `VietnameseTokenizer`
- [ ] Constructor takes `intent_classifier_meta.json` content. Parse `vocab` dict, `max_len`, `pad_id`, `unk_id`.
- [ ] `int[] Encode(string text, int maxLen=32)`:
  - Lowercase.
  - Longest-match against vocab keys (some multi-word like `"báo cáo"`).
  - OOV → unk_id. Pad → pad_id.
- [ ] EditMode tests with vocab fixture: `"báo cáo đầy đủ"` → `[9, ...]`; `"xyz_unknown"` → `[1, 0, 0, ...]`.

### 5.3 — `IntentClassifier`
- [ ] Constructor takes `Worker` + label list. `Predict(int[] tokens) → IntentResult{intent, score, top2}`.
- [ ] Use `Tensor<int>` input, schedule, peek `Tensor<float>` output, softmax, argmax.
- [ ] **Disposal discipline**: every tensor `using`.
- [ ] PlayMode test (needs ModelAsset): load model, predict on hardcoded token sequence, assert returns one of 8 intents with score in [0,1].

### 5.4 — `ResponseFiller`
- [ ] Load `responses.json` TextAsset. Parse JSON → `Dictionary<IntentId, List<string>>`.
- [ ] `Fill(IntentId, NpcContext) → string` randomly picks template and replaces placeholders `{scheduled_today}`, `{meal_time}`, `{place}`, etc., from `NpcContext` fields + injected lambdas.
- [ ] EditMode test: stub context filler, verify substitution.

### 5.5 — Update `OnnxDialogueSO` to use real classifier + filler
- [ ] Constructor wires Tokenizer + IntentClassifier + ResponseFiller from `NpcContext`.
- [ ] `Reply()` flow: tokenize → predict → threshold → fill → return.
- [ ] Add `confidenceThreshold` Inspector field. Fallback `OUT_OF_SCOPE` if below.

### 5.6 — `DialogueService` wired
- [ ] Build `NpcContext` properly: `playerName` from `PlayerStateRSO`, `todaySummary` from `IQuestRouter.GetTodaySummary()`, etc.

### 5.7 — `OnnxMovementSO` + `OnnxMovementAgent`
- [ ] `OnnxMovementSO` Inspector fields: `rayMaxDist=10`, `maxSpeed=3.5`, `maxTurnRadPerSec=π`, `LayerMask obstacleMask`, `string targetTag="Target"`, `float gravity=9.81`.
- [ ] `OnnxMovementAgent`:
  - `BuildObs() → float[21]` per spec §5.3.
  - `Tick(object workerOrAgent, float dt)`: cast to `Worker`; `using` input tensor with shape `(1, 21)`; schedule; peek output; clamp thrust/turn; `CharacterController.Move(forward * thrust * maxSpeed * dt + gravity * dt)` + `Rotate(0, turn * maxTurnRadPerSec * Rad2Deg * dt, 0)`.
- [ ] PlayMode test in isolated arena: spawn cube target, NPC with `OnnxMovementSO`, verify NPC reaches target within 10s of in-arena distance.

### 5.8 — `MovementService` real Tick
- [ ] At 5Hz, call `agent.Tick(_sentis.SoldierWorker, 1f/5f)` for each registered agent.
- [ ] Optional batch: stack obs from N agents into one `Tensor<float>(N, 21)` and schedule once — defer optimization to later phase.

### 5.9 — QA pass (intent generalization)
- [ ] Hand-author a CSV `Assets/Tests/EditMode/intent_qa_samples.csv` with 30 real Vietnamese sentences, expected intent.
- [ ] Write EditMode test that loops and asserts ≥80% accuracy. If < 80%, log fails and surface in [[open-questions#q-20260512-04]].
- [ ] If fail → ESCALATE to user before proceeding. Options: (1) accept low acc with fallback, (2) retrain in `AI_Training/`.

---

## Acceptance

- [ ] `SentisRuntime` initializes both Workers without throw on GPU + CPU backend.
- [ ] Tokenizer encodes known vocab correctly.
- [ ] `IntentClassifier.Predict` returns valid intent on test sentences.
- [ ] `ResponseFiller` replaces all known placeholders.
- [ ] `OnnxMovementAgent` reaches target in isolated arena test.
- [ ] Intent QA test ≥80% on hand-authored 30 sentences (or escalation acknowledged).

Proceed to `phase-06-prefabs.md`.

# Phase 2 — SO base classes + 1 concrete each

> **For agentic workers:** Use `subagent-driven-development` or `executing-plans`.

**Goal:** Define abstract polymorphic `QuestSO`, `InteractionSO`, `MovementStrategySO`, `DialogueStrategySO`, `ScoreRuleSO`, `EndingRuleSO`, plus 1 minimal concrete subclass each so Editor `CreateAssetMenu` produces working assets and unit tests verify polymorphism.

**Architecture:** All bases live in `TrainAI.SO.Base.asmdef`. Concretes (1 placeholder each) live in `TrainAI.SO.Concrete.asmdef`. Context classes (`QuestContext`, `InteractionContext`, `NpcContext`, `ScoreContext`) defined here as plain DTOs.

**Tech Stack:** Unity SO + UniTask for async signatures.

**Spec refs:** spec §3.2-3.3, [[technical/so-taxonomy]], [[systems/quest-system]].

---

### Task 2.1: `QuestSO` base + `IQuestRuntime` interface

**Files:**
- Create: `Assets/Scripts/SO/Base/QuestSO.cs`
- Create: `Assets/Scripts/SO/Base/IQuestRuntime.cs`
- Create: `Assets/Scripts/SO/Base/QuestContext.cs`

- [ ] **Step 1:** Write `IQuestRuntime`:

```csharp
namespace TrainAI.SO.Base {
    public interface IQuestRuntime {
        void Tick(float dt);
        void Begin();
        void OnComplete(bool success);
    }
}
```

- [ ] **Step 2:** Write `QuestContext` (DTO injected when runtime is created):

```csharp
using UnityEngine;
namespace TrainAI.SO.Base {
    public class QuestContext {
        public Transform player;
        // services injected later via service interfaces (Phase 4):
        public System.Action<int> awardScoreDelta;       // wired in Phase 4 to ScoreSystem
        public System.Action<bool> markComplete;         // wired to QuestRouter
    }
}
```

- [ ] **Step 3:** Write `QuestSO`:

```csharp
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base {
    public abstract class QuestSO : ScriptableObject {
        [Required] public string id;
        public string title;
        [Required] public AreaSO area;
        public TimeRange window;
        public int latePenalty = 5;
        public abstract IQuestRuntime CreateRuntime(QuestContext ctx);
    }
}
```

> Note: `AreaSO` is referenced but not yet defined → will create in Task 2.2.

- [ ] **Step 4:** Compile will fail. That's expected — move to Task 2.2.

---

### Task 2.2: `AreaSO` data SO

**Files:** Create `Assets/Scripts/SO/Base/AreaSO.cs`

- [ ] **Step 1:** Write:

```csharp
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base {
    [CreateAssetMenu(fileName="Area_New", menuName="TrainAI/Data/Area")]
    public class AreaSO : ScriptableObject {
        [Required] public string id;
        public Vector3 worldPos;
        public Vector3 size = new Vector3(2, 2, 2);
        public SceneRefSO scene;      // home scene
        public SceneRefSO subScene;   // optional sub-scene to load on interact
    }
}
```

- [ ] **Step 2:** Create `SceneRefSO`:

```csharp
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base {
    [CreateAssetMenu(fileName="SceneRef_New", menuName="TrainAI/Data/Scene Ref")]
    public class SceneRefSO : ScriptableObject {
        [Required] public string sceneName;
        public LoadMode mode = LoadMode.Additive;
        public enum LoadMode { Single, Additive }
    }
}
```

- [ ] **Step 3:** Compile clean.

- [ ] **Step 4:** Commit (bundles 2.1+2.2 because they're co-dependent):

```bash
git add Assets/Scripts/SO/Base/
git commit -m "feat(so-base): QuestSO + AreaSO + SceneRefSO + IQuestRuntime"
```

---

### Task 2.3: `InteractionSO` base + context

**Files:** `Assets/Scripts/SO/Base/InteractionSO.cs`, `Assets/Scripts/SO/Base/InteractionContext.cs`

- [ ] **Step 1:** Write `InteractionContext`:

```csharp
using UnityEngine;

namespace TrainAI.SO.Base {
    public class InteractionContext {
        public Transform player;
        public AreaSO area;
        // later: IUIRouter, ISceneRouter, IGameClock injected by InteractionRouter
    }
}
```

- [ ] **Step 2:** Write `InteractionSO`:

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TrainAI.SO.Base {
    public abstract class InteractionSO : ScriptableObject {
        public string promptText = "Bấm E để tương tác";
        public Sprite icon;
        public abstract UniTask Execute(InteractionContext ctx);
    }
}
```

- [ ] **Step 3:** Compile clean (UniTask reference present per asmdef).

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/SO/Base/InteractionSO.cs Assets/Scripts/SO/Base/InteractionContext.cs
git commit -m "feat(so-base): InteractionSO abstract + InteractionContext"
```

---

### Task 2.4: `MovementStrategySO` + `IMovementAgent`

**Files:** `Assets/Scripts/SO/Base/MovementStrategySO.cs`, `Assets/Scripts/SO/Base/IMovementAgent.cs`

- [ ] **Step 1:** Write:

```csharp
using UnityEngine;
namespace TrainAI.SO.Base {
    public interface IMovementAgent {
        Vector3 Target { get; set; }
        void Tick(object workerOrAgent, float dt);    // workerOrAgent boxed to avoid Sentis ref here
    }
    public abstract class MovementStrategySO : ScriptableObject {
        public abstract IMovementAgent Bind(Transform npc);
    }
}
```

> The `object workerOrAgent` keeps `TrainAI.SO.Base` free of a Sentis reference; Onnx variants cast back to `Worker` in `TrainAI.SO.Concrete`.

- [ ] **Step 2:** Commit.

```bash
git add Assets/Scripts/SO/Base/MovementStrategySO.cs Assets/Scripts/SO/Base/IMovementAgent.cs
git commit -m "feat(so-base): MovementStrategySO abstract + IMovementAgent"
```

---

### Task 2.5: `DialogueStrategySO` + `NpcContext`

**Files:** `Assets/Scripts/SO/Base/DialogueStrategySO.cs`, `Assets/Scripts/SO/Base/NpcContext.cs`

- [ ] **Step 1:** Write:

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TrainAI.SO.Base {
    public class NpcContext {
        public string playerName;
        public string todaySummary;      // populated by IQuestRouter.GetTodaySummary()
        public int hocTap, renLuyen;
        // tokenizer + classifier injected by DialogueService (Phase 4-5)
        public System.Func<string, string> fillTemplate;  // ResponseFiller.Fill bound here
    }
    public abstract class DialogueStrategySO : ScriptableObject {
        public abstract UniTask<string> Reply(string userInput, NpcContext ctx);
    }
}
```

- [ ] **Step 2:** Commit.

```bash
git add Assets/Scripts/SO/Base/DialogueStrategySO.cs Assets/Scripts/SO/Base/NpcContext.cs
git commit -m "feat(so-base): DialogueStrategySO abstract + NpcContext"
```

---

### Task 2.6: `ScoreRuleSO`, `EndingRuleSO`

**Files:** `Assets/Scripts/SO/Base/ScoreRuleSO.cs`, `Assets/Scripts/SO/Base/EndingRuleSO.cs`, `Assets/Scripts/SO/Base/PlayerStateRSO.cs`

- [ ] **Step 1:** Define `PlayerStateRSO` (data here, full impl in Phase 3 but base type needed by rules):

```csharp
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.SO.Base {
    [CreateAssetMenu(fileName="PlayerStateRSO", menuName="TrainAI/Runtime/Player State")]
    public class PlayerStateRSO : RuntimeSO {
        public string playerName;
        public int hocTap;
        public int renLuyen;
        public string currentScene;
        public Vector3 lastWorldPos;
        public override void Reset() {
            playerName = "";
            hocTap = 0;
            renLuyen = 100;
            currentScene = "10_World";
            lastWorldPos = Vector3.zero;
        }
    }
}
```

- [ ] **Step 2:** `ScoreRuleSO`:

```csharp
using UnityEngine;
namespace TrainAI.SO.Base {
    public class ScoreContext {
        public int scoreDelta;
        public string sourceQuestId;
    }
    public abstract class ScoreRuleSO : ScriptableObject {
        public abstract void Apply(ScoreContext ctx, PlayerStateRSO state);
    }
}
```

- [ ] **Step 3:** `EndingRuleSO`:

```csharp
using UnityEngine;
namespace TrainAI.SO.Base {
    public enum EndingGrade { XuatSac=0, Tot=1, TrungBinh=2 }
    public abstract class EndingRuleSO : ScriptableObject {
        public abstract EndingGrade Evaluate(PlayerStateRSO state);
    }
}
```

- [ ] **Step 4:** Compile clean.

- [ ] **Step 5:** Commit.

```bash
git add Assets/Scripts/SO/Base/ScoreRuleSO.cs Assets/Scripts/SO/Base/EndingRuleSO.cs Assets/Scripts/SO/Base/PlayerStateRSO.cs
git commit -m "feat(so-base): ScoreRuleSO + EndingRuleSO + PlayerStateRSO"
```

---

### Task 2.7: Minimal concrete `GotoConfirmQuestSO` + test

**Files:** `Assets/Scripts/SO/Concrete/GotoConfirmQuestSO.cs`, `Assets/Tests/EditMode/GotoConfirmQuestSOTests.cs`

- [ ] **Step 1:** Write failing test:

```csharp
using NUnit.Framework;
using UnityEngine;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;

namespace TrainAI.EditMode.Tests {
    public class GotoConfirmQuestSOTests {
        [Test]
        public void CreateRuntime_ReturnsNonNull() {
            var q = ScriptableObject.CreateInstance<GotoConfirmQuestSO>();
            q.id = "test"; q.title = "Test"; q.area = ScriptableObject.CreateInstance<AreaSO>();
            var rt = q.CreateRuntime(new QuestContext());
            Assert.NotNull(rt);
            Object.DestroyImmediate(q); Object.DestroyImmediate(q.area);
        }
    }
}
```

- [ ] **Step 2:** Run → FAIL.

- [ ] **Step 3:** Implement:

```csharp
using UnityEngine;
using TrainAI.SO.Base;

namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Quest_GotoConfirm_New", menuName="TrainAI/Quest/Goto + Confirm")]
    public class GotoConfirmQuestSO : QuestSO {
        public string confirmText = "Bạn đang thực hiện hành động.";
        public int skipToHour = 6;
        public int skipToMinute = 0;

        public override IQuestRuntime CreateRuntime(QuestContext ctx)
            => new GotoConfirmQuestRuntime(this, ctx);
    }

    internal class GotoConfirmQuestRuntime : IQuestRuntime {
        readonly GotoConfirmQuestSO _so;
        readonly QuestContext _ctx;
        public GotoConfirmQuestRuntime(GotoConfirmQuestSO so, QuestContext ctx) { _so = so; _ctx = ctx; }
        public void Begin() { /* arrow + HUD wired in Phase 4 by QuestRouter */ }
        public void Tick(float dt) { /* deadline check by QuestRouter */ }
        public void OnComplete(bool success) {
            if (!success) _ctx.awardScoreDelta?.Invoke(-_so.latePenalty);
        }
    }
}
```

- [ ] **Step 4:** Run test → PASS.

- [ ] **Step 5:** Commit.

```bash
git add Assets/Scripts/SO/Concrete/GotoConfirmQuestSO.cs Assets/Tests/EditMode/GotoConfirmQuestSOTests.cs
git commit -m "feat(so-concrete): GotoConfirmQuestSO + runtime + test"
```

---

### Task 2.8: Minimal concrete strategies for each base

For each of the remaining base types, write ONE concrete placeholder so `CreateAssetMenu` works and Inspector wiring can be exercised in later phases.

**Files:**
- `Assets/Scripts/SO/Concrete/OpenConfirmInteractionSO.cs`
- `Assets/Scripts/SO/Concrete/IdleMovementSO.cs`
- `Assets/Scripts/SO/Concrete/MockDialogueSO.cs`
- `Assets/Scripts/SO/Concrete/LatePenaltyRuleSO.cs`
- `Assets/Scripts/SO/Concrete/MilitaryAcademyEndingSO.cs`

- [ ] **Step 1: `OpenConfirmInteractionSO`:**

```csharp
using UnityEngine;
using Cysharp.Threading.Tasks;
using TrainAI.SO.Base;
namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Interact_Confirm_New", menuName="TrainAI/Interaction/Open Confirm")]
    public class OpenConfirmInteractionSO : InteractionSO {
        public string confirmText = "Bạn đang làm gì đó.";
        public override async UniTask Execute(InteractionContext ctx) {
            // Real UIRouter call wired in Phase 4-7.
            Debug.Log($"[Confirm] {confirmText}");
            await UniTask.Yield();
        }
    }
}
```

- [ ] **Step 2: `IdleMovementSO`:**

```csharp
using UnityEngine;
using TrainAI.SO.Base;
namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Movement_Idle", menuName="TrainAI/Movement/Idle")]
    public class IdleMovementSO : MovementStrategySO {
        public override IMovementAgent Bind(Transform npc) => new IdleMovementAgent(npc);
    }
    internal class IdleMovementAgent : IMovementAgent {
        readonly Transform _t;
        public Vector3 Target { get; set; }
        public IdleMovementAgent(Transform t) { _t = t; }
        public void Tick(object _, float __) { /* no-op */ }
    }
}
```

- [ ] **Step 3: `MockDialogueSO`:**

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using TrainAI.SO.Base;
namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Dialogue_Mock", menuName="TrainAI/Dialogue/Mock")]
    public class MockDialogueSO : DialogueStrategySO {
        public override UniTask<string> Reply(string input, NpcContext ctx)
            => UniTask.FromResult($"[mock-reply to: {input}]");
    }
}
```

- [ ] **Step 4: `LatePenaltyRuleSO`:**

```csharp
using UnityEngine;
using TrainAI.SO.Base;
namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Rule_LatePenalty", menuName="TrainAI/Rule/Late Penalty")]
    public class LatePenaltyRuleSO : ScoreRuleSO {
        public int defaultPenalty = 5;
        public override void Apply(ScoreContext ctx, PlayerStateRSO state) {
            if (ctx.scoreDelta < 0) state.renLuyen += ctx.scoreDelta;   // delta is negative
            state.renLuyen = Mathf.Max(0, state.renLuyen);
        }
    }
}
```

- [ ] **Step 5: `MilitaryAcademyEndingSO`:**

```csharp
using UnityEngine;
using TrainAI.SO.Base;
namespace TrainAI.SO.Concrete {
    [CreateAssetMenu(fileName="Rule_Ending_Academy", menuName="TrainAI/Rule/Ending — Military Academy")]
    public class MilitaryAcademyEndingSO : EndingRuleSO {
        public override EndingGrade Evaluate(PlayerStateRSO s) {
            if (s.hocTap >= 432 && s.renLuyen >= 90) return EndingGrade.XuatSac;
            if (s.hocTap >= 288 && s.renLuyen >= 60) return EndingGrade.Tot;
            return EndingGrade.TrungBinh;
        }
    }
}
```

- [ ] **Step 6:** Commit.

```bash
git add Assets/Scripts/SO/Concrete/
git commit -m "feat(so-concrete): minimal placeholders for each Strategy SO base"
```

---

### Task 2.9: Ending rule unit test

**Files:** `Assets/Tests/EditMode/EndingRuleTests.cs`

- [ ] **Step 1:** Write test:

```csharp
using NUnit.Framework;
using UnityEngine;
using TrainAI.SO.Base;
using TrainAI.SO.Concrete;

namespace TrainAI.EditMode.Tests {
    public class EndingRuleTests {
        [Test]
        public void XuatSac_RequiresBothHigh() {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 432; st.renLuyen = 90;
            Assert.AreEqual(EndingGrade.XuatSac, rule.Evaluate(st));
        }
        [Test]
        public void Tot_MidRange() {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 300; st.renLuyen = 70;
            Assert.AreEqual(EndingGrade.Tot, rule.Evaluate(st));
        }
        [Test]
        public void TrungBinh_BelowThreshold() {
            var rule = ScriptableObject.CreateInstance<MilitaryAcademyEndingSO>();
            var st = ScriptableObject.CreateInstance<PlayerStateRSO>();
            st.hocTap = 100; st.renLuyen = 40;
            Assert.AreEqual(EndingGrade.TrungBinh, rule.Evaluate(st));
        }
    }
}
```

- [ ] **Step 2:** Run all 3 tests. Expected: PASS.

- [ ] **Step 3:** Commit.

```bash
git add Assets/Tests/EditMode/EndingRuleTests.cs
git commit -m "test(so-concrete): ending rule grade thresholds"
```

---

### Task 2.10: Create 1 sample asset of each base via Editor menu

- [ ] **Step 1:** In Unity Editor, navigate to `Assets/_Data/`:
  - `Areas/` right-click → Create → TrainAI → Data → Area → name `Area_TestSanVanDong`. Set worldPos `(0, 0, 5)`.
  - `Scenes/` create `SceneRef_World` (sceneName=`10_World`, mode=Additive).
  - `Strategies/` create `Movement_Idle`, `Dialogue_Mock`.
  - `Rules/` create `Rule_LatePenalty`, `Rule_Ending_Academy`.
  - `Quests/` create `Quest_TheDuc_d1_Test` (GotoConfirmQuestSO). Set `id=Q_TheDuc_d1`, `title=Tập thể dục`, drag `Area_TestSanVanDong` into `area`, set `window=(5,0,5,15)`, `latePenalty=5`, `confirmText="Bạn đang tập thể dục."`, `skipToHour=5, skipToMinute=30`.
  - `Interactables/` create `Interact_SanVanDong_Confirm` (OpenConfirmInteractionSO).
  - `Runtime/` create `PlayerStateRSO.asset`.

- [ ] **Step 2:** Verify all assets created. None show "missing reference" warnings.

- [ ] **Step 3:** Commit assets.

```bash
git add Assets/_Data/
git commit -m "chore(data): sample 1-of-each SO assets for smoke testing"
```

---

## Phase 2 acceptance

- [ ] 6 abstract bases defined: `QuestSO`, `InteractionSO`, `MovementStrategySO`, `DialogueStrategySO`, `ScoreRuleSO`, `EndingRuleSO`.
- [ ] 1 minimal concrete of each in `TrainAI.SO.Concrete.asmdef`.
- [ ] Data SO: `AreaSO`, `SceneRefSO`, `PlayerStateRSO` (RSO).
- [ ] `CreateAssetMenu` menu entries exist for each.
- [ ] Test count: `GotoConfirmQuestSOTests` (1), `EndingRuleTests` (3) added, all green.
- [ ] At least 9 commits this phase.

Proceed to `phase-03-data-so.md`.

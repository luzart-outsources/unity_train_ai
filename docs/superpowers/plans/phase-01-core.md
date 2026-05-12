# Phase 1 — Core (BroadcastService + RuntimeSO base + utilities)

> **For agentic workers:** REQUIRED SUB-SKILL: `superpowers:subagent-driven-development` or `superpowers:executing-plans`. Steps use `- [ ]`.

**Goal:** Build the event backbone (`BroadcastService<T>`) and the runtime state SO base class (`RuntimeSO`), both unit-tested. These are the foundation every later phase compiles against.

**Architecture:** Static typed pub/sub (no MonoBehaviour). `struct` messages. Abstract `RuntimeSO` resets on `OnEnable`.

**Tech Stack:** Pure C# (in `TrainAI.Core` asmdef), `System.Action`, `Dictionary<Type, Delegate>`. NUnit for tests.

**Spec refs:** spec §4.1-4.2, [[systems/broadcast-service]], [[decisions/d02-broadcast-service]], [[technical/so-taxonomy]] § RuntimeSO.

---

### Task 1.1: `BroadcastService` skeleton + Subscribe/Send round-trip test

**Files:**
- Create: `Assets/Scripts/Core/BroadcastService.cs`
- Create: `Assets/Tests/EditMode/BroadcastServiceTests.cs`

- [ ] **Step 1: Write failing test:**

```csharp
using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests {
    public class BroadcastServiceTests {
        public readonly struct PingMsg { public readonly int value; public PingMsg(int v){value=v;} }

        [SetUp] public void Setup() => BroadcastService.Clear();
        [TearDown] public void Teardown() => BroadcastService.Clear();

        [Test]
        public void Subscribe_Send_DeliversToHandler() {
            int received = -1;
            void Handler(PingMsg m) => received = m.value;
            BroadcastService.Subscribe<PingMsg>(Handler);
            BroadcastService.Send(new PingMsg(42));
            Assert.AreEqual(42, received);
        }
    }
}
```

- [ ] **Step 2: Run** EditMode test `Subscribe_Send_DeliversToHandler`. Expected: FAIL (compile error — `BroadcastService` not defined).

- [ ] **Step 3: Implement minimal `BroadcastService`:**

```csharp
using System;
using System.Collections.Generic;

namespace TrainAI.Core {
    public static class BroadcastService {
        static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler) where T : struct {
            if (handler == null) return;
            if (_handlers.TryGetValue(typeof(T), out var d))
                _handlers[typeof(T)] = Delegate.Combine(d, handler);
            else
                _handlers[typeof(T)] = handler;
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct {
            if (handler == null) return;
            if (!_handlers.TryGetValue(typeof(T), out var d)) return;
            var nd = Delegate.Remove(d, handler);
            if (nd == null) _handlers.Remove(typeof(T));
            else _handlers[typeof(T)] = nd;
        }

        public static void Send<T>(T msg) where T : struct {
            if (_handlers.TryGetValue(typeof(T), out var d))
                ((Action<T>)d)?.Invoke(msg);
        }

        public static void Clear() => _handlers.Clear();
    }
}
```

- [ ] **Step 4: Run test.** Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/Core/BroadcastService.cs Assets/Tests/EditMode/BroadcastServiceTests.cs
git commit -m "feat(core): BroadcastService typed pub/sub + round-trip test"
```

---

### Task 1.2: Unsubscribe test

**Files:**
- Modify: `Assets/Tests/EditMode/BroadcastServiceTests.cs`

- [ ] **Step 1: Add failing test (append inside class):**

```csharp
[Test]
public void Unsubscribe_StopsDelivery() {
    int received = -1;
    void Handler(PingMsg m) => received = m.value;
    BroadcastService.Subscribe<PingMsg>(Handler);
    BroadcastService.Unsubscribe<PingMsg>(Handler);
    BroadcastService.Send(new PingMsg(99));
    Assert.AreEqual(-1, received, "Handler should not run after unsubscribe");
}
```

- [ ] **Step 2: Run.** Expected: PASS (already implemented).

- [ ] **Step 3: Add multiple-subscribers test:**

```csharp
[Test]
public void MultipleSubscribers_AllReceive() {
    int a = 0, b = 0;
    BroadcastService.Subscribe<PingMsg>(m => a = m.value);
    BroadcastService.Subscribe<PingMsg>(m => b = m.value * 2);
    BroadcastService.Send(new PingMsg(5));
    Assert.AreEqual(5, a);
    Assert.AreEqual(10, b);
}
```

- [ ] **Step 4: Run.** Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Tests/EditMode/BroadcastServiceTests.cs
git commit -m "test(core): unsubscribe + multi-subscriber Broadcast tests"
```

---

### Task 1.3: `Clear` test — used by MainMenu transition

- [ ] **Step 1: Add test:**

```csharp
[Test]
public void Clear_RemovesAllHandlers() {
    int hit = 0;
    BroadcastService.Subscribe<PingMsg>(_ => hit++);
    BroadcastService.Clear();
    BroadcastService.Send(new PingMsg(1));
    Assert.AreEqual(0, hit);
}
```

- [ ] **Step 2: Run.** Expected: PASS.

- [ ] **Step 3: Commit.**

```bash
git add Assets/Tests/EditMode/BroadcastServiceTests.cs
git commit -m "test(core): Clear empties handler table"
```

---

### Task 1.4: `RuntimeSO` base class + test

**Files:**
- Create: `Assets/Scripts/Core/RuntimeSO.cs`
- Create: `Assets/Tests/EditMode/RuntimeSOTests.cs`

- [ ] **Step 1: Write failing test:**

```csharp
using NUnit.Framework;
using UnityEngine;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests {
    public class RuntimeSOTests {
        public class CounterRSO : RuntimeSO {
            public int counter;
            public override void Reset() => counter = 0;
        }

        [Test]
        public void OnEnable_ResetsState() {
            var so = ScriptableObject.CreateInstance<CounterRSO>();
            so.counter = 99;
            // Simulate OnEnable by re-creating:
            ScriptableObject.DestroyImmediate(so);
            so = ScriptableObject.CreateInstance<CounterRSO>();
            Assert.AreEqual(0, so.counter, "OnEnable should call Reset()");
            ScriptableObject.DestroyImmediate(so);
        }
    }
}
```

- [ ] **Step 2: Run.** Expected: FAIL (CounterRSO/RuntimeSO not defined).

- [ ] **Step 3: Implement `RuntimeSO`:**

```csharp
using UnityEngine;

namespace TrainAI.Core {
    public abstract class RuntimeSO : ScriptableObject {
        void OnEnable() => Reset();
        public abstract void Reset();
    }
}
```

- [ ] **Step 4: Run test.** Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/Core/RuntimeSO.cs Assets/Tests/EditMode/RuntimeSOTests.cs
git commit -m "feat(core): RuntimeSO abstract base with OnEnable→Reset"
```

---

### Task 1.5: `Required` attribute (used by Validator in Phase 11)

**Files:**
- Create: `Assets/Scripts/Core/RequiredAttribute.cs`

- [ ] **Step 1: Implement attribute (test deferred — actual usage in Phase 11):**

```csharp
using System;

namespace TrainAI.Core {
    /// <summary>Marker for fields that Validator must check non-null in Phase 11.</summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class RequiredAttribute : Attribute { }
}
```

- [ ] **Step 2: Verify compile.**

- [ ] **Step 3: Commit.**

```bash
git add Assets/Scripts/Core/RequiredAttribute.cs
git commit -m "feat(core): [Required] attribute marker for Validator phase 11"
```

---

### Task 1.6: `TimeRange` value type (used by `QuestSO.window`)

**Files:**
- Create: `Assets/Scripts/Core/TimeRange.cs`
- Modify: `Assets/Tests/EditMode/` add `TimeRangeTests.cs`

- [ ] **Step 1: Write failing test:**

```csharp
using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests {
    public class TimeRangeTests {
        [Test]
        public void Contains_HourInRange_ReturnsTrue() {
            var r = new TimeRange(5, 0, 5, 15);
            Assert.IsTrue(r.Contains(5, 10));
        }
        [Test]
        public void Contains_HourOutOfRange_ReturnsFalse() {
            var r = new TimeRange(5, 0, 5, 15);
            Assert.IsFalse(r.Contains(5, 20));
            Assert.IsFalse(r.Contains(4, 59));
        }
        [Test]
        public void TotalMinutes_Compute() {
            var r = new TimeRange(7, 30, 11, 30);
            Assert.AreEqual(240, r.TotalMinutes);
        }
    }
}
```

- [ ] **Step 2: Run.** Expected: FAIL.

- [ ] **Step 3: Implement:**

```csharp
using System;
using UnityEngine;

namespace TrainAI.Core {
    [Serializable]
    public struct TimeRange {
        public int startHour, startMinute;
        public int endHour,   endMinute;

        public TimeRange(int sh, int sm, int eh, int em) {
            startHour = sh; startMinute = sm; endHour = eh; endMinute = em;
        }

        public bool Contains(int hour, int minute) {
            int t = hour * 60 + minute;
            int s = startHour * 60 + startMinute;
            int e = endHour * 60 + endMinute;
            return t >= s && t < e;
        }

        public int TotalMinutes => (endHour * 60 + endMinute) - (startHour * 60 + startMinute);

        public override string ToString() =>
            $"{startHour:D2}:{startMinute:D2}–{endHour:D2}:{endMinute:D2}";
    }
}
```

- [ ] **Step 4: Run.** Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/Core/TimeRange.cs Assets/Tests/EditMode/TimeRangeTests.cs
git commit -m "feat(core): TimeRange struct + Contains/TotalMinutes tests"
```

---

### Task 1.7: `Weekday` enum + `WeekdayHelper` skip-weekend logic

**Files:**
- Create: `Assets/Scripts/Core/Weekday.cs`
- Create: `Assets/Tests/EditMode/WeekdayTests.cs`

- [ ] **Step 1: Write failing test:**

```csharp
using NUnit.Framework;
using TrainAI.Core;

namespace TrainAI.EditMode.Tests {
    public class WeekdayTests {
        [Test] public void Next_FromFriday_SkipsWeekend_ToMonday() {
            Assert.AreEqual(Weekday.Monday, WeekdayHelper.Next(Weekday.Friday, skipWeekend: true));
        }
        [Test] public void Next_FromMonday_NoSkip_GoesToTuesday() {
            Assert.AreEqual(Weekday.Tuesday, WeekdayHelper.Next(Weekday.Monday, skipWeekend: false));
        }
        [Test] public void Next_FromSaturday_SkipFalse_GoesToSunday() {
            Assert.AreEqual(Weekday.Sunday, WeekdayHelper.Next(Weekday.Saturday, skipWeekend: false));
        }
    }
}
```

- [ ] **Step 2: Run.** Expected: FAIL.

- [ ] **Step 3: Implement:**

```csharp
namespace TrainAI.Core {
    public enum Weekday { Monday=1, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday }

    public static class WeekdayHelper {
        public static Weekday Next(Weekday current, bool skipWeekend) {
            var next = (Weekday)(((int)current % 7) + 1);
            if (skipWeekend && (next == Weekday.Saturday || next == Weekday.Sunday))
                return Weekday.Monday;
            return next;
        }
    }
}
```

- [ ] **Step 4: Run.** Expected: PASS.

- [ ] **Step 5: Commit.**

```bash
git add Assets/Scripts/Core/Weekday.cs Assets/Tests/EditMode/WeekdayTests.cs
git commit -m "feat(core): Weekday enum + skip-weekend Next helper"
```

---

### Task 1.8: Message catalog (struct shells, no logic)

**Files:**
- Create: `Assets/Scripts/Core/Messages.cs`

The full catalog from spec §4.2. These are *empty shells*: fields use `object` placeholders that later phases narrow when concrete SO types exist.

- [ ] **Step 1:** Create file:

```csharp
namespace TrainAI.Core.Messages {
    // ---- Time / Day ----
    public readonly struct DayStartedMsg { public readonly int day; public readonly Weekday weekday;
        public DayStartedMsg(int d, Weekday w){day=d; weekday=w;} }
    public readonly struct DayEndedMsg   { public readonly int day; public DayEndedMsg(int d){day=d;} }
    public readonly struct TimeTickMsg   { public readonly int hour, minute;
        public TimeTickMsg(int h, int m){hour=h; minute=m;} }

    // ---- Quest ----  (UnityEngine.Object substituted later with QuestSO)
    public readonly struct QuestActivatedMsg { public readonly UnityEngine.Object quest;
        public QuestActivatedMsg(UnityEngine.Object q){quest=q;} }
    public readonly struct QuestCompletedMsg { public readonly UnityEngine.Object quest; public readonly bool success; public readonly int scoreDelta;
        public QuestCompletedMsg(UnityEngine.Object q, bool s, int sd){quest=q; success=s; scoreDelta=sd;} }
    public readonly struct QuestMissedMsg    { public readonly UnityEngine.Object quest;
        public QuestMissedMsg(UnityEngine.Object q){quest=q;} }

    // ---- Score ----
    public readonly struct ScoreChangedMsg   { public readonly int hocTap, renLuyen;
        public ScoreChangedMsg(int h, int r){hocTap=h; renLuyen=r;} }
    public readonly struct ExpelTriggeredMsg { }

    // ---- Interaction ----
    public readonly struct InteractZoneEnteredMsg { public readonly UnityEngine.Object target;
        public InteractZoneEnteredMsg(UnityEngine.Object t){target=t;} }
    public readonly struct InteractZoneExitedMsg  { public readonly UnityEngine.Object target;
        public InteractZoneExitedMsg(UnityEngine.Object t){target=t;} }
    public readonly struct InteractPressedMsg     { public readonly UnityEngine.Object target;
        public InteractPressedMsg(UnityEngine.Object t){target=t;} }

    // ---- Scene ----
    public readonly struct SceneTransitionRequestedMsg { public readonly UnityEngine.Object scene;
        public SceneTransitionRequestedMsg(UnityEngine.Object s){scene=s;} }

    // ---- Dialogue / Quiz / Ending ----
    public readonly struct DialogueRequestMsg { public readonly UnityEngine.Object npc; public readonly string input;
        public DialogueRequestMsg(UnityEngine.Object n, string i){npc=n; input=i;} }
    public readonly struct DialogueRepliedMsg { public readonly UnityEngine.Object npc; public readonly string text;
        public DialogueRepliedMsg(UnityEngine.Object n, string t){npc=n; text=t;} }
    public readonly struct QuizStartedMsg     { public readonly UnityEngine.Object set;
        public QuizStartedMsg(UnityEngine.Object s){set=s;} }
    public readonly struct QuizEndedMsg       { public readonly int correctCount, totalCount;
        public QuizEndedMsg(int c, int t){correctCount=c; totalCount=t;} }
    public readonly struct GameEndedMsg       { public readonly int gradeIndex;     // 0=XuatSac,1=Tot,2=TB
        public GameEndedMsg(int g){gradeIndex=g;} }
}
```

- [ ] **Step 2:** Compile. No errors.

- [ ] **Step 3:** Commit.

```bash
git add Assets/Scripts/Core/Messages.cs
git commit -m "feat(core): Message catalog struct shells per spec §4.2"
```

> Note: later phases will replace `UnityEngine.Object` placeholders with concrete `QuestSO`, `InteractableSO`, etc., once those types exist (Phase 2-3). At that point, refactor commit `refactor(core): narrow Message types to concrete SO`.

---

### Task 1.9: Run all EditMode tests, verify green

- [ ] **Step 1:** Open Test Runner → EditMode → Run All.
- [ ] **Step 2:** Expected: 8+ tests pass. `BroadcastServiceTests` 4 tests, `RuntimeSOTests` 1, `TimeRangeTests` 3, `WeekdayTests` 3, `SkeletonSmokeTests` 1.
- [ ] **Step 3:** If any fail, fix before proceeding. No commit (verify only).

---

## Phase 1 acceptance

- [ ] `BroadcastService` Subscribe/Send/Unsubscribe/Clear all green.
- [ ] `RuntimeSO` resets state on OnEnable.
- [ ] `TimeRange` + `Weekday` utilities tested.
- [ ] All Phase 0 + 1 tests pass.
- [ ] Console clean.
- [ ] At least 8 commits this phase.

Proceed to `phase-02-so-base.md`.

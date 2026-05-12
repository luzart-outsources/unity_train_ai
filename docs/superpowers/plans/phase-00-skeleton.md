# Phase 0 — Project skeleton (asmdef, folders, ServiceLocator stub)

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans`. Steps use `- [ ]` syntax.

**Goal:** Lay down the assembly definition tree, folder structure, and an empty `ServiceLocatorSO` so the entire architecture has a compile-ready home before any logic is written.

**Architecture:** 8 asmdefs in 1-direction dependency order; folders under `Assets/_Data/`, `Assets/_Models/`, `Assets/Scripts/{Core,SO/Base,SO/Concrete,Services,Presentation,UI,Sentis,Editor}`, `Assets/Tests/{EditMode,PlayMode}`, `Assets/Editor/Automation/`.

**Tech Stack:** Unity 6, no additional packages this phase.

**Spec refs:** spec §2.3 folder layout, §13.1 asmdef tree, [[technical/asmdef-structure]].

---

### Task 0.1: Create top-level folders

**Files:**
- Create: `Assets/_Data/`, `Assets/_Data/{Days,Quests,NPCs,Interactables,Quizzes,Strategies,Areas,Scenes,Rules,Config,Runtime}/`
- Create: `Assets/_Models/`
- Create: `Assets/Scripts/{Core,SO/Base,SO/Concrete,Services,Presentation,UI,Sentis,Editor}/`
- Create: `Assets/Tests/{EditMode,PlayMode}/`
- Create: `Assets/Editor/Automation/`

- [ ] **Step 1:** From Unity, right-click in Project window → Create → Folder. Create each folder listed above. (Or via OS: `mkdir -p` paths inside `Assets/`. After OS-create, return to Editor → wait for asset re-import.)

- [ ] **Step 2:** Verify by Unity Project window showing folder tree exactly as above. No `.meta` orphans.

- [ ] **Step 3:** Commit.

```bash
git add Assets/
git commit -m "chore(structure): create top-level Assets folder tree per spec §2.3"
```

---

### Task 0.2: Add `TrainAI.Core.asmdef`

**Files:**
- Create: `Assets/Scripts/Core/TrainAI.Core.asmdef`

- [ ] **Step 1:** In `Assets/Scripts/Core/`, right-click → Create → Assembly Definition. Name `TrainAI.Core`. Unity will create `TrainAI.Core.asmdef`.

- [ ] **Step 2:** Open the file. Replace with:

```json
{
    "name": "TrainAI.Core",
    "rootNamespace": "TrainAI.Core",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save. Wait for Unity recompile. Check Console — no errors.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/Core/TrainAI.Core.asmdef Assets/Scripts/Core/TrainAI.Core.asmdef.meta
git commit -m "chore(asmdef): add TrainAI.Core base assembly"
```

---

### Task 0.3: Add `TrainAI.SO.Base.asmdef`

**Files:**
- Create: `Assets/Scripts/SO/Base/TrainAI.SO.Base.asmdef`

- [ ] **Step 1:** Create asmdef as above in `Assets/Scripts/SO/Base/`.

- [ ] **Step 2:** Set contents:

```json
{
    "name": "TrainAI.SO.Base",
    "rootNamespace": "TrainAI.SO.Base",
    "references": ["TrainAI.Core"],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, wait for recompile, verify Console clean.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/SO/Base/
git commit -m "chore(asmdef): add TrainAI.SO.Base referencing Core"
```

---

### Task 0.4: Add `TrainAI.Sentis.asmdef`

**Files:**
- Create: `Assets/Scripts/Sentis/TrainAI.Sentis.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/Sentis/`.

- [ ] **Step 2:** Contents:

```json
{
    "name": "TrainAI.Sentis",
    "rootNamespace": "TrainAI.Sentis",
    "references": [
        "TrainAI.Core",
        "Unity.InferenceEngine",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** If Unity reports "Cannot find Unity.InferenceEngine assembly" — open `Packages/manifest.json` and confirm `com.unity.ai.inference` is listed; if missing, ask user to install via Package Manager. The assembly name in Unity 6 may be `Unity.AI.Inference` instead of `Unity.InferenceEngine` — try that variant if first fails.

- [ ] **Step 4:** Save + verify clean Console.

- [ ] **Step 5:** Commit.

```bash
git add Assets/Scripts/Sentis/
git commit -m "chore(asmdef): add TrainAI.Sentis referencing Unity.InferenceEngine + UniTask"
```

---

### Task 0.5: Add `TrainAI.SO.Concrete.asmdef`

**Files:**
- Create: `Assets/Scripts/SO/Concrete/TrainAI.SO.Concrete.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/SO/Concrete/`.

- [ ] **Step 2:** Contents:

```json
{
    "name": "TrainAI.SO.Concrete",
    "rootNamespace": "TrainAI.SO.Concrete",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.Sentis",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, verify Console.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/SO/Concrete/
git commit -m "chore(asmdef): add TrainAI.SO.Concrete"
```

---

### Task 0.6: Add `TrainAI.Services.asmdef`

**Files:**
- Create: `Assets/Scripts/Services/TrainAI.Services.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/Services/`.

- [ ] **Step 2:** Contents:

```json
{
    "name": "TrainAI.Services",
    "rootNamespace": "TrainAI.Services",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Sentis",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, verify Console.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/Services/
git commit -m "chore(asmdef): add TrainAI.Services"
```

---

### Task 0.7: Add `TrainAI.Presentation.asmdef`

**Files:**
- Create: `Assets/Scripts/Presentation/TrainAI.Presentation.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/Presentation/`.

- [ ] **Step 2:** Contents:

```json
{
    "name": "TrainAI.Presentation",
    "rootNamespace": "TrainAI.Presentation",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Services",
        "Unity.InputSystem",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, verify Console.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/Presentation/
git commit -m "chore(asmdef): add TrainAI.Presentation"
```

---

### Task 0.8: Add `TrainAI.UI.asmdef`

**Files:**
- Create: `Assets/Scripts/UI/TrainAI.UI.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/UI/`.

- [ ] **Step 2:** Contents:

```json
{
    "name": "TrainAI.UI",
    "rootNamespace": "TrainAI.UI",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Services",
        "Unity.TextMeshPro",
        "Unity.InputSystem",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, verify Console.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/UI/
git commit -m "chore(asmdef): add TrainAI.UI"
```

---

### Task 0.9: Add `TrainAI.Editor.asmdef`

**Files:**
- Create: `Assets/Scripts/Editor/TrainAI.Editor.asmdef`

- [ ] **Step 1:** Create asmdef in `Assets/Scripts/Editor/`.

- [ ] **Step 2:** Contents (note `includePlatforms: ["Editor"]`):

```json
{
    "name": "TrainAI.Editor",
    "rootNamespace": "TrainAI.Editor",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Sentis",
        "TrainAI.Services",
        "TrainAI.Presentation",
        "TrainAI.UI"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": false,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3:** Save, verify Console.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/Editor/
git commit -m "chore(asmdef): add TrainAI.Editor (Editor-only)"
```

---

### Task 0.10: Add Test asmdefs

**Files:**
- Create: `Assets/Tests/EditMode/TrainAI.EditMode.Tests.asmdef`
- Create: `Assets/Tests/PlayMode/TrainAI.PlayMode.Tests.asmdef`

- [ ] **Step 1:** Ensure Test Framework is installed (Package Manager → Test Framework).

- [ ] **Step 2:** In `Assets/Tests/EditMode/`, right-click → Create → Testing → Tests Assembly Folder (Unity will set up).

- [ ] **Step 3:** Open the generated asmdef and overwrite:

```json
{
    "name": "TrainAI.EditMode.Tests",
    "rootNamespace": "TrainAI.EditMode.Tests",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Services",
        "TrainAI.Sentis",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": ["Editor"],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 4:** Repeat for `PlayMode/`:

```json
{
    "name": "TrainAI.PlayMode.Tests",
    "rootNamespace": "TrainAI.PlayMode.Tests",
    "references": [
        "TrainAI.Core",
        "TrainAI.SO.Base",
        "TrainAI.SO.Concrete",
        "TrainAI.Services",
        "TrainAI.Presentation",
        "TrainAI.UI",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": ["nunit.framework.dll"],
    "autoReferenced": false,
    "defineConstraints": ["UNITY_INCLUDE_TESTS"],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 5:** Verify Test Runner window shows both assemblies.

- [ ] **Step 6:** Commit.

```bash
git add Assets/Tests/
git commit -m "chore(test): add EditMode + PlayMode test asmdefs"
```

---

### Task 0.11: Stub `ServiceLocatorSO`

**Files:**
- Create: `Assets/Scripts/Services/ServiceLocatorSO.cs`

- [ ] **Step 1:** Create the file with stub:

```csharp
using UnityEngine;

namespace TrainAI.Services {
    [CreateAssetMenu(fileName="ServiceLocator", menuName="TrainAI/Service Locator")]
    public class ServiceLocatorSO : ScriptableObject {
        public void Bootstrap() {
            // Real wiring fills in during Phase 4.
            Debug.Log("[ServiceLocator] Bootstrap stub — no services wired yet.");
        }
    }
}
```

- [ ] **Step 2:** Wait for compile. Right-click in `Assets/_Data/Config/` → Create → TrainAI → Service Locator → name it `ServiceLocator.asset`.

- [ ] **Step 3:** Verify asset created, Inspector shows the SO.

- [ ] **Step 4:** Commit.

```bash
git add Assets/Scripts/Services/ServiceLocatorSO.cs Assets/_Data/Config/ServiceLocator.asset
git commit -m "feat(services): stub ServiceLocatorSO with Bootstrap()"
```

---

### Task 0.12: Edit-mode smoke test — assemblies compile

**Files:**
- Create: `Assets/Tests/EditMode/SkeletonSmokeTests.cs`

- [ ] **Step 1:** Write the failing test:

```csharp
using NUnit.Framework;
using UnityEngine;
using TrainAI.Services;

namespace TrainAI.EditMode.Tests {
    public class SkeletonSmokeTests {
        [Test]
        public void ServiceLocator_CanInstantiate() {
            var so = ScriptableObject.CreateInstance<ServiceLocatorSO>();
            Assert.NotNull(so, "ServiceLocatorSO should be instantiable");
            so.Bootstrap();   // must not throw
            ScriptableObject.DestroyImmediate(so);
        }
    }
}
```

- [ ] **Step 2:** Open Test Runner (Window → General → Test Runner) → EditMode tab → Run.
  - **Expected**: PASS. If FAIL with "ServiceLocatorSO not found" → asmdef reference missing. Inspect `TrainAI.EditMode.Tests.asmdef` references.

- [ ] **Step 3:** Commit.

```bash
git add Assets/Tests/EditMode/SkeletonSmokeTests.cs
git commit -m "test(skeleton): smoke test ServiceLocatorSO instantiation"
```

---

### Task 0.13: Verify asmdef dependency graph (no cycle)

- [ ] **Step 1:** Open Unity → Project Settings → Assembly Definitions Reference Inspector? (or visualize via `Window/General/Assemblies` if installed). Verify:
  - `TrainAI.Core` has 0 internal refs.
  - `TrainAI.SO.Base` → Core.
  - `TrainAI.Sentis` → Core.
  - `TrainAI.SO.Concrete` → SO.Base, Sentis.
  - `TrainAI.Services` → Core, SO.Base, SO.Concrete, Sentis.
  - `TrainAI.Presentation` → Services, SO.Base.
  - `TrainAI.UI` → Services, SO.Base.
  - `TrainAI.Editor` → all above (Editor-only).

- [ ] **Step 2:** No cycle: walk each asmdef's references, check none point back to an ancestor.

- [ ] **Step 3:** If cycle detected, **STOP and fix** — re-open offending asmdef and remove the back-reference. Move type to lower asmdef if needed.

- [ ] **Step 4:** No commit (verification step). If a fix was needed, commit with `fix(asmdef): break cycle X → Y`.

---

## Phase 0 acceptance

- [ ] All 8 production asmdefs + 2 test asmdefs exist with correct references.
- [ ] Folder tree under `Assets/` matches spec §2.3.
- [ ] `ServiceLocatorSO.asset` created in `Assets/_Data/Config/`.
- [ ] Edit-mode smoke test `SkeletonSmokeTests` passes.
- [ ] `git log --oneline` shows 12 commits from this phase.
- [ ] Unity Console clean (no errors, no asmdef cycle warnings).

When all 6 boxes ✅, proceed to `phase-01-core.md`.

---

## Notes for the executing agent

- If `Unity.InferenceEngine` reference fails in Task 0.4, try alternative names (`Unity.AI.Inference`, `Sentis`). The internal Unity assembly name is set by the package; check `Packages/com.unity.ai.inference/Runtime/Unity.InferenceEngine.asmdef` after the package is imported for the canonical name.
- If TestRunner doesn't appear, open Package Manager → Unity Registry → install "Test Framework" (usually pre-installed in Unity 6).
- Folder `.meta` files MUST be committed alongside their folder; never `.gitignore` `.meta` inside `Assets/`.

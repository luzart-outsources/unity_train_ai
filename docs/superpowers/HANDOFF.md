# Handoff Guide — Continue TrainAI on Another Machine / New Claude Conversation

This guide explains how to (a) pull this project to a different computer and (b) start a fresh Claude Code session that has full context to continue implementation without re-doing brainstorming or design.

---

## Part 1 — Move the project to another machine

### Prerequisites on the target machine
- **Git** (with LFS optional but not required — ONNX files are small ~50KB–1MB).
- **Unity Hub** + **Unity 6** (the exact version this project was created with — check `ProjectSettings/ProjectVersion.txt` after clone to confirm).
- **VS Code** or **Rider** (for C# editing).
- **Claude Code** (the CLI agent) installed and authenticated.
- **Python 3.10** (only if you plan to retrain ONNX models in `AI_Training/phase_a_sentis/`).

### Step 1 — Clone the repo

```bash
cd /path/to/your/workspace
git clone <REPO_URL> unity_train_ai
cd unity_train_ai
```

If using GitHub, set the remote:
```bash
git remote -v   # verify origin
```

### Step 2 — Restore Unity packages

Open the project in Unity Hub → Add → select the cloned folder → open with Unity 6 (matching version in `ProjectSettings/ProjectVersion.txt`). Unity will:
1. Resolve packages from `Packages/manifest.json` (downloads `com.unity.ai.inference 2.6.1`, UniTask via Git URL, Input System, etc.).
2. Generate `Library/` (large, gitignored — first import ~5-10 min on slow networks).
3. Trigger compile. Console should be clean.

If a package fails (e.g., UniTask Git URL unreachable), edit `Packages/manifest.json` to swap registry; commit fix.

### Step 3 — Verify project loads

- Open `Assets/Scenes/00_Bootstrap.unity` (if Phase 0+ done) or scratch scene.
- Console clean → ready.
- If `Library/` was committed by accident, delete it (`git rm -r --cached Library/ && git commit -m "chore: gitignore Library"`).

### Step 4 — Restore ONNX model files

ONNX deliverables in `AI_Training/deliverables/` are git-tracked (small files), so clone gets them automatically. If git LFS is configured for `.onnx`:
```bash
git lfs install
git lfs pull
```

### Step 5 — (Optional) Recreate Python venv for retraining

```bash
cd AI_Training/phase_a_sentis
python -m venv .venv
.venv/Scripts/python -m pip install -r requirements.txt    # Windows
# or .venv/bin/python on Linux/Mac
```

---

## Part 2 — Start a new Claude conversation with full context

A fresh Claude conversation starts with zero memory of this brainstorming. You need to load (1) the spec, (2) the wiki, (3) the plans, into the new session efficiently.

### Strategy: 3-step "context bootstrap" prompt

Open Claude Code in the project directory:
```bash
cd /path/to/unity_train_ai
claude
```

Then paste this **opening message** (or save as `docs/superpowers/CONTEXT_BOOTSTRAP.md` and reference):

````
Tôi đang tiếp tục dự án TrainAI — game mô phỏng học kỳ quân sự đề tài ĐATN.
Context đã được thiết kế và chốt trong phiên chat trước. Vui lòng:

1. Đọc spec master:
   `docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md`

2. Đọc master plan:
   `docs/superpowers/plans/PLAN_MASTER.md`

3. Đọc wiki overview + index (game-wiki skill):
   - `.wiki/CLAUDE.md` (schema)
   - `.wiki/wiki/overview.md`
   - `.wiki/wiki/index.md`
   - 11 decisions trong `.wiki/wiki/decisions/d01-...d11-*.md`

4. Kiểm tra trạng thái hiện tại bằng git log:
   `git log --oneline -20`
   Cho tôi biết Phase nào đã xong và Phase nào tiếp theo cần làm.

5. Bật skill `superpowers:subagent-driven-development` để thực thi
   phase tiếp theo theo plan tương ứng (`docs/superpowers/plans/phase-NN-*.md`).

Ngôn ngữ trao đổi: tiếng Việt. Kỹ thuật stack: Unity 6 + uGUI + UniTask + Input System + com.unity.ai.inference (Sentis kế nhiệm, namespace Unity.InferenceEngine).
````

Claude will read the files in parallel, summarize current state, and propose the next phase. You confirm or correct, then it begins implementation.

### Why this works

- **Spec is the immutable design contract** — Claude doesn't re-design.
- **Master plan is the index** — Claude knows phase order.
- **Per-phase plans** are bite-sized → Claude executes one task at a time with frequent commits.
- **Wiki provenance** (claims.md, decisions/) lets Claude check rationale without you re-explaining.
- **Git log** tells Claude what's done.

### Alternative: use `superpowers:executing-plans` directly

If you prefer batch execution within the conversation:
```
Đọc docs/superpowers/plans/phase-00-skeleton.md rồi dùng skill superpowers:executing-plans để chạy nó từ task 0.1 đến acceptance.
```

---

## Part 3 — Handoff between machines mid-phase

If you started Phase 2 on Machine A and want to continue on Machine B:

1. **Commit + push on A**:
   ```bash
   git add -A
   git commit -m "wip(phase-2): in progress through task 2.5"
   git push
   ```
2. **Pull on B**:
   ```bash
   git pull
   ```
3. Open the project in Unity 6, wait for re-import.
4. Open Claude on B with the context bootstrap above + add:
   ```
   Tôi đang ở giữa Phase 2, task 2.5 vừa xong. Đọc git log để xác nhận và làm task 2.6 tiếp theo.
   ```

---

## Part 4 — What to back up

Critical (must back up — git covers these):
- `Assets/` (all code, prefabs, scenes, SO assets, ONNX, responses.json)
- `Packages/manifest.json`
- `ProjectSettings/`
- `AI_Training/deliverables/` (ONNX + meta)
- `docs/superpowers/` (specs + plans + this handoff)
- `.wiki/` (wiki + raw GDD)

Do NOT back up:
- `Library/` (auto-regenerated by Unity)
- `Temp/`, `Logs/`, `obj/`, `Build/`
- `AI_Training/phase_a_sentis/.venv/` (recreate via `requirements.txt`)

Sample `.gitignore` keeps these out — verify by running `git status` and confirming none of the above appear.

---

## Part 5 — Verification checklist after handoff

After moving the project, run these checks to confirm everything works:

- [ ] `git status` clean (no uncommitted changes, no missing files).
- [ ] Unity opens project without errors. Console clean.
- [ ] `.wiki/` directory exists with `CLAUDE.md` + `wiki/` + `raw/` subdirectories.
- [ ] `docs/superpowers/specs/` and `docs/superpowers/plans/` directories present.
- [ ] `AI_Training/deliverables/*.onnx` files present (run `ls AI_Training/deliverables/` from shell).
- [ ] Test Runner shows EditMode tests (if Phase 0-2 complete) → run them → green.
- [ ] Claude Code session can read `.wiki/wiki/overview.md` and `docs/superpowers/plans/PLAN_MASTER.md` from this directory.
- [ ] If Phase 9+ done: open `00_Bootstrap.unity`, press Play, reach MainMenu.

When all boxes check, you're fully bootstrapped on the new environment.

---

## Part 6 — If something is broken

| Symptom | Fix |
|---|---|
| Unity says "missing package com.unity.ai.inference" | Open Package Manager → install manually, or add `"com.unity.ai.inference": "2.6.1"` to `Packages/manifest.json` |
| UniTask Git URL fails | Re-add via Package Manager → "Add package from git URL" → paste `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask` |
| ONNX files missing | They're git-tracked under `AI_Training/deliverables/`. Verify `git ls-files | grep onnx`. If absent, fetch from original machine. |
| Asmdef cycle error | Open `Validator.cs` (Phase 11) or manually check each `.asmdef` references graph. Use [[technical/asmdef-structure]] as canonical. |
| Compile errors after pull | `Library/ScriptAssemblies/` may be stale; in Unity: Edit → Preferences → External Tools → Regenerate project files. Then reopen. |
| Sentis `BackendType.GPUCompute` crashes | Switch `GameConfigSO.preferredBackend` to `BackendType.CPU`. |

---

## Part 7 — Continuing collaboration with the user (Vietnamese, casual)

The user prefers:
- Tiếng Việt trong hội thoại.
- Hỏi từng câu một, multiple choice khi có thể.
- Code SOLID, SO-First, mở rộng nhanh.
- Tránh overengineering — chỉ làm những gì spec ghi.
- Confirm trước khi commit hoặc destructive action.
- Bám sát plans/per-phase files; báo trước khi đổi phase order.

Saved in `.wiki/CLAUDE.md` under "This project's custom rules" — every new session should re-read that file.

---

**End of handoff guide.**

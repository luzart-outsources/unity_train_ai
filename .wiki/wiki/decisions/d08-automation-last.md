---
title: D-08 Automation tooling is the last phase
category: decisions
tags: [automation, phasing]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md]
created: 2026-05-12
updated: 2026-05-12
---

## D-08 — Automation = Phase 11-12, after vertical slice stable
**Date**: 2026-05-12
**Decided by**: User
**Status**: active

### Context
User: "Sau khi xong thì thêm các tool automation, tức là những scriptableobject thì để claude tự tạo, tự gán, tự sửa các chỉ số... Cái này làm sau cùng sau khi test tất cả các hệ thống trên ok."

### Decision
Implementation order:
- Phase 0-10: build manual vertical slice (1-day playable end-to-end).
- Phase 11: Editor tools (`SOGenerator`, `SceneBuilder`, `PrefabBuilder`, `ServiceWizard`, `Validator`, `MasterBuilder`).
- Phase 12: Run master `Generate All` for 30-day content via `world_template.json`.
- Phase 13: polish + balance.

### Consequences
- Avoids the failure mode of generating 200+ assets against an unstable schema.
- Validator checks compile + null refs + asmdef + scene-in-build-settings before claiming success.
- Master builder writes `Library/automation_report.md` PASS/FAIL.

## Backlinks
- [[technical/automation-tooling]]

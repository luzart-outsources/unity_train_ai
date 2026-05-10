---
title: Import DATN repo làm base game
category: decisions
tags: [datn, import, decision, scope]
sources: [Assets/Scripts/, Packages/manifest.json]
created: 2026-05-11
updated: 2026-05-11
---

## Import code DATN làm base game

**Date**: 2026-05-11
**Decided by**: User (chủ wiki) + Quyền (im plicit qua việc Quyền upload code)
**Status**: active

### Context

Trước 2026-05-11, project Unity của user chỉ có `Assets/AI/` (Phase A v1/v2 brain) + 1 scene test movement (`PhaseB_MovementTest.unity`). Phần *game thật* (player controller, scene doanh trại, NPC, UI) chưa tồn tại. Quyền đã đẩy code game lên `https://github.com/manhquyenkma/DATN` (ĐATN của bạn ấy) — code là Stardew Valley clone tutorial-grade nhưng có structure đầy đủ (~22 system).

User yêu cầu clone repo về và copy `Assets/` vào project, giữ lại `Assets/AI/`.

### Options considered

1. **Import full DATN, giữ AI/** ⭐ chọn
   - Pro: nhanh, có ngay full game loop để play test, nhiều system tham khảo
   - Pro: structure tốt (Singleton + ITimeTracker + SO content) cho việc thêm AI integration
   - Con: theme farming, không khớp GDD quân đội — phải reskin
   - Con: ~698MB binary (POLYGON Farm pack), repo phình
   - Con: có debt (BinaryFormatter, magic string blackboard, hardcoded LLM key)

2. **Chỉ copy code, không asset**
   - Pro: nhẹ
   - Con: scene/prefab không chạy được (thiếu mesh/texture) → phải tự dựng từng scene
   - Con: tốn thời gian dựng asset thay vì làm gameplay

3. **Build from scratch**
   - Pro: clean, không debt
   - Con: 5 tháng deadline, ko đủ thời gian
   - Con: user không phụ trách game (chỉ AI), không nên ôm hết

4. **Dùng Unity Asset Store template trả phí**
   - Pro: chất lượng cao
   - Con: license phí, không hợp ĐATN sinh viên

### Decision

**Chọn Option 1**: Import toàn bộ `Assets/*` của DATN, giữ `Assets/AI/`. Xoá URP-related vì user dùng Built-in. Add `www.nulltale.socollection` vào `Packages/manifest.json` để fix compile error.

### Consequences

- **Theme mismatch**: code farming, GDD nói quân đội. Phải reskin (đổi prefab, dialogue, NPC) sau. Wiki record contradiction. Nguy cơ không kịp deadline nếu reskin cồng kềnh.
- **License risk**: POLYGON Farm là asset Synty trả phí. Repo public DATN đã upload — nếu Quyền release commercial cần mua license. Đối với demo ĐATN trường thì OK.
- **Repo bloat**: `git add Assets/` sẽ tăng repo từ vài MB lên ~700MB binary. Nên xem xét `.gitignore` hoặc Git LFS sau.
- **Architecture acceptable**: 22 system Quyền đã có. Phase A integration cần wire vào DialogueManager. Phase B integration cần wrap lính NPC vào CharacterMovement + ITimeTracker.
- **Debt phải clean trước nộp**:
  - BinaryFormatter → JSON
  - Hardcoded API key trong `LLMNetworkManager.cs`
  - Magic string blackboard (low priority)

### Follow-up tasks

- [ ] Xác nhận với Quyền: theme cuối cùng là farming hay quân sự? Reskin ở mức nào?
- [ ] Ghi note trong [[contradictions]] về GDD vs code mismatch
- [ ] Xoá `D:/OutSources/Unity_AI/_DATN_clone/` (~700MB) sau khi confirm import OK
- [ ] Mở Unity, check Console: nếu compile error → log + fix tiếp
- [ ] Wire `Assets/AI/` Phase A vào `DialogueManager` của DATN (sau)

### Snapshot rollback

Snapshot trước khi import: commit `b2e26bf` (`Snapshot truoc khi import asset tu repo manhquyenkma/DATN`). Lệnh rollback hoàn toàn nếu cần:
```bash
git reset --hard b2e26bf
rm -rf Assets/Animation Assets/Imported\ Asset Assets/Lighting Assets/Prefabs Assets/Resources Assets/Scripts Assets/TextMesh\ Pro Assets/TextureAtlasSlicer Assets/UI\ Elements Assets/UI\ Toolkit Assets/allthestuff*
git checkout Assets/Scenes/
```

---
## Backlinks
- [[sources/datn-game-repo]]
- [[index]]
- [[overview]]

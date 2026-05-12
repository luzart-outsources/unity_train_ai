# Context Bootstrap — Paste at start of new Claude conversation

Tôi đang tiếp tục dự án TrainAI — game mô phỏng học kỳ quân sự đề tài ĐATN Nguyễn Mạnh Quyền. Context đã được thiết kế và chốt trong phiên chat trước. Vui lòng:

1. Đọc spec master:
   `docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md`

2. Đọc master plan:
   `docs/superpowers/plans/PLAN_MASTER.md`

3. Đọc wiki overview + index + 11 decisions (game-wiki skill):
   - `.wiki/CLAUDE.md`
   - `.wiki/wiki/overview.md`
   - `.wiki/wiki/index.md`
   - `.wiki/wiki/decisions/d01-so-first-modular.md` ... `d11-character-controller.md`

4. Kiểm tra trạng thái hiện tại:
   ```bash
   git log --oneline -20
   ls docs/superpowers/plans/
   ```
   Tóm tắt cho tôi: Phase nào đã hoàn thành (theo commit messages), Phase nào đang dở, Phase nào tiếp theo.

5. Đề xuất next action:
   - Nếu Phase đang dở → resume task tiếp theo theo plan file tương ứng (`docs/superpowers/plans/phase-NN-*.md`).
   - Nếu Phase đã xong + acceptance pass → đề xuất sang Phase kế tiếp.
   - Dùng skill `superpowers:subagent-driven-development` (recommended) hoặc `superpowers:executing-plans` để thực thi.

**Ngôn ngữ trao đổi**: tiếng Việt.

**Tech stack**: Unity 6 · uGUI · UniTask · Input System · `com.unity.ai.inference` 2.6.1 (Sentis kế nhiệm, namespace `Unity.InferenceEngine`) · CharacterController · SO-First Modular + BroadcastService.

**Quy tắc ngầm** (từ `.wiki/CLAUDE.md`):
- SO-First triệt để; mỗi thêm quest/NPC/môn = tạo asset, không sửa core.
- Mọi event qua `BroadcastService<TMsg>` (struct, typed).
- Không SoCollection; DB SO dùng `List<T>` thuần.
- Placeholder thủ công per-prefab (không registry chung).
- Automation tooling đặt Phase 11-12 (cuối cùng).
- Asmdef 1-chiều, validator check cycle.
- Tensor `using`-disposed bắt buộc cho mọi Sentis call.

Bắt đầu bằng cách đọc files trên + git log → tóm tắt → đề xuất next action.

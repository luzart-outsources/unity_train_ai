---
title: Cắt scope — không làm 8 mini-game
category: decisions
tags: [scope, project-management, gameplay]
sources: [raw/gdd/context_v1.md, raw/gdd/context_v2.md]
created: 2026-05-07
updated: 2026-05-07
---

## Cắt scope — không làm 8 mini-game

**Date**: ~2026-04 (trước khi gộp context v2)
**Decided by**: Quyền + AI helper
**Status**: active

### Context

Đề cương ĐATN ban đầu của Quyền có 30 ngày simulation, 8 mini-game khi đi học (Rhythm, Quiz, FPS, Tower Defense, Maze, Memory, Card, Drag-drop). Với 5 tháng và 1 dev → không khả thi.

### Options considered

1. **Làm tất cả** — thua deadline
2. **Làm 2-3 mini-game** — vẫn nhiều, chia thời gian
3. **Cắt hết, vào lớp = fade to black + cộng stats** ⭐ chọn

### Decision

**Option 3**. Vào lớp giờ chỉ là:
```
Đi tới lớp → Bấm E → Fade to black → Pass time → +stats → Quay lại điều khiển
```

### Consequences

**Trade-offs accepted**:
- Demo "đi học" mất chiều sâu — chỉ là trigger, không có gameplay
- Người chơi không "cảm" được sự khác nhau giữa các môn học

**Lợi ích**:
- Tập trung tối đa vào day-loop + AI (Sentis chat + lính tự đi) — đây là phần khó nhất
- 7 ngày demo (không bắt buộc 30) đủ cho ĐATN
- 2 endings (Pass / Fail) thay vì branching tree phức tạp

### Phạm vi cuối cùng

- Vòng lặp ngày demo (ưu tiên 7 ngày)
- Tập thể dục sáng (rhythm đơn giản 30s)
- Đi học = fade + cộng stat
- 3 lựa chọn Player Choice buổi tối
- 2 endings (Pass / Fail)
- AI lính tự đi theo schedule ([[systems/movement-ai]])
- AI chỉ huy nói chuyện ([[systems/sentis-chat]])
- 3 chỉ số Adaptive: Kỷ luật / Thể lực / Kiến thức

### Backlinks

- [[overview]]
- [[world/scope]]

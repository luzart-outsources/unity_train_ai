---
title: Contradictions
category: meta
created: 2026-05-07
updated: 2026-05-11
---

# Contradictions

When a new GDD/spec/playtest disputes an existing claim or page, record it here instead of overwriting silently. Keep both sides until resolved.

## Format

```markdown
### x-YYYYMMDD-NN — <topic>
- **Claim A** ([[claims#c-...]] or page ref): "..."
  - Source: `raw/gdd/combat-v2.md`
- **Claim B** (newer, conflicting): "..."
  - Source: `raw/feedback/playtest-2025-01.md`
- **Status**: open | resolved → A | resolved → B | superseded by [[decisions/...]]
- **Resolution notes**:
```

## Open contradictions

<!-- Append below. Move resolved entries to "Resolved" with the decision link. -->

### x-20260511-01 — Theme farming vs GDD quân đội
- **Claim A** (GDD đề cương ĐATN): Game đề tài là *"mô phỏng học kỳ quân đội"* — trải nghiệm sinh viên KTMM khi học quân đội. Vòng lặp ngày: tập sáng → đi học → buổi tối Player Choice → ngủ. Stat: Kỷ luật / Thể lực / Kiến thức.
  - Source: `raw/gdd/context_v1.md`, `raw/gdd/context_v2.md`, `Nguyễn Mạnh Quyền -Đề cương ĐATN.docx`
- **Claim B** (DATN repo code, 2026-05-11): Code thực tế là Stardew Valley farming clone — gieo crop, nuôi gà, chăn nuôi, festival mùa xuân, shop bán hạt giống, ShippingBin tally tiền. Không có gì military.
  - Source: `Assets/Scripts/Farming/`, `Assets/Scripts/Animals/`, `Assets/Scripts/Festivals/`, [[sources/datn-game-repo]]
- **Status**: open
- **Resolution notes**: Có 2 hướng:
  1. Quyền dự định reskin DATN code thành quân đội (đổi prefab/dialogue, có thể giữ farming reskin "tăng gia") — chờ Quyền confirm
  2. DATN code chỉ là code base structure, sẽ rewrite lại nhiều phần — phải hỏi Quyền tiến độ
  - Cần Quyền confirm trước khi viết tiếp wiki page entities/world cho game cuối.

## Resolved

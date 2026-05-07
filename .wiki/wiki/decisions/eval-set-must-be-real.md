---
title: Eval set phải đủ khó để có signal
category: decisions
tags: [eval, methodology, real-world-test]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

## Eval set phải đủ khó để có signal

**Date**: 2026-05-07
**Decided by**: AI helper
**Status**: active

### Context

Phase A v2/v3 train xong → tất cả 3 archs đều 100% trên test 16 câu. Khi user yêu cầu "scale data nhiều hơn", thấy ngay vấn đề: làm sao đo được cải thiện khi metric đã saturate?

→ Test cũ là **sanity check**, không phải **eval thực**.

Khi mở rộng lên 64 câu khó (slang, telex typo, compound, OOD), spread accuracy:
- FastText: 25%
- LSTM: 95.3%
- Transformer: 18.8%

Đây là số liệu CÓ SIGNAL. Có thể đo cải thiện qua các iter loop.

### Options considered

1. **Giữ test cũ + báo cáo "đã đạt 100%"** — sai sự thật, ĐATN sẽ bị giáo viên phát hiện
2. **Mở rộng test lên 64 câu** ⭐ chọn
3. **Auto-generate test** từ data mới (held-out split) — nhưng vẫn cùng distribution, ko đo được OOD

### Decision

**Option 2 — viết tay test 64 câu**.

Cách thiết kế test:
- 8 câu mỗi intent (cân bằng)
- Mỗi nhóm 8: 2 câu sạch keyword rõ, 2 câu có typo / no-accent (telex), 2 câu compound dài, 2 câu adversarial near-confusion (e.g. "có gì ăn không em đói" — gần OUT_OF_SCOPE)
- OUT_OF_SCOPE: real student small talk (KTMM, tech, food)

### Consequences

**Trade-offs accepted**:
- Test set 64 câu vẫn nhỏ — không đủ cho ĐATN report (cần 200-500 câu)
- Hand-crafted = bias của tác giả, có thể không reflect Quyền's real users

**Follow-up**:
- Khi Quyền test trong Unity → record các câu user thật gõ, append vào eval set
- ĐATN final cần expand lên 200+ câu

### Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[technical/architecture-comparison]]
- [[bugs/fasttext-overfit-narrow-test]]

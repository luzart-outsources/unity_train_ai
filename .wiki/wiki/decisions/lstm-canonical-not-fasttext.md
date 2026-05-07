---
title: Đổi canonical Phase A từ FastText sang LSTM
category: decisions
tags: [phase-a, architecture, canonical, deliverable]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

## Đổi canonical Phase A từ FastText sang LSTM

**Date**: 2026-05-07
**Decided by**: AI helper (sau khi mở rộng eval test set)
**Status**: active (supersedes earlier "FastText is best" claim trong HANDOFF.md sáng)

### Context

Sáng 7/5, sau v3 baseline (16k samples), test set 16-câu cho thấy:
- FastText: 16/16 = 100% ⭐
- LSTM: 15/16 = 93.8%
- Transformer: 14/16 = 87.5%

→ HANDOFF.md ban đầu công bố "FastText canonical, LSTM/Transformer overkill".

Trưa 7/5, mở rộng test lên 64 câu (slang, telex typo, compound, OOD):
- **FastText: 25%** ‼️
- **LSTM: 95.3%** ⭐
- **Transformer: 18.8%**

Test cũ misleading vì chỉ kiểm tra câu keyword đơn giản — không phân biệt được model có thực sự generalize hay chỉ overfit pattern.

### Options considered

1. **Giữ FastText canonical** — đã document trong HANDOFF.md. Đổi sẽ confuse Quyền.
2. **Đổi canonical sang LSTM** — phản ánh đúng performance trên test thực.
3. **Cung cấp cả 3 archs, để Quyền tự chọn** — đẩy quyết định cho Quyền.

### Decision

**Option 2 — Đổi canonical sang LSTM**, đồng thời:
- Giữ legacy file name `fasttext_intent.onnx` cho backward-compat với HANDOFF.md cũ (nội dung là LSTM)
- Thêm canonical mới `intent_classifier.onnx` (cùng nội dung)
- Document arch winner trong `intent_classifier_winner.txt`

Logic: Quyền tin "nhất quán" hơn "đúng tên file". HANDOFF.md sáng đã nói "drag fasttext_intent.onnx vào Unity" → giữ tên file đó để Quyền không phải thay đổi.

### Consequences

**Trade-offs accepted**:
- Tên `fasttext_intent.onnx` chứa LSTM model — gây nhầm lẫn nếu Quyền inspect kỹ
- Mỗi iter loop phải copy onnx winning arch sang `fasttext_intent.onnx` (đã code)

**Follow-up**:
- Update HANDOFF.md để rõ canonical là LSTM
- Mở rộng test set lên 100+ câu cho ĐATN final report
- Cảnh báo Quyền: "model file tên 'fasttext_*' nhưng implement là LSTM"

> [!warning]
> Nếu Quyền thấy model file 'fasttext_intent.onnx' ~217KB thì biết là LSTM (FastText thật chỉ ~52KB). Size khác biệt rõ rệt.

### Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[technical/architecture-comparison]]
- [[decisions/eval-set-must-be-real]]
- [[bugs/fasttext-overfit-narrow-test]]

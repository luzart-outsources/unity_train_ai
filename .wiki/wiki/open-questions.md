---
title: Open Questions
category: meta
created: 2026-05-07
updated: 2026-05-07
---

# Open Questions

Câu hỏi chưa trả lời. Append khi gặp, move xuống "Answered" khi resolve.

## Format

```markdown
### q-YYYYMMDD-NN — <question>
- **Why it matters**: gameplay/tech impact
- **Where it surfaced**: [[systems/x]] / raw/...
- **Status**: open | answered → [[decisions/...]] | obsolete
```

## Open

### q-20260507-01 — Quyền có chốt 8 intent này không?
- **Why matters**: Nếu Quyền muốn thêm intent (KHEN_NGOI, PHẠT, GIAI_THICH...) → phải retrain Phase A từ đầu.
- **Where surfaced**: [[systems/sentis-chat]], [[claims#c-20260507-04]]
- **Candidates**: 8 intent hiện tại đủ cho ĐATN demo cơ bản
- **Status**: open

### q-20260507-02 — Bao giờ test ONNX trong Unity scene thật?
- **Why matters**: Có thể có Sentis 2.x API gotcha, raycast direction CCW vs CW, tokenization mismatch.
- **Where surfaced**: [[entities/commander-npc]], [[entities/soldier-npc]]
- **Status**: open — chờ Quyền có thời gian

### q-20260507-03 — Phase B sim-to-real gap (doanh trại thật)
- **Why matters**: Train env arena 15-25m × random 3-12 obstacles. Doanh trại thật có hành lang hẹp 1.5m → có thể policy fail.
- **Where surfaced**: [[systems/movement-ai]], [[entities/soldier-npc]]
- **Mitigation**: vòng 2 train với obstacle dày hơn (chưa làm)
- **Status**: open — đợi test thực tế

### q-20260507-04 — Confidence threshold tối ưu cho Sentis chat
- **Why matters**: 0.40 hiện tại có thể quá thấp/cao. Quá thấp → false positive intent. Quá cao → too many fallback OUT_OF_SCOPE.
- **Where surfaced**: [[entities/commander-npc]]
- **Candidates**: A/B test {0.30, 0.40, 0.50, 0.60}
- **Status**: open

### q-20260507-05 — TTS tiếng Việt cho NPC chỉ huy?
- **Why matters**: Immersion ++. Nhưng thêm 1 model = phức tạp setup, +disk size, +latency.
- **Where surfaced**: [[entities/commander-npc]]
- **Candidates**: Coqui-TTS, Piper TTS, vbee-cloud
- **Status**: out-of-scope cho ĐATN demo

### q-20260507-06 — Phase B: có cần thêm raycast cho NPC layer (multi-agent)?
- **Why matters**: Nếu 2 lính cùng tới 1 điểm cùng lúc → đụng nhau (obs hiện không thấy NPC khác).
- **Where surfaced**: [[entities/soldier-npc]]
- **Candidates**:
  1. Stagger schedule (đơn giản, no retrain)
  2. Add NPC layer raycast → retrain với 23-dim obs
- **Status**: open — vòng 2 nếu cần

## Answered

### q-20260507-07 — Train Phase B bằng ML-Agents hay standalone Python?
- **Resolved**: [[decisions/standalone-ppo-not-ml-agents]]

### q-20260507-08 — FastText hay LSTM làm canonical?
- **Resolved**: [[decisions/lstm-canonical-not-fasttext]]

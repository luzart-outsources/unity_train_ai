---
title: Phase B — Train standalone PyTorch PPO thay ML-Agents
category: decisions
tags: [phase-b, ml-agents, ppo, training-architecture]
sources: [raw/gdd/context_v2.md, raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

## Phase B — Train standalone PyTorch PPO thay ML-Agents

**Date**: 2026-05-07
**Decided by**: AI helper (vai trò train AI cho Quyền)
**Status**: active

### Context

V1 và v2 context handoff dự kiến train Phase B bằng Unity ML-Agents toolkit. Khi user nói "tự làm tự train thông đêm, tôi đi ngủ", phải đánh giá lại tính khả thi.

ML-Agents Toolkit:
- YÊU CẦU Unity Editor mở suốt training (Python communicator gắn vào Editor)
- Train 5M steps ~ 6-15h
- Editor có thể sleep / freeze nếu user khóa máy → training ngắt

### Options considered

1. **Option A — ML-Agents Unity (kế hoạch ban đầu)**
   - Pros: native Unity, scene tự visualize, sample envs có sẵn (Hallway, PushBlock)
   - Cons: bắt buộc Unity Editor mở; khó "thông đêm tự chạy"; nếu user lock screen → fail

2. **Option B — Standalone Python Gymnasium + SB3 PPO** ⭐ chọn
   - Pros: 100% độc lập với Unity; chạy nohup background được; output ONNX vẫn nạp được vào Unity 6
   - Cons: phải tự code env (~200 lines numpy); raycast trong Python có bug có thể không match Unity

3. **Option C — Cloud GPU + ML-Agents (rent)**
   - Pros: train nhanh, không tốn pin máy local
   - Cons: phức tạp setup; không match constraint "thông đêm tự chạy local"

### Decision

**Option B — Standalone PyTorch PPO**.

Lý do: constraint "thông đêm tự train" loại bỏ Option A. Cloud (Option C) overkill cho ĐATN demo. Output cuối cùng đều là 1 file ONNX → Unity load như nhau.

### Consequences

**Trade-offs accepted**:
- Phải tự code Gymnasium env mô phỏng raycast 2D (`nav_env.py`)
- Phải đảm bảo observation contract trong env Python KHỚP với raycast Unity (raycast direction CCW vs CW khác nhau, đã fix)
- Mất khả năng visualize training trong Unity Editor — chỉ có log mean_reward
- Quyền không cần cài `mlagents` Python package

**Follow-up**:
- Quyền tự code `MovementAgent.cs` đọc raycast Unity theo cùng layout obs
- Wiki: [[technical/unity-integration]] document raycast direction gotcha
- Backup options trong deliverables: `soldier_v2_fixedenv.onnx` (fixed 6 obstacles) và `soldier.onnx` (random env)

### Backlinks

- [[overview]]
- [[systems/movement-ai]]
- [[technical/unity-integration]]

---
title: Game Scope
category: world
tags: [scope, gameplay, design]
sources: [raw/gdd/context_v1.md, raw/gdd/context_v2.md]
created: 2026-05-07
updated: 2026-05-07
---

# Game Scope — Final ĐATN

Phần này chỉ tóm tắt — Quyền sở hữu game, đây là context cho AI work.

## Game loop

```
Wake up (06:00)
    ↓
Tập thể dục sáng (rhythm 30s đơn giản)
    ↓
Ăn sáng (cộng stat: Thể lực)
    ↓
Đi học (fade to black, +Kiến thức)
    ↓
Ăn trưa
    ↓
Học chính trị / tăng gia / tự học (fade)
    ↓
Ăn tối
    ↓
Player Choice 3 lựa chọn (gặp chỉ huy / đọc sách / tập riêng / nói chuyện / nghỉ ngơi...)
    ↓
Ngủ → Day++
```

## Day count

Ưu tiên **7 ngày** demo. 30 ngày là mục tiêu kéo dài nếu thời gian.

## Stats Adaptive

3 chỉ số tăng/giảm theo lựa chọn:
- **Kỷ luật**: tăng khi đúng giờ, làm theo schedule. Giảm khi vắng / ngủ trễ
- **Thể lực**: tăng khi tập, ăn đủ. Giảm khi bỏ tập
- **Kiến thức**: tăng khi đi học, tự học, đọc sách

## 2 Endings

- **Pass**: cuối kỳ cả 3 stat ≥ threshold → Quân đội đánh giá đạt
- **Fail**: bất kỳ stat nào < threshold → đánh giá không đạt

## NPCs có AI

- [[entities/commander-npc]] — sĩ quan chỉ huy (Sentis chat)
- [[entities/soldier-npc]] — lính nền tự đi (Movement AI)

## Phạm vi đã CẮT

[[decisions/scope-cuts|Decision]]: KHÔNG làm 8 mini-game (Rhythm, Quiz, FPS, Tower Defense...). Vào lớp = fade + cộng stats only.

## Backlinks

- [[overview]]
- [[decisions/scope-cuts]]
- [[entities/commander-npc]]
- [[entities/soldier-npc]]

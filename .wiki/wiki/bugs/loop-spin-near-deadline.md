---
title: Loop spin 493k iterations gần deadline
category: bugs
tags: [overnight-loop, infinite-loop, deadline-handling]
sources: [raw/technical/overnight_v2.log]
created: 2026-05-07
updated: 2026-05-07
---

# Loop spin 493k iterations gần deadline

## Symptoms

Sau khi v2 overnight loop chạy ~2h:
```
[04:58:59] [B] skipping (only 1.0 min left, need 12)
[04:58:59] --- iter 493894 starting (1.0 min left) ---
[04:58:59] [B] skipping (only 1.0 min left, need 12)
[04:58:59] --- iter 493895 starting (1.0 min left) ---
[04:59:00] === deadline reached ===
[04:59:00]      iterations: 493895
```

493,895 iterations — trong 1 phút loop spin tốc độ kinh khủng.

## Repro

`overnight_loop.py` (v2):
```python
while True:
    tl = time_left()
    if tl <= 60:
        break
    state["iter"] += 1
    
    if tl >= MIN_TIME_FOR_LIGHT:
        phase_a_iter(...)
    
    tl = time_left()
    if tl >= MIN_TIME_FOR_HEAVY:
        phase_b_iter(...)
    else:
        log("skipping")
```

Khi `MIN_TIME_FOR_LIGHT < tl < MIN_TIME_FOR_HEAVY` → cả 2 phase skip. Loop body return, while continue. Không sleep → spin vô tận.

## Root cause

Loop không có `time.sleep()` ở case "đã skip cả 2". `tl` giảm chậm (chỉ qua wall clock) nên while exit chỉ khi clock vượt deadline.

Trong 60 giây ngay trước deadline:
- Cả 2 phase skip
- Mỗi iter ~120 microseconds (chỉ time check + log + state save)
- → 60s / 120μs = **500,000 iter** — match thực tế

## Fix

`overnight_loop.py` v3:
```python
while True:
    tl = time_left()
    if tl <= 60:
        break
    state["iter"] += 1
    
    did_anything = False
    if tl >= MIN_TIME_FOR_LIGHT:
        phase_a_iter(...)
        did_anything = True
    
    tl = time_left()
    if tl >= MIN_TIME_FOR_HEAVY:
        phase_b_iter(...)
        did_anything = True
    
    # SPIN GUARD
    if not did_anything:
        time.sleep(SPIN_GUARD_SLEEP)   # 30s
```

Sleep 30s nếu không có work nào → loop body chạy max 2 lần/min thay vì 500k lần.

## Affected

- v2 overnight log — 54MB toàn dòng "iter 493xxx skipping". Đã trim trong wiki/raw.
- v2 best metrics — KHÔNG ảnh hưởng (best_phase_a/b lưu đúng từ trước phase spin).
- Performance: cpu-spin trong 60s burn 1 core 100% — tốn pin nhưng không crash.

## Pattern

> [!warning] Future loops
> Bất kỳ `while True` nào có thể skip body — luôn thêm sleep guard. Nguyên tắc: nếu loop không làm I/O hoặc compute → phải có throttle.

## Backlinks

- [[technical/training-pipeline]]

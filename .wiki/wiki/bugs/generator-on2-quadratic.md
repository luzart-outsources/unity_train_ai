---
title: Generator O(N²) ở per-intent count
category: bugs
tags: [phase-a, performance, generator, dataset]
sources: []
created: 2026-05-07
updated: 2026-05-07
---

# Generator O(N²) ở per-intent count

`generate_dataset_v3.py` có vòng while với điều kiện `len([r for r in rows if r["intent"] == intent])` — scan toàn bộ `rows` mỗi iteration để đếm. Quadratic theo `args.per_intent`. Tại scale máy 1 (5000/intent) chỉ chậm vừa, tại scale máy 2 HEAVY (25000/intent) thành blocker.

## Symptoms

- Máy 2 launch loop, log dừng ở `[A] iter — seed=5000 arch=lstm HEAVY target=25000/intent (200k total)` quá 7 phút
- Worker python process dùng 100-120MB RAM, CPU 100%, working set tăng tuyến tính
- Không có CSV `intents_v3_m2.csv` được ghi
- Sẽ hit timeout `PHASE_A_TRAIN_TIMEOUT=2400`s ở `phase_a_iter` (thực ra timeout của subprocess generate là 600s, sẽ TIMEOUT trước)

## Repro

```bash
cd AI_Training/phase_a_sentis
.venv/Scripts/python scripts/generate_dataset_v3.py --seed 5000 --per_intent 25000 --out data/intents_v3_m2.csv
# Trước fix: chạy 30+ phút (chưa đo chính xác, kill ở 7 min)
# Sau fix: 3 giây — 199,999 rows, vocab 4070 tokens
```

## Root cause

`scripts/generate_dataset_v3.py:746` (trước fix):

```python
for intent, templates in TEMPLATES.items():
    seen = set(r["text"] for r in rows if r["intent"] == intent)
    attempts = 0
    target_attempts = args.per_intent * 5
    while (len([r for r in rows if r["intent"] == intent]) < args.per_intent
           and attempts < target_attempts):
```

Mỗi iteration của while loop:
1. Build list comprehension `[r for r in rows if r["intent"] == intent]` — **O(rows)**
2. Lấy `len()` của list đó — O(1) sau khi đã build

Trong khi `seen` (set per-intent) đã có sẵn để dedup ở line 755, nó CŨNG đại diện cho count chính xác — nhưng không được dùng làm điều kiện thoát.

**Phân tích complexity** với target `K = args.per_intent`:
- Mỗi intent loop: tổng (1+2+...+K) ≈ O(K²) lần check
- 8 intents → O(8 × K²) = O(K²) tổng
- K=5000: 8 × 25M = 2×10⁸ ops (~30s ở Python)
- K=25000: 8 × 625M = **5×10⁹ ops** (~30+ min)
- Quadratic blow-up: 5× target → 25× thời gian

## Fix

Dùng counter `kept` O(1):

```python
for intent, templates in TEMPLATES.items():
    seen = set(r["text"] for r in rows if r["intent"] == intent)
    kept = len(seen)  # O(1) running count for this intent
    attempts = 0
    target_attempts = args.per_intent * 5
    while kept < args.per_intent and attempts < target_attempts:
        ...
        seen.add(text)
        rows.append({"text": text, "intent": intent})
        kept += 1
```

Tại sao `len(seen)` đúng nghĩa: `seen` được seed ở line 743 với pre-existing rows của intent này, và mỗi `seen.add()` đi cặp với `rows.append()`. Số phần tử của `seen` = số rows thuộc intent hiện tại.

## Verify

```bash
.venv/Scripts/python scripts/generate_dataset_v3.py --seed 5000 --per_intent 25000 --out data/intents_v3_m2.csv
# real    0m2.997s
# [gen v3] wrote 199999 rows to data\intents_v3_m2.csv
#         HOI_LICH        25000
#         HOI_GIO_AN      25000
#         ...
```

200k samples trong 3 giây trên máy 2 (CPU). Tỷ lệ thành công ~99.9995% (1/200000 dedup loss ở OUT_OF_SCOPE).

## Affected

- **Máy 2 (HEAVY) — blocker** trước fix, không thể chạy iter nào
- **Máy 1 (overnight_loop.py)** — không bị block ở 5000/intent (~30s acceptable), nhưng vẫn được hưởng fix sau git pull. Tổng thời gian Phase A của máy 1 sẽ giảm vài chục giây/iter.

## Lessons

- **Linter không bắt** — Python list comprehension trong while condition là valid, nhưng đắt
- **Code review nên flag**: bất kỳ `len([... for ... if ...])` trong vòng lặp đều là red flag
- Khi scale data 5×, complexity quadratic biến từ "chậm vừa phải" thành "không thể dùng được"
- Generator có sẵn `seen` set perfect cho counter, không cần re-scan list

## Backlinks

- [[live-status]] — báo bug đã fix
- [[systems/sentis-chat]] — generator là input cho Phase A
- [[decisions/two-machine-parallel]] — máy 2 HEAVY phụ thuộc fix này
- [[technical/data-generation]] — generator pipeline chi tiết

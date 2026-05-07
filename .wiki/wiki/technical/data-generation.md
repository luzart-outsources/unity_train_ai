---
title: Vietnamese Dataset Generator (v3)
category: technical
tags: [data-augmentation, vietnamese, nlp, templates]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# Vietnamese Dataset Generator — generate_dataset_v3.py

Generator template-based + augmentation cho data Phase A. Mục tiêu: vocab phong phú (>2000 tokens), data đa dạng đủ để LSTM/Transformer generalize trên speech thực.

## Pipeline

```
1. Word pools: TIMES (60+), MEALS (22), PLACES (50+), KNOWLEDGE (60+),
                REPORT_OBJ (25+), REASONS (45+), PRE_GREETINGS (30),
                POST_PARTICLES (~25), CONNECTORS (10), OOC (60+)
2. Templates per intent: 50+ patterns mỗi intent (8 intent × ~50 = 400+)
3. fill(template) → string với placeholder thay từ pool
4. decorate(text) → prepend greeting + append particle
5. maybe_compound(text) → 10% chance link 2 templates với connector
6. augment(text) → 0-2 augmentation: drop_accent / telex_typo /
                    char_swap / drop_filler / random_typo
7. dedup → write CSV
```

## Word pools — 6× lớn hơn v2

Examples:

```python
TIMES = ["hôm nay", "hôm qua", "ngày mai", "tuần sau", "thứ 2", ..., "5 giờ sáng",
         "7 giờ tối", ..., "lát nữa", "tý nữa", "khi nãy", "ban nãy", ...]   # 60+
PLACES = [..., "phòng thí nghiệm", "phòng máy tính", "căng tin", "khu B5",
          "phòng 305", "tòa B5", "trung tâm thể thao", "bãi gửi xe", ...]    # 50+
KNOWLEDGE = ["súng AK", "súng K54", "súng RPG", "lựu đạn M67", ...,
             "AES", "DES", "RSA", "ECC", "SHA-256", "MD5", "TLS",
             "tường lửa", "an toàn thông tin", ...]   # 60+ — quân đội + KTMM
```

Đặc biệt KTMM-specific: `mật mã đối xứng`, `chữ ký số`, `tường lửa`, `IDS/IPS`...

## Templates — 50+ per intent (compound forms)

Examples cho `HOI_LICH`:
```
"{time} có lịch gì",
"lịch {time} thế nào",
"{time} đại đội có lịch gì",
"thời khoá biểu {time}",
"chiều nay đại đội ta bố trí gì không",
...   # 50 templates total
```

Compound: 10% chance generate 2-template với CONNECTOR:
```
"hôm nay có lịch gì với lại tuần sau bố trí gì không"
```

## Augmentations

| Type | Probability | Effect |
|---|---|---|
| `drop_accent` | 7% | "Thủ trưởng" → "Thu truong" |
| `telex_typo` | 5% | "ô" → "oo", "đ" → "dd" — common keyboard miss |
| `random_typo` | 4% | drop 1 random char |
| `char_swap` | 3% | swap adjacent chars |
| `drop_filler` | 6% | drop "thì", "là", "vậy"... |
| `cap_first` | 30% | capitalize first letter |

> [!info] Conservative augmentation
> Không nên augment quá tay. Mục tiêu data trông giống cách user thật gõ — không phải garbage. Tổng probability augment ~25% mỗi sample, 75% giữ nguyên đẹp.

## Pre-greeting + post-particle

Tăng diversity bằng cách stochastic prefix/suffix:
```
"thưa thủ trưởng " + "{time} có lịch gì" + " ạ"
"em hỏi với "    + "{time} có lịch gì" + " nhé"
""               + "{time} có lịch gì" + ""        # bias to no decoration
```

Bias toward empty (4 entries `""` đầu pool) để không phải mọi câu cũng có "thưa thủ trưởng".

## OOD coverage cho OUT_OF_SCOPE

Pool `OOC` chứa real student-life topics:
- Weather, sports, food, mood
- Tech slang (ChatGPT, Github Copilot, VS Code, Linux Ubuntu)
- KTMM-specific (mật mã, an toàn thông tin small talk)
- Class talk ("đề thi học kỳ ra dễ", "thầy môn toán")

Mục tiêu: model học được "đây là small talk, không phải command".

## Stats

| Metric | v3 (per_intent=2000) | v3.1 (per_intent=5000) |
|---|---|---|
| Total samples | 16,000 | 40,000 |
| Vocab unique tokens | 1,975 | ~3,000 (est) |
| Generation time | ~40 sec | ~2 min |
| Train time (cached) | 64 sec FastText | ~3 min FastText, ~6 min LSTM |

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[technical/training-pipeline]]
- [[decisions/eval-set-must-be-real]]

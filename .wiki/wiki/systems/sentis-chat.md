---
title: Sentis Chat (Phase A)
category: systems
tags: [phase-a, sentis, intent-classification, vietnamese, nlp, onnx]
sources: [raw/gdd/context_v2.md, raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# Sentis Chat — Phase A NPC Intent Classifier

NPC sĩ quan chỉ huy hiểu câu hỏi tiếng Việt của người chơi, phân loại thành 1 trong 8 intent, rồi tra response template. Toàn bộ inference offline, chạy local trong Unity 6 qua [[technical/unity-integration|InferenceEngine 2.6]].

## Pipeline

```
User text "Mấy giờ ăn cơm?"
    ↓ tokenize (whitespace lowercase, port underthesea-ish)
    ↓ vocab lookup → int64 ids[max_len=32]
    ↓ ONNX inference (LSTM 116K params)
    ↓ softmax → argmax → intent="HOI_GIO_AN"
    ↓ random pick từ responses["HOI_GIO_AN"]
    ↓ substitute {meal_time}, {place}... từ game state
NPC nói: "Sáng 6h, trưa 11h30, tối 18h..."
```

## 8 intent (chưa Quyền duyệt chính thức, đang tạm)

| Intent | Mô tả | Ví dụ |
|---|---|---|
| `HOI_LICH` | hỏi lịch / thời khoá biểu | "Hôm nay có lịch gì?" |
| `HOI_GIO_AN` | hỏi giờ ăn cơm | "Mấy giờ thì ăn?" |
| `HOI_VI_TRI` | hỏi địa điểm | "Nhà ăn ở đâu?" |
| `HOI_KIEN_THUC` | hỏi kiến thức quân đội / KTMM | "Súng AK dùng thế nào?" |
| `BAO_CAO` | báo cáo lên cấp trên | "Báo cáo đại đội đủ quân" |
| `XIN_PHEP` | xin nghỉ / xin phép | "Cho em xin nghỉ" |
| `TAM_BIET` | chào kết thúc | "Chào thủ trưởng em đi đây" |
| `OUT_OF_SCOPE` | small talk fallback | "Wifi yếu quá" |

## Architecture comparison

3 arch đều train trên cùng dataset, eval trên cùng test 64 câu khó. Chi tiết: [[technical/architecture-comparison]].

| Arch | Params | val_acc | 64-test acc | Verdict |
|---|---|---|---|---|
| FastText | 51K | 99% | **25%** | underfit, ý nghĩa rỗng |
| **LSTM** ⭐ | 116K | 96% | **98.4%** | **canonical winner** |
| Transformer | 120K | 95% | 18.8% | confused, cần epochs nhiều hơn |

> [!warning] Bài học
> Test set 16 câu cũ (chỉ keyword đơn giản) cho cả 3 arch đều 100% — misleading. Phải mở rộng test lên 64 câu (slang, telex typo, compound) mới phân biệt được architecture nào thật sự generalize. Xem [[bugs/fasttext-overfit-narrow-test]].

## Data evolution

| Version | Samples | Vocab | Real-world acc |
|---|---|---|---|
| v1 (template nghèo) | 529 | 165 | 1/8 = 12.5% |
| v2 (richer template) | 2000 | 769 | 16/16 = 100% (test cũ) |
| v3 (massive pool) | 16k | 1975 | 16/16 = 100% (test cũ) |
| **v3.1 (40k)** | 40k | ~3000 (est.) | **63/64 = 98.4%** (test mới) |

Generator chi tiết: [[technical/data-generation]].

## ONNX contract (Quyền cần biết để load)

```
Model file        : deliverables/intent_classifier.onnx (legacy: fasttext_intent.onnx)
Meta file         : deliverables/intent_classifier_meta.json
Input  "input_ids" : int64 tensor shape [batch=1, seq_len=32]
Output "logits"    : float tensor shape [batch=1, num_classes=8]
Tokenization      : lowercase + whitespace split + vocab.json lookup → max_len=32 + pad with id=0
Confidence policy : softmax → argmax. Nếu top prob < 0.40 → fallback OUT_OF_SCOPE
```

C# wrapper code mẫu: [[technical/unity-integration]] hoặc file `deliverables/NPCDialogueBrain.cs`.

## Response templates

`deliverables/responses.json` chứa **4 câu mẫu cho mỗi intent**, có placeholder runtime: `{scheduled_today}`, `{meal_time}`, `{place}`, `{topic}`, ... Quyền tự implement `IRuntimeContext` trong Unity để fill từ game state (lịch hôm nay, vị trí gần nhất, ...).

> [!info] 2-tầng kiến trúc
> Model CHỈ phân loại intent. KHÔNG biết "lịch hôm nay là gì" — đó là việc của tầng 2 ở Unity (Quyền lo). Nếu nói chuyện 1 chiều với chỉ huy mà NPC trả lời "Hôm nay đại đội ta có {scheduled_today}" tức là `IRuntimeContext.Get("scheduled_today")` trả null → wrapper điền `[scheduled_today]`. Phải implement context để hết bug.

## Backlinks

- [[overview]]
- [[entities/commander-npc]] — consumer
- [[technical/training-pipeline]] — quy trình huấn luyện
- [[technical/unity-integration]] — cách load ONNX
- [[technical/architecture-comparison]] — so sánh 3 arch
- [[technical/data-generation]] — generator v3
- [[decisions/lstm-canonical-not-fasttext]]
- [[decisions/eval-set-must-be-real]]

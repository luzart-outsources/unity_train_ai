---
title: Sentis Chat (Phase A)
category: systems
tags: [phase-a, sentis, intent-classification, vietnamese, nlp, onnx]
sources: [raw/gdd/context_v2.md, raw/technical/handoff_morning.md, raw/technical/phase_a_v2_report.md]
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
| **LSTM** ⭐ | 116K | 96% | **98.4%** | **canonical winner cho v1** |
| Transformer | 120K | 95% | 18.8% | confused, cần epochs nhiều hơn |

> [!warning] Test 64 câu là synthetic-friendly — không expose generalization gap
> Test set cũ 64 câu cho LSTM 98.4% nhưng khi build hard test 216 câu (10
> category gồm paraphrase, ellipsis, compound, no-accent, telex, code-mix,
> slang, synonym, complaint, adversarial) thì LSTM v1 chỉ đạt **38%**. Phải
> rebuild test set cho mọi cải thiện sau v1. Xem [[decisions/phase-a-v2-iteration]].

## Data + model evolution (deployed = v5 = 96.8% hard)

| Iter | Dataset | Vocab | Augment | Hard 216 | Notes |
|------|--------:|------:|--------:|---------:|-------|
| v1 (LSTM v3) | 40k | 5k | ~10% | **38.0%** | deployed pre-2026-05-07 |
| v4 (iter 1) | 240k | 10k | ~32% | **92.6%** (+54.6) | 200-300 tpl/intent, paraphrase |
| **v5 (iter 2 — deployed v2)** | 240k | 10k | ~45% | **96.8%** (+4.2) | gap-fill v4 misses |
| v6 (rejected) | 240k | 10k | ~50% | 96.3% (-0.5) | overfit thêm template gây regression |

Cả v4 và v5 đều cùng arch LSTM (707K params, max_len=40, embed=64, hidden=64
bidirectional). Lever cải thiện chính là **dataset diversity + augmentation
rate**, không phải arch. Generator chi tiết: [[technical/data-generation]].
Lý do iteration: [[decisions/phase-a-v2-iteration]].

## v2 stack — 2 model + slot extractor co-deployed

User trải nghiệm "AI ngu" có 2 root cause khác nhau:

1. **Intent classifier yếu trên paraphrase** → fix bằng v5 model (96.8%)
2. **Response template không nhận biết entity user vừa hỏi** → fix bằng
   [[systems/entity-extractor]]

Cả 2 layer chạy song song trong v2. Đường compare trực tiếp với v1:
**Unity → menu AI → 4. Phase A — Compare V1 (Old) vs V2 (New)**.

| | V1 (cũ) | V2 (mới) |
|---|---|---|
| Model | `intent_classifier.onnx` (LSTM v3, 116K params) | `intent_classifier_v2.onnx` (LSTM v5, 707K) |
| Meta | `intent_classifier_meta.json` (max_len=32, vocab 5k) | `intent_classifier_v2_meta.json` (max_len=40, vocab 10k) |
| Responses | `responses.json` (4 template/intent) | `responses_v2.json` (5-7 template, slot-aware) |
| Slot extraction | ❌ | ✅ `slot_vocab.json` + `EntityExtractor.cs` |
| RuntimeContext | `DummyContext` (hardcoded) | `SmartRuntimeContext` (extracted → fallback) |
| Hard test 216 | 38.0% | 96.8% |

## Tokenization — multi-word match (Vietnamese-specific)

Vocab có entries multi-word chứa space (do Python `underthesea` segment):
- `"ăn cơm"` (1 token, có space)
- `"thủ trưởng"` (1 token)
- `"báo cáo"` (1 token)
- `"điều lệnh"`, `"chỉ huy"`, etc.

C# wrapper PHẢI greedy longest-match, không split whitespace đơn giản:

```csharp
// Tại mỗi vị trí từ, thử ghép N..1 word liên tiếp
for (int span = maxMultiWordLen; span >= 1; span--) {
    string candidate = string.Join(" ", words, wi, span);
    if (vocab.TryGetValue(candidate, out int id)) {
        // Match longest! Consume span words.
        wi += span;
        break;
    }
}
```

Bug whitespace-only đã document chi tiết: [[bugs/unity-integration-bugs]] bug 4.

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
- [[systems/entity-extractor]] — v2 slot-filling layer (depends on)
- [[technical/training-pipeline]] — quy trình huấn luyện
- [[technical/unity-integration]] — cách load ONNX
- [[technical/architecture-comparison]] — so sánh 3 arch
- [[technical/data-generation]] — generator v4/v5
- [[decisions/lstm-canonical-not-fasttext]]
- [[decisions/eval-set-must-be-real]]
- [[decisions/phase-a-v2-iteration]] — eval-driven iteration v3→v4→v5
- [[sources/phase-a-v2-report]]

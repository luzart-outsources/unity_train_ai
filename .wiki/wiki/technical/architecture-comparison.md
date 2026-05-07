---
title: Architecture Comparison (FastText vs LSTM vs Transformer)
category: technical
tags: [architecture, comparison, intent-classification, eval]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# Architecture Comparison — 3 archs trên cùng dataset

3 kiến trúc nhỏ (50K – 120K params) huấn luyện trên cùng dataset v3.1 (40k samples) và eval trên cùng test set 64 câu khó. Mục tiêu: chọn arch tốt nhất làm canonical deliverable.

## Architecture summary

| Arch | Params | Layers | Pool | Khi nào tốt |
|---|---|---|---|---|
| **FastText** | 51K | Embed → mean → 2 Linear | mean | data sạch, keyword rõ |
| **LSTM** | 116K | Embed → BiLSTM → mean | mean | có thứ tự từ, ngữ cảnh dài |
| **Transformer** | 120K | Embed + pos → 2 layer self-attn → mean | mean | data nhiều, regularize tốt |

Code: `phase_a_sentis/scripts/model.py`.

## Kết quả

### Test set CŨ (16 câu, simple)

| Arch | val_acc | 16-test acc |
|---|---|---|
| FastText | 0.96 | **16/16 = 100%** |
| LSTM | 0.93 | 15/16 = 93.8% |
| Transformer | 0.95 | 14/16 = 87.5% |

Kết luận sai: **"FastText thắng"**. Lý do — test set cũ chỉ chứa câu keyword rõ, mean-pool đủ.

### Test set MỚI (64 câu, slang/telex/compound/OOD)

| Arch | val_acc | 64-test acc | Verdict |
|---|---|---|---|
| FastText | 0.99 | **25%** | underfit, fall through to dominant class |
| **LSTM** | 0.96 | **95.3% → 98.4%** ⭐ | **canonical winner** |
| Transformer | 0.95 | 18.8% | confused, undertrained |

Kết luận đúng: **LSTM thắng**. Xem [[decisions/lstm-canonical-not-fasttext]] và [[bugs/fasttext-overfit-narrow-test]].

## Phân tích

### Tại sao FastText fail trên test khó

FastText = `mean_pool(embedding(tokens))`. Sentence như:
```
"Có gì ăn không em đói"   (HOI_GIO_AN)
```
embedding trung bình bao gồm "có", "gì", "không" — phổ biến across intent. Không có signal mạnh từ "ăn" + "đói" kết hợp. Model fall through → predict thành intent khác (TAM_BIET vì "không" cuối câu).

LSTM giữ thứ tự → biết "ăn" và "đói" gần nhau → emit signal HOI_GIO_AN mạnh hơn.

### Tại sao Transformer cũng fail

Suprising. Lý do giả thuyết:
1. **Param underutilized**: 120K params chia cho 2 layer × 4 head × 64-dim ≈ tiny. Self-attention cần signal đủ mạnh.
2. **Padding mask**: Transformer encoder dùng `src_key_padding_mask`. Nếu input toàn pad → divide by zero ở pooling. Có warning trong export ONNX. Không hỏng inference thực, nhưng có thể signal training noisy.
3. **Epochs ít**: 40 epochs cho Transformer trên 40k samples có thể chưa đủ. Cần 60-80 epochs.

> [!info] Future work
> Có thể bump Transformer lên 256-dim, 4 layer, 80 epochs để xem có break được LSTM không. Hiện tại LSTM đủ tốt.

## Selection logic in loop v3.1

`overnight_loop.py` cycle qua 3 archs, mỗi iter pick arch theo `arch_idx % 3`. Sau eval, copy ONNX winning arch (highest 64-test acc) sang `intent_classifier.onnx` (canonical). LSTM thắng → deliverable luôn là LSTM dù iter nào.

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[decisions/lstm-canonical-not-fasttext]]
- [[bugs/fasttext-overfit-narrow-test]]

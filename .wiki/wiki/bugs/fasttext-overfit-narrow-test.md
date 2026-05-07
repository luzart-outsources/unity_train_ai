---
title: FastText "100%" lừa dối trên test 16 câu
category: bugs
tags: [phase-a, fasttext, eval, methodology]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# FastText "100%" lừa dối trên test 16 câu

## Symptoms

Sáng 7/5, sau v3 baseline:
- FastText (51K params) báo cáo: 16/16 = 100% trên real-world test
- LSTM (116K) báo cáo: 15/16 = 93.8%
- Transformer (120K): 14/16 = 87.5%

→ kết luận sai: **"FastText là model tốt nhất, nhỏ nhất + giỏi nhất"**.

## Repro

```bash
.venv/Scripts/python.exe scripts/eval_realworld.py --archs fasttext lstm transformer
```

Khi test set chỉ chứa 16 câu keyword rõ ("Hôm nay có lịch gì", "Mấy giờ ăn cơm"...) → FastText mean-pool đủ tín hiệu.

Khi mở rộng test lên 64 câu (slang, telex typo, compound, OOD):
- FastText: **25%** ‼️
- LSTM: **95.3%** ⭐
- Transformer: **18.8%**

## Root cause

**FastText = `mean_pool(embedding(tokens))`**. Câu phức tạp như:
```
"Có gì ăn không em đói"  (HOI_GIO_AN)
```
Embeddings của "có", "gì", "không" — phổ biến nhiều intent. Sau mean-pool, signal "ăn" + "đói" bị làm loãng. Model fall through to dominant class trong vocab phân bố (TAM_BIET vì "không" cuối câu).

LSTM giữ thứ tự + bidirectional state → biết "ăn" và "đói" gần nhau → emit signal HOI_GIO_AN mạnh.

## Fix

1. Mở rộng test set 16 → 64 câu khó. Code: `phase_a_sentis/scripts/eval_realworld.py::TEST_CASES`
2. Đổi canonical từ FastText sang LSTM. Quyết định: [[decisions/lstm-canonical-not-fasttext]]
3. Document trade-off này trong [[technical/architecture-comparison]]
4. Loop v3.1 cycle 3 archs, pick winner per iter (auto-fallback nếu LSTM dữ liệu mới regress)

## Affected

- HANDOFF.md sáng — đã claim "FastText canonical" sai. Cần update.
- `deliverables/fasttext_intent.onnx` — file name vẫn giữ legacy, nội dung giờ là LSTM (~217KB thay vì ~52KB). Quyền không tráo file thì OK.
- Các bài học cho ĐATN: KHÔNG kết luận arch winner trên synthetic test set.

## Pattern

Khi val_acc trên synthetic data đạt ~100%, đó là dấu hiệu **test set quá dễ**, không phải **model giỏi**. Generalization cần test set đủ khó để có spread.

## Backlinks

- [[systems/sentis-chat]]
- [[technical/architecture-comparison]]
- [[decisions/lstm-canonical-not-fasttext]]
- [[decisions/eval-set-must-be-real]]

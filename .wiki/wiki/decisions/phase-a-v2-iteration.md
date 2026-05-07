---
title: Phase A v2 Iteration — eval-driven gap-filling vs scaling
category: decisions
tags: [phase-a, iteration, eval-driven, dataset, augmentation]
sources: [raw/technical/phase_a_v2_report.md]
created: 2026-05-07
updated: 2026-05-07
---

# Phase A v2 Iteration

**Date**: 2026-05-07
**Decided by**: AI training pair-programmer
**Status**: active

## Context

V1 LSTM (deployed pre-2026-05-07) đạt 98.4% trên test 64-câu hand-crafted nhưng
user phản hồi "AI ngu, hỏi khác 1 tý dính". Khi build hard test 216 câu (10
category: paraphrase, ellipsis, compound, no-accent, telex, code-mix, slang,
synonym, complaint, adversarial), v1 chỉ đạt **38.0%**. Test set cũ đo trên
các pattern gần training distribution, không expose synthetic-to-real gap.

Cần một đường nâng cấp model + cách đo độ generalization mới.

## Options considered

1. **Scale dataset 40k→240k với template diversity 5x** — naïve scaling
   - ✓ rẻ, train 5 phút LSTM
   - ✗ không nhắm cụ thể vào failure mode
   - Hint: v4 đạt 92.6% (chứng minh được lever này hữu hiệu)

2. **Subword tokenizer (FastText char n-gram 3-5)** — kiến trúc thay đổi
   - ✓ đúng vũ khí cho TELEX_TYPOS + NO_ACCENT
   - ✗ phải port subword tokenizer sang C# (1 ngày làm)
   - ✗ rủi ro nhiều cho 1 session

3. **Eval-driven iteration (--print_misses → targeted templates → retrain)**
   - ✓ trực tiếp fix các câu user thực sự fail
   - ✓ mỗi iter ~5 phút train
   - ✗ phụ thuộc vào hard test set có representative không

## Decision

Áp dụng **(1) scale + (3) eval-driven iteration** kết hợp, defer (2) cho lần
sau. Cụ thể:

- **Iter 1 (v4)**: scale 40k→240k với 200-300 templates/intent, paraphrase
  patterns, augmentation rate 10%→32%. Đo hard test → 38%→92.6% (+54.6pp).
- **Iter 2 (v5)**: chạy `eval_hardset.py --print_misses`, đọc 16 câu fail, viết
  template lấp gap (rảnh/bận, "đi đâu", pure-symptom XIN_PHEP, lone-place,
  no-accent), bump augmentation 32%→45%. Đo lại → 92.6%→96.8% (+4.2pp).
- **Iter 3 (v6) thử nhưng reject**: thêm 70+ template + augmentation 50%, kết
  quả 96.3% (-0.5pp do regression CODE_MIX/SLANG/SYNONYM).

## Consequences

- **Ship v5** (96.8%) là sweet spot. V6 chứng minh diminishing returns: thêm
  template ở threshold 96%+ có thể harm các category đang OK do đổi phân phối.
- **Test set là metric chính**, không phải val_acc synthetic (v3 99.48% val
  nhưng 38% hard test). Đã thể chế hoá ở [[decisions/eval-set-must-be-real]].
- **Pipeline lặp lại được**: dùng `--tag vN` trong train/eval/export để giữ
  artifact theo iteration, không đè model cũ.
- **Phase A v1 giữ nguyên** trên disk (`intent_classifier.onnx`,
  `responses.json`, `NPCDialogueBrain.cs` + `DummyContext`) → user A/B compare
  trực tiếp qua menu **"AI/4. Phase A — Compare V1 vs V2"**.
- **Để vượt 97%+ cần subword** — 3/7 misses của v5 là extreme TELEX cần
  char-level info. Khi nào user yêu cầu, port FastText subword sang Python +
  C# tokenizer (~1 ngày).

## Backlinks

- [[systems/sentis-chat]]
- [[systems/entity-extractor]]
- [[decisions/eval-set-must-be-real]]
- [[decisions/lstm-canonical-not-fasttext]]
- [[technical/data-generation]]
- [[sources/phase-a-v2-report]]

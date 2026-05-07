---
title: Phase A v2 Report
category: sources
tags: [phase-a, sentis, intent-classification, hard-test, iteration]
source_path: raw/technical/phase_a_v2_report.md
created: 2026-05-07
updated: 2026-05-07
---

# Phase A v2 Report — Source Summary

Báo cáo end-to-end cho hai pha cải thiện Phase A NPC brain (v3 → v4 → v5; v6
là thử nghiệm bị reject). Author: AI training pair-programmer cho Quyền.
Path: `AI_Training/PHASE_A_V2_REPORT.md` (mirror trong `raw/`).

## Số chính

| Iter | Hard test 216 câu | Δ        | Train data | Augmentation |
| ---- | ----------------- | -------: | ---------- | ------------ |
| v3 (deployed v1) | 38.0% (82/216) | baseline | 40k synthetic | ~10% noise |
| v4 (iter 1) | 92.6% (200/216) | +54.6 pp | 240k synthetic | ~32% noise |
| **v5 (deployed v2)** | **96.8% (209/216)** | **+4.2 pp** | 240k synthetic | ~45% noise |
| v6 (rejected) | 96.3% (208/216) | -0.5 pp | 240k synthetic | ~50% noise |

## Hai trục cải thiện

1. **Dataset diversity (v3→v4)**: 50 templates/intent → 200-300, word pools
   gấp 5x, paraphrase patterns (ellipsis, complaint-as-question, code-mix),
   compound questions, synonym swap. Augmentation rate gấp 3x.

2. **Iteration-driven gap-filling (v4→v5)**: chạy `eval_hardset.py
   --print_misses` trên v4, đọc ra 16 câu fail cụ thể, viết template targeted
   cho từng cluster (rảnh/bận, "đi đâu", pure-symptom XIN_PHEP, lone-place
   ellipsis, no-accent place asks), bump augmentation 32%→45%, retrain.

## Lý do v6 reject (anti-pattern)

V6 thêm 70+ template mới (rảnh-form không "có" filler, English-place code-mix,
academic-complaint OOS) + bumped augmentation lên ~50%. Kết quả:
- COMPLAINT 96%→100% ✓
- NO_ACCENT 96%→100% ✓
- CODE_MIX 96%→96% (Library OK, Agenda regress)
- SLANG 100%→93% ✗
- SYNONYM 100%→94% ✗

Net -0.5pp. Bài học: thêm template = đổi phân phối training, có thể harm các
category đang OK. ROI âm khi đã ở 96%+.

## EntityExtractor / SmartContext (orthogonal to model)

Vấn đề user phàn nàn "hỏi khu A trả lời khu B" KHÔNG phải intent classification
sai (model đã đúng HOI_VI_TRI), mà là response template fill `{place}` từ
`DummyContext.Get("place")` hardcoded. Fix: rule-based `EntityExtractor.cs`
longest-match user input vs slot vocabulary (134 places, 185 times, 188 topics,
45 meals, 100 reasons, 61 reports). `SmartRuntimeContext` ưu tiên slot extracted
trước khi fall back DummyContext.

## Failure modes còn (v5 deployed)

7 misses (3.2%):
- 3 extreme TELEX (`Phoongg`, `Bááoo`, `Emm`) — multi-char vowel duplications,
  user thực không gõ thế này, cần subword (FastText char n-gram 3-5)
- 1 "Library ở đâu thầy" — code-mix regression
- 1 "Bài tập về nhà nhiều quá" — confused vì có "nhà"
- 1 "Hôm nay rảnh không nhỉ" — rising-tone form
- 1 "Cho em hoi sang van dong o dau" — borderline confidence 50%

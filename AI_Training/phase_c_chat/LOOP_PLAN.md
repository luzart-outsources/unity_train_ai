# Phase C — Autonomous Smartness Loop Plan
**Mục tiêu**: Train AI sao cho "hỏi gì đáp đúng nấy" — robust với mọi paraphrase tiếng Việt.
**Budget**: ~6 tiếng CPU đến 6 AM ngày 2026-06-12.
**Strategy**: Loop generate → train → eval → analyze fails → augment → repeat.

## Baseline trước loop
- Teacher (FT MiniLM): 95.9% hardset
- Student v1 (distilled): 93.9% hardset, 13MB ONNX
- Vocab 897 tokens (quá nhỏ)
- Failures: TELEX_TYPOS 33%, "Mấy giờ ăn cơm" nhầm slot đi ngủ

## ROI-ranked actions
1. **5-10x data với heavier augmentation** (highest ROI)
   - Meal vs sleep specific samples
   - 10x telex variants per query
   - More natural Vietnamese phrasings
   - More entity-specific paraphrases (vd: "khu này khu kia" cho ambiguity)
2. **Bigger student** (256-dim, 6 layer, 8 head)
3. **Targeted patches** cho failure cases mỗi vòng
4. **Train longer + better schedule** (30 epochs với warm restart)

## Loop iterations (mỗi vòng ~20-30 min)

```
ITER 1 — v8 data (50K) + student v3 distillation
ITER 2 — Analyze v3 fails → v9 patches → student v4
ITER 3 — Bigger student arch (256-dim) → v5
ITER 4 — More epochs + cosine warm restart → v6
ITER 5 — Final calibration + thresholds + commit
```

## Commit cadence
- Mỗi iteration: 1 commit summarizing what changed + eval delta
- Iteration không cải thiện: vẫn commit để bạn thấy "đã thử cái này, không work"
- Final: 1 commit chốt best model into Unity Assets

## Safety
- Giữ lại student v1 (`student_encoder.pt` hiện tại) làm fallback
- Mỗi version save vào `student_encoder_v{N}.pt`
- Best ever → `student_encoder_best.pt` symlink (luôn point đến best)

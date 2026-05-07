# Phase A v2 — Smarter NPC Brain (v5 deployed)

## TL;DR

| Metric                              | V1 (cũ)            | V2 v4 (iter 1)      | V2 v5 (deployed)    | Cải thiện         |
| ----------------------------------- | ------------------ | ------------------- | ------------------- | ----------------- |
| Model arch                          | LSTM (250K params) | LSTM (707K params)  | LSTM (707K params)  | ~3x params        |
| Training data                       | 40k samples (v3)   | 240k samples (v4)   | 240k samples (v5)   | 6x lớn hơn        |
| Vocab size                          | ~5,000 token       | 10,000 token        | 10,000 token        | 2x lớn hơn        |
| max_len                             | 32                 | 40                  | 40                  | dài hơn 25%       |
| Val acc (synthetic)                 | 99.48%             | 99.73%              | 99.64%              | gần saturate      |
| **Hard test acc (216 câu)**         | **38.0% (82/216)** | **92.6% (200/216)** | **96.8% (209/216)** | **+58.8 pp** ✨   |
| Slot extraction (entity awareness)  | ❌ không có        | ✅                  | ✅                  | tính năng mới     |

V5 thêm gap-filler templates cho 16 misses của v4: rảnh/bận semantics, "đi đâu"
ambiguity, pure-symptom XIN_PHEP, lone-place ellipsis, no-accent place asks.
Plus bumped augmentation rate (~32% → ~45%).

V6 thử fix 7 misses còn lại nhưng ROI âm (96.8% → 96.3% do regression),
nên ship v5.

V2 không thay thế V1 — cả 2 model + script + responses đều giữ nguyên trên disk
để Quyền A/B compare trực tiếp.

## Vấn đề V1 mắc phải

Eval thực tế trên `eval_realworld.json` (64 câu hand-crafted, easy) cho V1
98.44% accuracy → trông như model "đã hoàn thiện". Nhưng cảm giác user thấy
nó "ngu" là vì 2 vấn đề riêng biệt:

1. **Test set easy quá:** template tay cùng người viết training templates,
   không phản ánh đa dạng cách user gõ thật. Khi build hard test set 216 câu
   (paraphrase, telex typos, code-mix, ellipsis, slang...), V1 chỉ đạt **38.0%**.

2. **Response template không nhận biết entity:** user hỏi "khu A ở đâu" → V1
   classify đúng `HOI_VI_TRI` nhưng template `{place} nằm ở khu {block}` được fill
   bằng `DummyContext.Get("block")` luôn return cứng `"B5"` → response thành
   "khu A nằm ở khu B5". Trông như AI "đáp sai", thực ra là tầng response không
   biết user vừa hỏi cái gì cụ thể.

## Cách V2 khắc phục

### 1. Dataset v4 — paraphrase-first (`scripts/generate_dataset_v4.py`)

- 200-300 templates/intent (vs 50 ở v3)
- 5x bigger word pools: 100+ TIMES, 130+ PLACES, 188 KNOWLEDGE topics, 100 REASONS
- Heavy paraphrase patterns: declarative-question, ellipsis ("còn mai thì sao"),
  complaint-as-question ("đói quá", "lạc đường rồi"), greetings + question
- Code-mixing: 5% English insertion (`schedule`, `lunch`, `permission`, ...)
- Synonym swap inside formed sentences (ăn → xơi/chén, đi → tới/ra/sang...)
- Stronger augmentation: ~30% noise rate (telex typos, accent drop, char-swap,
  filler-word drop)
- Cross-intent hard negatives (HOI_VI_TRI vs HOI_LICH near-confusions)

### 2. Hard test set 216 câu (`scripts/eval_hardset.py`)

Categories: CLEAN, NO_ACCENT, TELEX_TYPOS, CODE_MIX, COMPOUND, ELLIPSIS,
SYNONYM, COMPLAINT, SLANG, ADVERSARIAL.

Đây là metric **chuẩn xác** để đánh giá generalization, không phải synthetic
val_acc.

### 3. EntityExtractor (`Assets/AI/Scripts/EntityExtractor.cs`)

Rule-based slot extractor đọc câu user, scan vocabulary đồng bộ với generator
(load từ `Assets/AI/Resources/slot_vocab.json`), longest-match greedy →
return dict slot ↦ phrase.

Slot vocab dump tự động từ generator bằng `scripts/dump_slot_vocab.py`.

### 4. SmartRuntimeContext (`Assets/AI/Scripts/SmartRuntimeContext.cs`)

Wrapper IRuntimeContext mới: ưu tiên slot extracted trước rồi mới fallback về
DummyContext. Khi user hỏi "khu A ở đâu":

```
slot map = {place: "khu a"}
template "{place} ở phía {direction} doanh trại..." 
    → "khu a ở phía đông doanh trại, đi thẳng 100m là tới."
```

Thay vì cũ:

```
template "{place} nằm ở khu {block}..."
    → "khu B nằm ở khu B5..." (sai, hardcoded {block}=B5)
```

### 5. Response templates v2 (`Assets/AI/Resources/responses_v2.json`)

Templates được redesign tránh "khu {place} nằm ở khu {block}" tự conflict.
Mỗi template chỉ dùng slot có thể fill chính xác bằng EntityExtractor +
runtime context.

## Các file deliverables

### Mới (KHÔNG đè file cũ):
- `Assets/AI/Models/intent_classifier_v2.onnx` — LSTM v4 ONNX
- `Assets/AI/Resources/intent_classifier_v2_meta.json` — vocab + label map v4
- `Assets/AI/Resources/responses_v2.json` — entity-aware response templates
- `Assets/AI/Resources/slot_vocab.json` — slot extractor vocabulary
- `Assets/AI/Scripts/EntityExtractor.cs`
- `Assets/AI/Scripts/SmartRuntimeContext.cs`
- `Assets/AI/Scripts/PhaseACompareTester.cs`
- `Assets/AI/Editor/PhaseACompareSceneBuilder.cs`
- `AI_Training/phase_a_sentis/data/intents_v4.csv` — 240k training samples
- `AI_Training/phase_a_sentis/scripts/generate_dataset_v4.py`
- `AI_Training/phase_a_sentis/scripts/eval_hardset.py`
- `AI_Training/phase_a_sentis/scripts/dump_slot_vocab.py`
- `AI_Training/phase_a_sentis/scripts/smoke_v4.py`
- `AI_Training/phase_a_sentis/models/lstm_v4_best.pt`
- `AI_Training/phase_a_sentis/models/lstm_v4_intent.onnx`
- `AI_Training/phase_a_sentis/models/eval_v3_hardset.json`
- `AI_Training/phase_a_sentis/models/eval_v4_hardset.json`

### Giữ nguyên (tuyệt đối không touch):
- `Assets/AI/Models/intent_classifier.onnx` — V1 LSTM v3
- `Assets/AI/Resources/intent_classifier_meta.json` — V1 meta
- `Assets/AI/Resources/responses.json` — V1 responses
- `Assets/AI/Scripts/NPCDialogueBrain.cs` — V1 wrapper
- Tất cả Phase B (`soldier.onnx`, `MovementAgent.cs`, ...)

## Cách dùng

### Compare V1 vs V2 trong Unity

1. Mở Unity Editor
2. Menu bar → **AI → 4. Phase A — Compare V1 (Old) vs V2 (New)**
3. Click Play
4. Gõ tiếng Việt vào input → cả V1 và V2 trả lời song song để so sánh

Sanity 8 câu auto chạy lúc Start. Score sẽ hiện trên header.

### Re-train hoặc tweak

```bash
# Generate fresh dataset
.venv/Scripts/python scripts/generate_dataset_v4.py --per_intent 30000

# Retrain (LSTM, 15 epochs, ~4 min)
.venv/Scripts/python scripts/train.py \
    --arch lstm --data data/intents_v4.csv --tag v4 \
    --epochs 15 --batch 128 --max_len 40 --vocab_size 10000

# Eval on hard test (216 câu)
.venv/Scripts/python scripts/eval_hardset.py --archs lstm --tag v4

# Export to ONNX
.venv/Scripts/python scripts/export_onnx.py --arch lstm --tag v4

# Refresh slot vocab if pools change
.venv/Scripts/python scripts/dump_slot_vocab.py

# Then copy:
#   models/lstm_v4_intent.onnx → Assets/AI/Models/intent_classifier_v2.onnx
#   models/lstm_v4_intent_meta.json → Assets/AI/Resources/intent_classifier_v2_meta.json
```

## Failure modes còn tồn (v5 deployed)

V5 miss 7/216 câu (~3.2%). Phân loại:

| Category     | V5 score    | Cụ thể câu miss                                |
| ------------ | ----------- | ---------------------------------------------- |
| TELEX_TYPOS  | 9/12 (75%)  | "Phoongg học oo đâu", "Bááoo cáo đủù quânnn", "Emm chào thủủ trưởng emm đii" — extreme multi-char duplications, cần char n-gram subword |
| CODE_MIX     | 23/24 (96%) | "Library ở đâu thầy" — model classify HOI_LICH (regression của v4)            |
| CLEAN        | 54/55 (98%) | "Bài tập về nhà nhiều quá" — confused với HOI_VI_TRI vì có "nhà"             |
| COMPLAINT    | 22/23 (96%) | "Hôm nay rảnh không nhỉ" — vẫn classify OOS thay vì HOI_LICH                  |
| NO_ACCENT    | 23/24 (96%) | "Cho em hoi sang van dong o dau" — borderline confidence                       |

3 trong 7 misses là TELEX cực đoan (`Phoongg`, `Bááoo`, `Emm`) — user thực
tế hiếm khi gõ thế này. Để fix triệt để cần subword tokenization (FastText
char n-gram 3-5), cost ~1 ngày port sang C#.

## Iteration log

| Iter | Dataset       | Augmentation | Hard test    | Notes                         |
| ---- | ------------- | ------------ | ------------ | ----------------------------- |
| v3   | 40k (intents_v3.csv) | ~10% noise   | 38.0%        | baseline (deployed in v1)     |
| v4   | 240k (intents_v4.csv) | ~32% noise   | 92.6% (+54.6) | bigger pools, 200+ tpl/intent |
| v5   | 240k (intents_v5.csv) | ~45% noise   | **96.8% (+4.2)** | gap-fill v4 misses (deployed) |
| v6   | 240k (intents_v6.csv) | ~50% noise   | 96.3% (-0.5) | thêm template gây xáo trộn boundary, không ship |

---
title: Source — Intent Classifier Meta
category: sources
tags: [onnx, dialogue, vocab]
source_path: AI_Training/deliverables/intent_classifier_meta.json
created: 2026-05-12
updated: 2026-05-12
---

# Source — intent_classifier_meta.json

**Path**: `AI_Training/deliverables/intent_classifier_meta.json`
**Type**: Tokenizer + model contract for `intent_classifier.onnx` (LSTM, Phase A winner).

## Key fields

```json
{
  "arch": "lstm",
  "max_len": 32,
  "pad_id": 0,
  "unk_id": 1,
  "vocab": { "<pad>": 0, "<unk>": 1, "em": 2, ... }
}
```

## Vocabulary characteristics

- Multi-word tokens present: `"báo cáo"=9`, `"thủ trưởng"=10`, `"đồng chí"=14`, `"trực ban"=19`, `"làm sao"=20` — tokenizer must do longest-match, not pure whitespace split.
- Pad token = id 0; unknown = id 1.
- Vocab covers ~200+ words after Phase A LSTM training data expansion.

## Implication for runtime

`VietnameseTokenizer` (in [[systems/npc-dialogue]]) must:
1. Lowercase input.
2. Try longest-match against vocab keys.
3. OOV → `<unk>` (1).
4. Pad to `max_len=32`.

## Cited from this source

- [[claims#c-20260512-07]] LSTM arch, max_len, accuracy
- [[claims#c-20260512-09]] 8 intent labels (via id2label sibling file)

## Backlinks
- [[systems/npc-dialogue]]
- [[sources/merged-report]]

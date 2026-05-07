---
title: Entity Extractor (Phase A v2 slot-filling)
category: systems
tags: [phase-a, slot-filling, ner, runtime-context, vietnamese]
sources: [raw/technical/phase_a_v2_report.md]
created: 2026-05-07
updated: 2026-05-07
---

# Entity Extractor — Phase A v2 Slot Filling

Rule-based slot extractor giải quyết vấn đề **"hỏi khu A trả lời khu B5"** —
không phải intent classification sai (model classify HOI_VI_TRI đúng), mà là
response template fill `{place}` từ `DummyContext.Get("place")` HARDCODED.

## 2-tầng pipeline (v2)

```
User text "Khu A ở đâu"
    ↓ [classify]  NPCDialogueBrain (LSTM v5) → HOI_VI_TRI 100%
    ↓ [extract]   EntityExtractor → {place: "khu a"}
    ↓ [substitute] SmartRuntimeContext.Get("place") → "khu a"
                                  Get("direction") → "đông" (DummyContext fallback)
    ↓ template "{place} ở phía {direction} doanh trại, đi thẳng 100m là tới."
NPC nói: "khu a ở phía đông doanh trại, đi thẳng 100m là tới."
```

So với v1 thì user hỏi "Khu A ở đâu" nhả "khu A nằm ở khu B5..." (sai vì
template `{place} nằm ở khu {block}` fill `{place}=khu A` + `{block}=B5` cứng
→ self-conflict).

## Slot vocabulary

`Assets/AI/Resources/slot_vocab.json` được dump từ Python generator
[[technical/data-generation]] qua `scripts/dump_slot_vocab.py`. Tổng cộng
715 phrases, sorted descending by token count để longest-match work:

| Slot | Count | Examples |
|------|------:|----------|
| place | 136 | "khu A", "khu B5", "phòng 305", "thư viện", "căn tin", "bãi tập", ... |
| time | 185 | "hôm nay", "ngày mai", "tuần sau", "9h sáng", "buổi tối", "lát nữa", ... |
| topic | 188 | "súng AK", "RSA", "AES", "thuật toán Dijkstra", "10 lời thề", ... |
| meal | 45 | "ăn cơm", "ăn trưa", "phát cơm", "cơm chiều", "lĩnh phần", ... |
| reason | 100 | "ốm", "sốt cao", "đau bụng", "việc gia đình", "bố mẹ ốm", ... |
| report | 61 | "đại đội", "trung đội 1", "tiểu đội 3", "ban chỉ huy", ... |

> [!info] Synced với generator
> Khi modify pools (PLACES, TIMES, KNOWLEDGE) trong `generate_dataset_v5.py`,
> **MUST** re-run `dump_slot_vocab.py` để giữ slot_vocab.json đồng bộ.
> Mất sync = model classify đúng nhưng extractor không trích được entity.

## Algorithm — longest-match greedy

```csharp
// Pseudo-code: EntityExtractor.Extract
foreach (slot, phrases) in slot_vocab:
    foreach phrase in phrases:           // pre-sorted longest-first
        if user_text.contains_as_word(phrase):
            result[slot] = phrase
            break                         // first match wins per slot
```

**Word-boundary check**: pad cả `user_text` và `phrase` với spaces (`" khu a "`)
rồi substring match. Tránh false positive như `"khu"` match `"khu xự"`.

## SmartRuntimeContext

Implement `IRuntimeContext`. Order resolution:

```
Get(key) → extracted_slots[key] (nếu có)
       → fallback inner.Get(key) (DummyContext, hardcoded từ game state)
```

Tester gọi `ctx.SetExtractedSlots(extractor.Extract(userText))` TRƯỚC mỗi
`brain.Respond(userText)`. Slot map clear sau mỗi câu — tránh leak entity từ
câu trước.

## Response templates v2

`Assets/AI/Resources/responses_v2.json` được redesign để **không tự conflict**
khi `{place}` đã extract:

```diff
- "{place} nằm ở khu {block}. Có biển chỉ dẫn rồi đó."
+ "Muốn tới {place} thì đi theo hướng {direction} doanh trại, có biển chỉ dẫn."
+ "{place} cách đây không xa, đi {direction} chừng {distance}m là tới."
```

Loại template tránh `{block}` vì nếu `{place}` đã là một block (vd "khu A"),
substitute `{block}=B5` sẽ cho ra "khu A nằm ở khu B5" — vô lý.

## Files & contracts

```
Assets/AI/Resources/slot_vocab.json     <- dumped từ generator
Assets/AI/Resources/responses_v2.json   <- 5-7 templates/intent, slot-aware
Assets/AI/Scripts/EntityExtractor.cs    <- standalone, parse JSON + match
Assets/AI/Scripts/SmartRuntimeContext.cs <- impl IRuntimeContext
Assets/AI/Scripts/PhaseACompareTester.cs <- 2-column UI
Assets/AI/Editor/PhaseACompareSceneBuilder.cs <- AI/4 menu
```

## Limitations

- **Rule-based, không phải NER**: không hiểu phrase ngoài vocab. Vd hỏi "phòng
  1024 ở đâu" → "phòng 1024" không có trong vocab → no slot extracted →
  template fall back DummyContext.
- **No entity disambiguation**: nếu câu chứa nhiều entity cùng slot ("khu A
  và khu C"), chỉ trích entity đầu tiên (longest-match-first).
- **No coreference**: "Cái đó ở đâu" không nhớ "cái đó" là gì từ turn trước.

Cho domain hẹp như NPC quân đội KTMM thì rule-based đủ — vocab pools cover ~95%
câu thực tế. Nếu sau này cần generalize, có thể thay bằng PhoBERT NER nhưng
phải port sang Sentis (hiện chưa hỗ trợ BERT tokenizer trên C#).

## Backlinks

- [[systems/sentis-chat]] — provides intent classifier
- [[entities/commander-npc]] — consumer
- [[decisions/phase-a-v2-iteration]] — context cho v2
- [[technical/data-generation]] — vocab pools
- [[technical/unity-integration]] — load ONNX
- [[sources/phase-a-v2-report]]

# Phase C — ChatGPT-level NPC Chat Upgrade Plan

**Created:** 2026-06-03
**Goal:** Nâng chất lượng NPC chat từ "mode collapse trên A1/A2/A3/B" lên gần ChatGPT-level cho game RPG/farming domain.

---

## 1. Root Cause Diagnosis (xác nhận bằng code reading)

### Bug 1: Slot vocab mismatch with game world
- `slot_vocab.json` chỉ có "phòng học a1", "phòng học a2"... (military academy classrooms)
- **KHÔNG có** các vùng game thật: "khu A1", "khu A2", "khu A3", "khu B", farm, inn, town
- → EntityExtractor không extract được place khi user hỏi về areas

### Bug 2: DummyContext hardcode
- `NPCDialogueBrain.cs:351`: `case "place": return "khu B"` — luôn trả về "khu B"
- Khi slot extraction fail → fallback context → mọi response về vị trí đều nói "khu B"

### Bug 3: Domain mismatch
- Training data v3-v5: 240K câu **military academy** (báo cáo, súng AK, điều lệnh, thủ trưởng)
- Game scenes thật: **Farm, ChickenCoop, Forest, Inn, PlayerHome, Town, TownSquare, YodelRanch** (farming RPG)
- → AI nói tiếng "đại đội trưởng" trong game farm

### Bug 4: Tokenizer asymmetry
- Training: `underthesea.word_tokenize` (Vietnamese morphological segmentation)
- Runtime C#: whitespace + greedy multi-word match
- → cùng câu input ra token IDs khác nhau → confidence drop, edge cases miss

### Bug 5: 8-intent quá thô
- HOI_VI_TRI dồn tất cả câu "X ở đâu?" vào 1 lớp
- Response selection random từ pool của HOI_VI_TRI → không phụ thuộc X cụ thể
- Mode collapse "luôn ra cùng kiểu trả lời" là *by design* trong kiến trúc này

---

## 2. Architecture Decision

### Chosen: **Hybrid Embedding Retrieval + Slot Fill + LLM Fallback**

```
User input (Vietnamese)
    │
    ├──► Slot Extractor (rule-based, regex + dict)
    │       └─► {place: "khu A1", time: ..., topic: ...}
    │
    ├──► Sentence Embedding (multilingual-MiniLM-L12-v2 ONNX, ~118MB)
    │       └─► 384-dim vector
    │
    ├──► Cosine retrieval over Q&A bank (~50K paraphrases × 384 dims)
    │       └─► top-1 + score
    │
    ├── score > 0.75 ──► Template fill with extracted slots + game state ──► Reply
    │
    └── score < 0.75 OR multi-turn OR complex ──► Groq LLM API (already integrated)
                                                  with system prompt + game context ──► Reply
```

### Why this design:
1. **Embedding retrieval không bao giờ collapse** — distance metric, không phải argmax softmax
2. **Multilingual MiniLM pre-trained trên 50+ ngôn ngữ** (gồm Vietnamese) bằng 1B+ sentence pairs — đã hiểu ngữ nghĩa Vietnamese không cần fine-tune
3. **Offline 90%** queries qua retrieval — fast, free, no internet
4. **Online 10%** hard queries qua Groq — đã có sẵn integration, free tier đủ
5. **Game state aware** — Slot extractor + RuntimeContext biết player ở đâu, NPC nào, giờ nào
6. **CPU friendly** — MiniLM ONNX inference ~5ms trên CPU, không cần GPU runtime

### Tier so sánh:
| Approach | Quality | Size | Offline | GPU train? |
|---|---|---|---|---|
| Current LSTM 8-class | ~40% out-of-domain | 3MB | Yes | No |
| **Embedding retrieval (this plan)** | **~80%** | **120MB** | **Yes** | **No** |
| Fine-tuned Qwen 0.5B | ~85% | 500MB | Yes | Yes (need) |
| Embedding + Groq fallback (this plan) | **~92%** | 120MB | Hybrid | No |

---

## 3. Execution Phases

### Phase C.1: Game World Extraction (data prep, 1-2h)
- Quét toàn bộ `Assets/Scenes/*.unity` (YAML) → extract GameObject names có ý nghĩa địa lý
- Quét `Assets/Scripts/TrainAI/SO/*.asset` → entities, items, NPCs
- Xuất `AI_Training/phase_c_chat/data/game_entities.json`:
  ```json
  {
    "areas": ["khu A1", "khu A2", "khu A3", "khu B", "farm", "town", ...],
    "npcs": ["John", "Mary", "Đại đội trưởng", ...],
    "items": ["seed", "carrot", "axe", ...],
    "scenes": ["Farm", "Town", "Inn", ...],
    "interactables": ["bed", "calendar", "tv", "chest", ...]
  }
  ```

### Phase C.2: New Intent Taxonomy (1h)
Mở rộng từ 8 → 20+ intents phù hợp game RPG:
```
ASK_LOCATION    # "X ở đâu" — slot: place
ASK_DIRECTION   # "đi đâu để tới X" — slot: place
ASK_TIME        # "mấy giờ" — slot: event
ASK_SCHEDULE    # "hôm nay làm gì" — slot: date
ASK_NPC         # "John là ai" — slot: npc
ASK_QUEST       # "quest này làm sao" — slot: quest
ASK_ITEM        # "axe ở đâu mua" — slot: item
ASK_HOWTO       # "trồng cây như nào" — slot: action
ASK_PRICE       # "X giá bao nhiêu" — slot: item
ASK_WEATHER     # "hôm nay trời như nào"
GREETING        # "xin chào" "hi"
GOODBYE         # "bye" "tạm biệt"
THANKS          # "cảm ơn"
COMPLIMENT      # "đẹp quá" "ngon thật"
COMPLAINT       # "khó quá" "buồn"
CONFIRM         # "ok" "đúng" "vâng"
DENY            # "không" "thôi"
SMALL_TALK      # general chat
HELP            # "giúp với" "làm sao bây giờ"
OUT_OF_SCOPE    # fallback
```

### Phase C.3: Massive Dataset Generation (3-4h)
- Script `generate_dataset_v7.py`:
  - Mỗi intent × mỗi entity × N paraphrase patterns = ~50K-100K samples mỗi intent
  - Vietnamese paraphrase patterns:
    - Formal: "Cho hỏi X ở đâu ạ"
    - Casual: "X ở đâu nhỉ"
    - Truncated: "X đâu?"
    - With filler: "À cho hỏi cái X ở đâu"
    - Code-mixed: "Where is X"
    - Telex typos: "X owr ddaau"
    - No accent: "X o dau"
    - Slang/internet: "X ở dau v"
    - With politeness: "Cho em hỏi X chỗ nào ạ"
    - Multi-turn followup: "Còn X thì sao"
  - **Per (intent, slot value)** pairs (chứ không phải per intent) → đảm bảo có example riêng cho "khu A1" vs "khu A2" vs "khu B"
- Target: ~1M samples cho 20 intents

### Phase C.4: Embedding Model Pipeline (2-3h)
- Download `paraphrase-multilingual-MiniLM-L12-v2` via `sentence-transformers`
- Test inference với data
- Export to ONNX (opset 14, dynamic batch)
- Verify output dim 384, max_seq_len 128
- Build embedding index:
  - Embed all 1M samples → save as `.npy` (~1.5GB float32, có thể quantize float16 → 750MB)
  - Or embed only ~50K canonical Q&A pairs (1 representative per (intent, slot)) → 75MB

### Phase C.5: C# Retrieval Runtime (3-4h)
- New script `Assets/AI/Scripts/EmbeddingChatBrain.cs`:
  - Load MiniLM ONNX
  - Tokenize input (SentencePiece BPE) — need C# tokenizer port
  - Forward pass → 384-dim vector
  - Cosine vs index (load `embeddings.bin` + `qa_metadata.json`)
  - Top-1 → response template / canonical answer
- New script `Assets/AI/Scripts/HybridChatOrchestrator.cs`:
  - Tries EmbeddingChatBrain first
  - If score < threshold → falls back to LLMNetworkManager (Groq)
  - Provides game state context to both

### Phase C.6: Game State Context (2h)
- New `Assets/AI/Scripts/GameStateContext.cs : IRuntimeContext`
  - Reads player current scene → "current_area"
  - Reads time-of-day → "current_time"
  - Reads quest log → "active_quest"
  - Reads NPC state → "npc_status"
- Replaces DummyContext in PhaseAChatUI

### Phase C.7: Eval Suite (1-2h)
- `eval_c_realgame.py`: 500+ test cases focused on:
  - Specific area queries: "khu A1 ở đâu", "khu A2 ở đâu", "khu A3 ở đâu", "khu B ở đâu" → EXPECT different responses
  - NPC names, items, quests
  - Multi-turn followups
  - Slang, typos, code-mix
- Report per-intent, per-slot-value accuracy

### Phase C.8: Polish & Commit (1h)
- Documentation
- Integration test in Unity Editor
- Git commit with detailed message

---

## 4. Constraints & Risks

### Constraints
- **CPU only** trên máy này (torch 2.5.1+cpu, no nvidia-smi)
- **Unity Inference Engine 2.6.1** (legacy Sentis API, namespace `Unity.InferenceEngine`)
- **Tokenizer port**: SentencePiece BPE phải port sang C# hoặc dùng simpler tokenizer
- **Disk**: dataset 1M samples × ~50 chars × Vietnamese UTF-8 = ~150MB; embeddings 1M × 384 × 4 = 1.5GB (large)

### Risks
1. **MiniLM SentencePiece tokenizer trong C#**: solution = port BPE algorithm hoặc dùng wordpiece-only model
2. **ONNX opset compatibility với Unity Inference Engine 2.6.1**: test trước, có thể cần opset 14 not 17
3. **Embedding model size 120MB** trên mobile có thể to: solution = quantize INT8 (~30MB)
4. **Groq API key in repo** (LLMNetworkManager.cs:15): security issue tách-rời, sẽ flag riêng

### Time estimate (sequential, CPU-only)
- Phase C.1-C.2: ~3h (data + intent design)
- Phase C.3: ~4h (dataset generation script + run)
- Phase C.4: ~3h (embedding pipeline)
- Phase C.5: ~4h (C# retrieval runtime — tokenizer port là phần khó nhất)
- Phase C.6: ~2h (game state)
- Phase C.7-C.8: ~3h (eval + commit)
- **Total: ~20 hours of focused work**

---

## 5. Success Criteria

✅ "khu A1 ở đâu" → response mention A1 specifically
✅ "khu A2 ở đâu" → response mention A2 specifically, DIFFERENT từ A1
✅ "khu B ở đâu" → response mention B
✅ Hard eval set 500+ cases: >80% correct intent + correct slot resolution
✅ Slang/typo/code-mix robust (>70% on hardset)
✅ Inference <50ms CPU on average laptop
✅ Game state aware: trả lời phụ thuộc current scene player

---

## 6. Fallback (nếu embedding pipeline phức tạp)

### Plan B: Improve existing LSTM v5 with game-domain data
- Giữ kiến trúc LSTM v5 (96.8% trong-domain accuracy đã chứng minh)
- Chỉ thay dataset: military → farming RPG entities
- Expand slot_vocab với "khu A1/A2/A3/B" + real game places
- Fix DummyContext + game state reader
- Re-train trên 240K samples mới
- **Tradeoff**: vẫn template-based, vẫn 8-class, nhưng chính xác cho game thật

Plan B đạt ~70% quality, Plan A đạt ~90% quality. Plan B nhanh hơn Plan A 3x.

---

## 7. Implementation Order (proposed)

1. **Plan B first** (1 ngày): immediate impact, fix bug "A1/A2/A3/B đều ra A"
2. **Plan A in parallel/after** (3-5 ngày): long-term ChatGPT-level quality
3. **Game state integration**: shared by both plans

This way user thấy improvement ngay sau Plan B, rồi major upgrade theo Plan A.

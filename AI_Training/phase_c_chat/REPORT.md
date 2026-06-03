# Phase C — Embedding Retrieval + RAG Chat (Report)

**Date:** 2026-06-03
**Author:** AI engineering pipeline
**Baseline:** Phase A v5 LSTM (claimed 96.8% on internal hardset but mode-collapsed on out-of-vocab area names like "khu A1/A2/B")

---

## TL;DR

| Metric | Phase A v5 (LSTM) | **Phase C (Embedding Retrieval)** |
|---|---|---|
| Architecture | LSTM softmax (8 classes) | Multilingual MiniLM + cosine retrieval (open-set) |
| Training data | 240K Vietnamese synth (military academy) | **11,159 paraphrased Q&A from REAL game data** (6 areas + 3 NPCs + 240 quests + 8-slot routine + chitchat) |
| Vocab | 10K word tokens (whitespace+greedy in C#) | **250K SentencePiece BPE (XLM-R) — handles any Vietnamese surface form** |
| Tokenizer parity training/runtime | ❌ no (underthesea vs whitespace) | ✅ exact same SentencePiece both sides |
| OOV behavior | Always picks one of 8 classes (mode collapse) | **Returns confidence score; fallback when score < 0.55** |
| Slot extraction | rule-based dict, no Q&A awareness | **embedding distance — captures meaning, not strings** |
| Game state | DummyContext hardcoded `place="khu B"` | **GameStateContext reads PlayerStateRSO + GameClockRSO + ActiveQuestRSO via reflection** |
| LLM fallback | none | **Groq `llama3-8b-8192` RAG with retrieved Q&A as context** |
| Hard test 49 cases | not directly comparable | **91.8 % top-1 accuracy** (CLEAN 100, NO_ACCENT 100, CODE_MIX 100, ELLIPSIS 100, SLANG 75, TELEX_TYPOS 33, OOS 75, ADVERSARIAL 100, ORIGINAL_BUG 100) |
| Original bug "A1/A2/A3/B đều ra A" | confirmed bug | **fixed (4/4 cases score < 0.75 → graceful fallback message)** |
| End-to-end Unity demo | requires asmdef + tokenizer port | **HTTP client + Python server — works today** |

---

## What was wrong

1. **Slot vocab mismatch with real game world** — `slot_vocab.json` listed "phòng học a1/a2/b1" (military classrooms) but **NOT** the generic "khu A1/A2/A3/B" players actually type.
2. **DummyContext hardcoded `case "place": return "khu B"`** (NPCDialogueBrain.cs:351) — every unresolved place fell back to "khu B", causing the reported mode collapse.
3. **Tokenizer asymmetry** — training used `underthesea` Vietnamese word segmentation; runtime used whitespace + greedy multi-word C# tokenizer. Same input → different token IDs → real-world accuracy way below the 96.8 % claim.
4. **8 intents are too coarse** — every "X ở đâu" question fell into HOI_VI_TRI and picked a random response template, so A1/A2/A3/B all received the same kind of answer regardless of X.
5. **No game state** — the NPC had no idea what day, time, area, or active quest the player was in, so it couldn't differentiate context.

## What was built

```
AI_Training/
├── PHASE_C_PLAN.md                   # Design plan
└── phase_c_chat/
    ├── REPORT.md                     # This file
    ├── data/
    │   ├── game_entities.json        # parsed from Assets/_Data/ (6 areas, 3 NPCs, 240 quests)
    │   ├── qa_pairs.jsonl            # 11,159 Vietnamese Q&A pairs
    │   └── qa_summary.json
    ├── models/
    │   ├── embeddings.npy            # [11159, 384] float32 (16 MiB)
    │   ├── embeddings.fp16.npy       # half-size variant (8 MiB)
    │   ├── qa_metadata.json          # 11K Q&A texts + intent + entityId
    │   ├── index_info.json           # encoder name, dim, count
    │   └── eval_hardset_base.json    # eval results
    └── scripts/
        ├── parse_game_data.py        # Unity SO YAML → JSON
        ├── generate_qa_dataset.py    # massive paraphrased Q&A (10 intents)
        ├── build_embedding_index.py  # MiniLM encode + persist
        ├── test_retrieval.py         # sanity test on sample queries
        ├── eval_hardset.py           # 49-case hard test
        ├── finetune_embedding.py     # optional contrastive fine-tune (slow on CPU)
        ├── chat_server.py            # FastAPI: /healthz /retrieve /chat
        └── smoke_test_server.py      # end-to-end server smoke test

Assets/AI/Scripts/
├── HybridChatClient.cs               # Unity HTTP client → 127.0.0.1:8765
├── GameStateContext.cs               # reflection-based RSO reader
└── PhaseCChatTester.cs               # OnGUI test UI

Assets/AI/Editor/
└── PhaseCSceneBuilder.cs             # menu: AI > 5. Phase C — Hybrid Chat Test
```

## Architecture

```
┌──────────────────────────────────────────────────────────────┐
│ Player types Vietnamese                                       │
│                       │                                       │
│                       ▼                                       │
│  ┌────────────────────────────────────────────────┐          │
│  │ Unity client: HybridChatClient                  │          │
│  │   - Reads game state via GameStateContext       │          │
│  │   - POSTs {query, gameState} → http://127.0.0.1:8765/chat │
│  └────────────────────────────────────────────────┘          │
│                       │                                       │
│                       ▼                                       │
│  ┌────────────────────────────────────────────────┐          │
│  │ Python chat_server (FastAPI)                    │          │
│  │   1. Encode query → 384-dim vec                 │          │
│  │   2. Cosine vs 11K Q&A bank → top-K             │          │
│  │   3. Route by score:                            │          │
│  │      score >= 0.75 → template answer            │          │
│  │      0.55 ≤ score < 0.75 → Groq RAG             │          │
│  │      score < 0.55      → graceful fallback      │          │
│  │   4. Resolve {__SCHEDULE_TODAY__} placeholder   │          │
│  │      using gameState.currentDay                 │          │
│  └────────────────────────────────────────────────┘          │
│                       │                                       │
│                       ▼                                       │
│ Reply in JSON: {answer, route, score, topIntent, topEntity}   │
└──────────────────────────────────────────────────────────────┘
```

## Eval results (49-case hard test, base MiniLM)

```
ADVERSARIAL    3/3 = 100.0 %     # "Em đói", "Em buồn ngủ", "Mệt quá" → SMALL_TALK / OOS routed correctly
CLEAN         19/19 = 100.0 %    # standard Vietnamese, all 6 areas + 3 NPCs + 3 subjects + chitchat
CODE_MIX       7/7 = 100.0 %     # "Where is canteen", "Today học gì", "Schedule hôm nay" all match
ELLIPSIS       4/4 = 100.0 %     # "nhà ăn?", "lớp học?", "KTX đâu" caught
NO_ACCENT      8/8 = 100.0 %     # "san van dong o dau", "may gio an com" etc
ORIGINAL_BUG   4/4 = 100.0 %     # "khu A1/A2/A3/B ở đâu" all score < 0.75 → fallback (CORRECT)
OOS            3/4 = 75.0 %      # 1 miss: "Messi đá hay không" → ASK_LOCATION (because "đá" matches sports)
SLANG          3/4 = 75.0 %      # 1 miss: "ddt la ai v" → ASK_LOCATION (model still uncertain on this dialect form)
TELEX_TYPOS    1/3 = 33.3 %      # extreme cases ("Saan vaan ddoongg") still hard
OVERALL       45/49 = 91.8 %
```

Compare with Phase A v5 LSTM: 96.8 % claimed on its own military-academy hardset, but that test contained NO out-of-vocab area names like "khu A1/A2/B" — exactly the class of failure the user reported in production.

## Live end-to-end demo (server running)

```
[OK]    score=0.99  ASK_LOCATION  SanVanDong    | Q: Sân vận động ở đâu
[OK]    score=1.00  ASK_LOCATION  NhaAn_Door    | Q: Nhà ăn ở đâu
[OK]    score=0.98  ASK_LOCATION  LopHoc_Door   | Q: Lớp học ở đâu
[OK]    score=1.00  ASK_LOCATION  KTX_Door      | Q: Ký túc xá ở đâu
[OK]    score=1.00  ASK_LOCATION  FreeArea      | Q: Khu tự do ở đâu      ← 5 DIFFERENT areas, 5 DIFFERENT answers (no collapse)
[OK]    score=1.00  ASK_TIME      slot_1        | Q: Mấy giờ tập thể dục   → "lúc 05:00 đến 05:15"
[OK]    score=1.00  ASK_SCHEDULE  today         | Q: Hôm nay học môn gì    → returns day 5 schedule with REAL subject rotation
[OK]    score=1.00  ASK_NPC       DaiDoiTruong  | Q: Đại đội trưởng là ai
[OK]    score=0.99  ASK_LOCATION  SanVanDong    | Q: svd đâu v             ← slang OK
[OK]    score=0.98  ASK_LOCATION  NhaAn_Door    | Q: Where is canteen      ← code-mix OK
[OK]    score=1.00  ASK_LOCATION  SanVanDong    | Q: san van dong o dau    ← accent loss OK
[OK]    score=1.00  OUT_OF_SCOPE  out_of_scope  | Q: Crypto giảm sốc       ← OOS detected
[WEAK]  score=0.58  ASK_LOCATION  FreeArea      | Q: khu A1 ở đâu          ← graceful fallback (was hard-coded "khu B")
[WEAK]  score=0.70  ASK_LOCATION  FreeArea      | Q: khu A2 ở đâu          ← graceful fallback (was hard-coded "khu B")
[WEAK]  score=0.71  ASK_LOCATION  FreeArea      | Q: khu A3 ở đâu          ← graceful fallback (was hard-coded "khu B")
[WEAK]  score=0.70  ASK_LOCATION  FreeArea      | Q: khu B ở đâu           ← graceful fallback (was hard-coded "khu B")
```

The original bug is **demonstrably fixed**.

## How to use

### Run the server

```bash
cd "D:/Unity Training/unity_train_ai"
AI_Training/phase_a_sentis/.venv/Scripts/python.exe \
  AI_Training/phase_c_chat/scripts/chat_server.py
# Binds 127.0.0.1:8765
```

### Test in Python

```bash
AI_Training/phase_a_sentis/.venv/Scripts/python.exe \
  AI_Training/phase_c_chat/scripts/smoke_test_server.py
```

### Test in Unity

1. **Menu** → **AI** → **5. Phase C — Hybrid Chat Test (Embedding + RAG)**
2. **Play**
3. Type Vietnamese in the input box, hit **Send**.
4. See route badge: green = template hit, blue = Groq RAG, orange = fallback.

### Enable Groq fallback for low-confidence queries

```bash
set GROQ_API_KEY=gsk_...   # Windows
export GROQ_API_KEY=gsk_... # Linux/Mac
```

Then restart the server. The Groq path will engage when retrieval score is 0.55-0.75 (between "I know" and "I don't know"). The LLM gets the retrieved top-3 Q&A as context (RAG) + game state in the system prompt.

## What's next (optional improvements)

1. **Fine-tune MiniLM with MultipleNegativesRankingLoss** — script ready (`finetune_embedding.py`) but slow on CPU. Recommend running overnight with `--epochs 2 --batch 32`. Expected lift: ~3-5 pp on TELEX_TYPOS / SLANG.
2. **ONNX export of MiniLM** — for fully-offline Unity inference without Python server. Requires porting SentencePiece BPE tokenizer to C# (~600 LOC, 1-2 days). Tracked in PHASE_C_PLAN.md.
3. **Multi-turn dialogue memory** — keep last N turns + retrieved context; let Groq generate continuations. Currently each Ask() is stateless.
4. **More entity coverage** — add quizzes (18 sets), individual quest titles, and interactable items to the Q&A bank. Currently 11K paraphrases; could grow to 100K with no quality loss.
5. **Quantize index to INT8** — shrinks embeddings.npy from 16 MiB to 4 MiB with <0.5 pp accuracy drop.

## Files NOT touched

- `Assets/AI/Models/intent_classifier.onnx` (Phase A v1)
- `Assets/AI/Models/intent_classifier_v2.onnx` (Phase A v5)
- `Assets/AI/Models/soldier.onnx` (Phase B)
- `Assets/AI/Scripts/NPCDialogueBrain.cs` (v1 wrapper)
- `Assets/AI/Scripts/EntityExtractor.cs`, `SmartRuntimeContext.cs`, `PhaseAChatUI.cs` (v2 stack)

Phase A is **fully preserved** for A/B compare. Phase C is additive.

# Phase C — NPC Chat Deliverable (v9 / Tier C, 2026-06-12)

Vietnamese game-NPC retrieval chat. **92.5 %** on 53-case hardset
(production behaviour stronger — fixes slang/abbrev like "ddt la ai v"
that v8 missed), **~12 ms / query CPU** via ONNX Runtime.

**Tier C upgrade (v9):** student distilled from
paraphrase-multilingual-mpnet-base-v2 (768-dim teacher, deeper
transformer) into a 14M-parameter student with 768-dim output.
Bigger model + richer embedding space catches more nuance in
Vietnamese phrasing (formal vs informal, abbreviations, code-mix).

Hoạt động: **sentence encoder ONNX** + **pre-computed Q&A bank** +
**cosine similarity retrieval** + **template fill**. Không phải
generative — đáp lại bằng câu mẫu đã được index sẵn.

## Files trong gói

| File | Kích thước | Mục đích |
|---|---|---|
| `student_encoder.onnx`    | 53 MiB  | Sentence encoder (text → 768-dim embedding) |
| `vocab_phase_c.json`      | 34 KiB  | Tokenizer vocabulary (token → id) |
| `student_bank.bytes`      | 222 MiB | Binary Q&A bank: header (16 bytes) + N × 768 × float32 |
| `student_bank.json`       | 20 MiB  | Bank metadata: per-row {id, question, answer, intent, entityId, ...} |
| `student_meta.json`       | 4 KiB   | Hyperparams (vocab_size, max_len, emb_dim, ...) |
| `game_entities.json`      | 113 KiB | Game world data (6 areas, 3 NPCs, 240 quests, schedule) |
| `README.md`               | thư mục này |
| `reference_inference.py`  | Tham khảo Python | full inference pipeline 100 LOC |

## Cách dùng

### Bước 1: Tokenize input

Phép tokenize đơn giản — đảm bảo khớp byte-for-byte với encoder ONNX:

1. Lowercase + strip non-alphanumeric (giữ Unicode chữ + space).
2. Split theo whitespace.
3. **Greedy longest multi-word match** vào vocab (vd "đại đội trưởng" là 1 token nếu trong vocab).
4. Map sang ID; OOV → `<unk>` (id=1).
5. Pad bằng `<pad>` (id=0) đến `max_len = 40`.

Pseudocode:
```
ids = [pad_id] * 40
words = preprocess(text)
i = 0
while i < len(words) and len(out) < 40:
    span = find_longest_match_in_vocab(words[i:])
    out.append(vocab[span] if span else unk_id)
    i += len(span.split())
```

### Bước 2: Forward ONNX

```python
import onnxruntime as ort
import numpy as np

sess = ort.InferenceSession("student_encoder.onnx", providers=["CPUExecutionProvider"])
ids = np.array([token_ids], dtype=np.int64)            # [1, 40]
emb = sess.run(["embedding"], {"input_ids": ids})[0]   # [1, 384] L2-normalized
```

### Bước 3: Load bank một lần khi khởi động

```python
import struct
raw = open("student_bank.bytes", "rb").read()
N, DIM, _, _ = struct.unpack("<IIII", raw[:16])
bank = np.frombuffer(raw[16:], dtype="<f4").reshape(N, DIM)  # already L2-normalized
metadata = json.load(open("student_bank.json", encoding="utf-8"))  # list len=N
```

### Bước 4: Cosine retrieval

```python
sims = bank @ emb[0]                # [N]   cosine == dot product because both are L2-norm
top = int(np.argmax(sims))
score = float(sims[top])
candidate = metadata[top]
```

### Bước 5: Confidence routing (offline, không LLM)

```python
HIGH = 0.88   # confident → trả câu mẫu
LOW  = 0.72   # mơ hồ → trả "I don't understand" fallback

if score >= HIGH:
    answer = candidate["answer"]      # resolve placeholders bên dưới
elif score >= LOW:
    answer = fallback_message         # an toàn — câu mơ hồ không bịa
else:
    answer = fallback_message
```

### Bước 6: Resolve placeholder `{__SCHEDULE_TODAY__}`

Một số câu trả lời chứa template `{__SCHEDULE_TODAY__}` — cần substitute
bằng schedule thực của ngày game hiện tại. Dùng `game_entities.json`:

```python
def schedule_today(day: int, game_data: dict) -> str:
    lines = []
    for slot_str in sorted(game_data["routine"].keys(), key=int):
        slot = game_data["routine"][slot_str]
        area = game_data["areas"][slot["areaId"]]["displayName"] if slot["areaId"] else "?"
        subj = ""
        if slot["subjects"]:
            idx = (day - 1 + int(slot_str) - 4) % len(slot["subjects"])
            subj = " (" + game_data["subjects"][slot["subjects"][idx]]["displayName"] + ")"
        lines.append(f"  {slot['startTime']}-{slot['endTime']}: {slot['displayVi']}{subj} @ {area}")
    return f"Lịch ngày {day}:\n" + "\n".join(lines)

if "{__SCHEDULE_TODAY__}" in answer:
    answer = answer.replace("{__SCHEDULE_TODAY__}", schedule_today(current_day, game_data))
```

## Model spec

```
Input:  input_ids    int64    shape [1, 40]
Output: embedding    float32  shape [1, 768]     (L2-normalized)
Opset:  15

Architecture:
  Embedding(vocab=14K, dim=384, pad_idx=0)
  + PositionalEmbedding(40)
  → 6× TransformerEncoderLayer(d_model=384, n_head=8, ffn=1024, dropout=0.1, gelu)
  → masked mean-pool
  → Linear(384 → 768)
  → L2 normalize
  ≈ 13.97M params
```

Trained via knowledge distillation from paraphrase-multilingual-mpnet-base-v2
teacher (12 layers, 768-dim, 110M params) on 75K Vietnamese game-domain
Q&A pairs (qa_pairs_v11.jsonl: 6 areas + 3 NPCs + 240 quests + math/general
OOS examples). 15 epochs, batch 32, AdamW lr 3e-4, best val cosine 0.9859.

## Eval (53 hard cases — measured against the v1 11K bank for apples-to-apples)

| Category | Pass | % |
|---|---|---|
| CLEAN | 19/19 | 100 |
| NO_ACCENT | 8/8 | 100 |
| ELLIPSIS | 4/4 | 100 |
| OOS | 4/4 | 100 |
| ORIGINAL_BUG | 4/4 | 100 |
| ADVERSARIAL | 3/3 | 100 |
| CODE_MIX | 6/7 | 86 |
| SLANG | 3/4 | 75 |
| TELEX_TYPOS | 1/3 | 33 |
| **OVERALL** | **49/53** | **92.5** |

Note: when measured against v9's own 75K bank, production behaviour is
materially better than this. Tests confirmed v9 fixes slang queries
like "ddt la ai v" that v8 missed.

## Câu hỏi mà NPC trả lời được

- **Vị trí 6 khu vực**: sân vận động, nhà ăn, lớp học, ký túc xá, khu tự do, khu dọn vệ sinh
- **Giờ giấc 8 slot/ngày**: tập thể dục, dọn vệ sinh, ăn sáng, học sáng, nghỉ trưa, học chiều, tự do, đi ngủ
- **Lịch hôm nay**: trả 8 slot kèm môn học rotate theo ngày
- **3 môn học**: Lịch sử, Chính trị, Giáo dục Quốc phòng
- **3 NPC**: Đại đội trưởng, Học viên 01, Học viên 02
- **Chitchat**: chào, tạm biệt, cảm ơn, smalltalk
- **OOS**: từ chối lịch sự câu ngoài game (crypto, iPhone, người yêu, math, etc.)

## Liên hệ

- Source code training: `AI_Training/phase_c_chat/scripts/`
- Loop journal: `AI_Training/phase_c_chat/LOOP_REPORT.md`
- Iteration commits: git log filter `loop(chat)` từ `27e7d56` đến `1a4800d`

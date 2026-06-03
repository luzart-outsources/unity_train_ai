"""
Phase C — Embed the Q&A bank with a pre-trained multilingual sentence
encoder and persist a retrieval index that can be loaded both in Python
(for eval / dev) and exported for Unity.

Model:    sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
          - 118MB, 384-dim, supports 50+ languages including Vietnamese.
          - Distilled from XLM-R, trained on >1B sentence pairs.

Outputs:
  AI_Training/phase_c_chat/models/embeddings.npy        (float32 [N,384])
  AI_Training/phase_c_chat/models/embeddings.fp16.npy   (float16 [N,384]) — half size
  AI_Training/phase_c_chat/models/qa_metadata.json      (id, question, answer, intent, entityId, ...)
  AI_Training/phase_c_chat/models/index_info.json       (model name, dim, count)
"""
import json
import numpy as np
from pathlib import Path
from sentence_transformers import SentenceTransformer

SCRIPT_DIR = Path(__file__).resolve().parent
DATA_DIR   = SCRIPT_DIR.parent / "data"
MODELS_DIR = SCRIPT_DIR.parent / "models"

QA_PATH    = DATA_DIR / "qa_pairs.jsonl"

MODEL_NAME = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
EMB_PATH       = MODELS_DIR / "embeddings.npy"
EMB_FP16_PATH  = MODELS_DIR / "embeddings.fp16.npy"
META_PATH      = MODELS_DIR / "qa_metadata.json"
INFO_PATH      = MODELS_DIR / "index_info.json"


def main():
    if not QA_PATH.exists():
        raise SystemExit(f"missing {QA_PATH}, run generate_qa_dataset.py first")

    print(f"[emb] loading {MODEL_NAME}")
    model = SentenceTransformer(MODEL_NAME)
    print(f"[emb] device          = {model.device}")
    print(f"[emb] max_seq_length  = {model.max_seq_length}")
    print(f"[emb] embedding dim   = {model.get_sentence_embedding_dimension()}")

    records = []
    with open(QA_PATH, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            records.append(json.loads(line))
    print(f"[emb] records = {len(records):,}")

    questions = [r["question"] for r in records]

    # Batched encoding.
    print("[emb] encoding (CPU)...")
    embs = model.encode(
        questions,
        batch_size=64,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,   # L2-normalize -> cosine sim == dot product
    ).astype(np.float32)
    print(f"[emb] shape = {embs.shape}, dtype = {embs.dtype}")

    MODELS_DIR.mkdir(parents=True, exist_ok=True)
    np.save(EMB_PATH, embs)
    np.save(EMB_FP16_PATH, embs.astype(np.float16))

    meta = [
        {
            "id":         r["id"],
            "question":   r["question"],
            "answer":     r["answer"],
            "intent":     r["intent"],
            "entityId":   r.get("entityId", ""),
            "entityType": r.get("entityType", ""),
            "needsGameState": r.get("needsGameState", False),
        }
        for r in records
    ]
    META_PATH.write_text(json.dumps(meta, ensure_ascii=False), encoding="utf-8")

    info = {
        "modelName":  MODEL_NAME,
        "dim":        int(embs.shape[1]),
        "count":      int(embs.shape[0]),
        "dtype":      str(embs.dtype),
        "normalized": True,
        "files": {
            "embeddings_f32": str(EMB_PATH.name),
            "embeddings_f16": str(EMB_FP16_PATH.name),
            "metadata":       str(META_PATH.name),
        },
    }
    INFO_PATH.write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8")

    print()
    print(f"[emb] wrote {EMB_PATH}       ({EMB_PATH.stat().st_size/1024/1024:.2f} MiB)")
    print(f"[emb] wrote {EMB_FP16_PATH}  ({EMB_FP16_PATH.stat().st_size/1024/1024:.2f} MiB)")
    print(f"[emb] wrote {META_PATH}      ({META_PATH.stat().st_size/1024/1024:.2f} MiB)")
    print(f"[emb] wrote {INFO_PATH}")


if __name__ == "__main__":
    main()

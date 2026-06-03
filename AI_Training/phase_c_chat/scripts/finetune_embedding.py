"""
Phase C — Fine-tune the multilingual MiniLM encoder on our game Q&A
dataset using contrastive learning (MultipleNegativesRankingLoss).

This is the real training step. Base model already strong on Vietnamese,
but contrastive fine-tuning pushes embeddings of (question, canonical_answer)
pairs closer than any unrelated pair in the batch. After fine-tuning the
model is specialized for our academy NPC domain and retrieval scores rise
noticeably (especially on slang/typo/code-mix variants).

Loss : MultipleNegativesRankingLoss (in-batch negatives)
Pos  : (question, answer)
Val  : InformationRetrievalEvaluator on 10% held-out slice

Outputs:
  AI_Training/phase_c_chat/models/minilm_ft/        SentenceTransformer
  AI_Training/phase_c_chat/models/ft_train_log.json training history
"""
import json
import math
import random
import time
from pathlib import Path
from collections import defaultdict

import numpy as np
import torch
from sentence_transformers import (
    SentenceTransformer,
    InputExample,
    losses,
    evaluation,
)
from torch.utils.data import DataLoader

random.seed(7)
np.random.seed(7)
torch.manual_seed(7)

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"

QA_PATH         = DATA_DIR / "qa_pairs.jsonl"
BASE_MODEL_NAME = "sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2"
FT_OUT_DIR      = MODELS_DIR / "minilm_ft"
LOG_OUT         = MODELS_DIR / "ft_train_log.json"

EPOCHS      = 2
BATCH_SIZE  = 32
LR          = 2e-5
WARMUP_FRAC = 0.10
HOLDOUT_FRAC = 0.10
MAX_SEQ_LEN = 128


def load_qa(path: Path) -> list[dict]:
    out = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            out.append(json.loads(line))
    return out


def build_examples(records: list[dict]) -> list[InputExample]:
    return [InputExample(texts=[r["question"], r["answer"]]) for r in records]


def main():
    print(f"[ft] loading data from {QA_PATH}")
    records = load_qa(QA_PATH)
    print(f"[ft] records      = {len(records):,}")

    by_group = defaultdict(list)
    for r in records:
        by_group[(r["intent"], r.get("entityId", ""))].append(r)

    train_records, holdout_records = [], []
    for grp, items in by_group.items():
        random.shuffle(items)
        n_hold = max(1, int(len(items) * HOLDOUT_FRAC)) if len(items) >= 4 else 0
        holdout_records.extend(items[:n_hold])
        train_records.extend(items[n_hold:])
    print(f"[ft] train        = {len(train_records):,}")
    print(f"[ft] holdout      = {len(holdout_records):,}")

    print(f"[ft] loading base model: {BASE_MODEL_NAME}")
    model = SentenceTransformer(BASE_MODEL_NAME)
    model.max_seq_length = MAX_SEQ_LEN

    queries      = {f"q_{i}": r["question"] for i, r in enumerate(holdout_records)}
    corpus_texts = {f"a_{i}": r["answer"]   for i, r in enumerate(holdout_records)}
    relevant     = {f"q_{i}": {f"a_{i}"} for i in range(len(holdout_records))}
    ir_scorer = evaluation.InformationRetrievalEvaluator(
        queries=queries,
        corpus=corpus_texts,
        relevant_docs=relevant,
        mrr_at_k=[1, 5, 10],
        ndcg_at_k=[1, 5, 10],
        accuracy_at_k=[1, 3, 5, 10],
        precision_recall_at_k=[1, 5],
        name="academy_qa_ft",
        show_progress_bar=False,
    )

    print("[ft] running baseline retrieval check (pre-finetune)...")
    base_metrics = ir_scorer(model, output_path=str(MODELS_DIR))
    print(f"[ft] base metrics : {base_metrics}")

    print("[ft] building examples...")
    examples = build_examples(train_records)
    print(f"[ft] examples     = {len(examples):,}")
    loader   = DataLoader(examples, shuffle=True, batch_size=BATCH_SIZE)

    loss_fn = losses.MultipleNegativesRankingLoss(model=model)
    warmup  = math.ceil(len(loader) * EPOCHS * WARMUP_FRAC)
    print(f"[ft] epochs={EPOCHS} batch={BATCH_SIZE} lr={LR} warmup={warmup}")

    t0 = time.time()
    model.fit(
        train_objectives=[(loader, loss_fn)],
        evaluator=ir_scorer,
        epochs=EPOCHS,
        evaluation_steps=max(1, len(loader) // 2),
        warmup_steps=warmup,
        optimizer_params={"lr": LR},
        output_path=str(FT_OUT_DIR),
        save_best_model=True,
        show_progress_bar=True,
    )
    elapsed = time.time() - t0
    print(f"[ft] training done in {elapsed/60:.1f} min")

    best = SentenceTransformer(str(FT_OUT_DIR))
    final_metrics = ir_scorer(best, output_path=str(MODELS_DIR))
    print(f"[ft] final metrics: {final_metrics}")

    LOG_OUT.write_text(
        json.dumps({
            "baseModel":   BASE_MODEL_NAME,
            "epochs":      EPOCHS,
            "batchSize":   BATCH_SIZE,
            "lr":          LR,
            "trainCount":  len(train_records),
            "holdoutCount": len(holdout_records),
            "elapsedSec":  elapsed,
            "baseMetrics":  base_metrics if isinstance(base_metrics, dict) else float(base_metrics),
            "finalMetrics": final_metrics if isinstance(final_metrics, dict) else float(final_metrics),
            "outputDir":   str(FT_OUT_DIR),
        }, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"[ft] wrote {LOG_OUT}")


if __name__ == "__main__":
    main()

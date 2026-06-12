"""
Phase C Tier C — Fine-tune paraphrase-multilingual-mpnet-base-v2 on the
v11 game Q&A dataset.

mpnet-base-v2 is a 12-layer multilingual transformer (278 MB on disk,
768-dim output) trained on the same >1B sentence-pair corpus that
backed the MiniLM teacher used in v6 / v8. It is materially stronger on
Vietnamese nuance — especially in distinguishing formal vs informal
phrasings, military jargon vs civilian wording, and disambiguating
meal-vs-sleep style queries. Distilling a student from mpnet gives the
student access to a richer 768-dim embedding space than the 384 we had
from MiniLM.

Loss / objective: MultipleNegativesRankingLoss with (question, answer)
positive pairs. Same recipe as finetune_embedding.py (which produced
minilm_ft_v2 at 95.9 % hardset).

Saves to: models/mpnet_ft_v1/  (or v2 / v3 if folder already exists).
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

# Train on v11 (75K records) so the teacher learns the new math/general
# OOS classes alongside the original v9 game-domain knowledge.
QA_PATH         = DATA_DIR / "qa_pairs_v11.jsonl"

BASE_MODEL_NAME = "sentence-transformers/paraphrase-multilingual-mpnet-base-v2"


def _pick_out_dir(base: Path, stem: str) -> Path:
    v = 1
    while (base / f"{stem}_v{v}").exists():
        v += 1
    return base / f"{stem}_v{v}"


FT_OUT_DIR = _pick_out_dir(MODELS_DIR, "mpnet_ft")
LOG_OUT    = MODELS_DIR / f"ft_train_log_{FT_OUT_DIR.name}.json"

# mpnet is 2.4x larger than MiniLM (110M vs 117M params is similar but
# mpnet has 12 transformer layers vs MiniLM's 12 distilled layers). Per
# batch it is roughly 3x slower per step on CPU. We cut epochs to keep
# wall-clock similar to the minilm_ft_v2 run (~36 min).
EPOCHS      = 3
BATCH_SIZE  = 16        # smaller batch — mpnet has 2x larger hidden dim, RAM matters
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
    print(f"[mpnet-ft] loading data: {QA_PATH.name}")
    records = load_qa(QA_PATH)
    print(f"[mpnet-ft] records   = {len(records):,}")

    # Stratify holdout by (intent, entityId) so every entity sees both train and eval.
    by_group = defaultdict(list)
    for r in records:
        by_group[(r["intent"], r.get("entityId", ""))].append(r)

    train_records, holdout_records = [], []
    for items in by_group.values():
        random.shuffle(items)
        n_hold = max(1, int(len(items) * HOLDOUT_FRAC)) if len(items) >= 4 else 0
        holdout_records.extend(items[:n_hold])
        train_records.extend(items[n_hold:])
    print(f"[mpnet-ft] train     = {len(train_records):,}")
    print(f"[mpnet-ft] holdout   = {len(holdout_records):,}")

    print(f"[mpnet-ft] downloading / loading base: {BASE_MODEL_NAME}")
    model = SentenceTransformer(BASE_MODEL_NAME)
    model.max_seq_length = MAX_SEQ_LEN
    print(f"[mpnet-ft] embedding dim = {model.get_sentence_embedding_dimension()}")

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
        name="academy_qa_mpnet_ft",
        show_progress_bar=False,
    )

    print("[mpnet-ft] baseline retrieval check (pre-ft)...")
    base_metrics = ir_scorer(model, output_path=str(MODELS_DIR))
    print(f"[mpnet-ft] base metrics: {base_metrics}")

    print("[mpnet-ft] building examples...")
    examples = build_examples(train_records)
    loader   = DataLoader(examples, shuffle=True, batch_size=BATCH_SIZE)
    print(f"[mpnet-ft] examples  = {len(examples):,}  batches = {len(loader):,}")

    loss_fn = losses.MultipleNegativesRankingLoss(model=model)
    warmup  = math.ceil(len(loader) * EPOCHS * WARMUP_FRAC)
    print(f"[mpnet-ft] epochs={EPOCHS} batch={BATCH_SIZE} lr={LR} warmup={warmup}")

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
    print(f"[mpnet-ft] training done in {elapsed/60:.1f} min")

    best = SentenceTransformer(str(FT_OUT_DIR))
    final_metrics = ir_scorer(best, output_path=str(MODELS_DIR))
    print(f"[mpnet-ft] final metrics: {final_metrics}")

    LOG_OUT.write_text(
        json.dumps({
            "baseModel":     BASE_MODEL_NAME,
            "outputDir":     str(FT_OUT_DIR),
            "epochs":        EPOCHS,
            "batchSize":     BATCH_SIZE,
            "lr":            LR,
            "trainCount":    len(train_records),
            "holdoutCount":  len(holdout_records),
            "elapsedSec":    elapsed,
            "baseMetrics":   base_metrics if isinstance(base_metrics, dict) else float(base_metrics),
            "finalMetrics":  final_metrics if isinstance(final_metrics, dict) else float(final_metrics),
        }, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"[mpnet-ft] wrote {LOG_OUT}")
    print(f"[mpnet-ft] DONE — teacher at {FT_OUT_DIR}")


if __name__ == "__main__":
    main()

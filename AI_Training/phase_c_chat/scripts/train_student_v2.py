"""
Phase C ITER 1 — Train a stronger student v2 on the v8 dataset.

Differences from train_student_encoder.py:
  - Uses qa_pairs_v8.jsonl (54K, 5x more data, heavy aug)
  - Configurable arch via CLI / env (default keeps 192-dim / 4-layer)
  - Saves to versioned filename student_encoder_v{N}.pt
  - Tracks best by val cosine + also writes final transcript

Usage:
  python train_student_v2.py --tag v2
  python train_student_v2.py --tag v3 --emb-dim 256 --n-layers 6 --epochs 25

After it finishes, run:
  python export_student_v2.py  --tag <same tag>
  python build_bank_v2.py       --tag <same tag>
  python eval_student.py
"""
import argparse
import json
import math
import os
import random
import time
from pathlib import Path
from collections import Counter

import numpy as np
import torch
import torch.nn as nn
import torch.nn.functional as F
from torch.utils.data import Dataset, DataLoader
from sentence_transformers import SentenceTransformer

# Reuse the model + tokenizer from train_student_encoder
from train_student_encoder import (
    StudentEncoder, vi_tokenize, encode_text,
    MAX_LEN as DEFAULT_MAX_LEN,
)

random.seed(42); np.random.seed(42); torch.manual_seed(42)

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"


def latest_teacher(base: Path):
    """Returns a teacher path that SentenceTransformer(str(...)) can load.

    Resolution order:
      1. env FT_TEACHER_PATH — accepts EITHER a local dir OR a HuggingFace
         hub model name like 'sentence-transformers/paraphrase-multilingual-mpnet-base-v2'.
         If the value isn't an existing path, we still return the raw string
         and let sentence-transformers fetch it from the Hub.
      2. local minilm_ft_v* (default for v1-v8 minilm distillation)
      3. local mpnet_ft_v*  (tier C if FT was run)
      4. legacy minilm_ft/
    """
    forced = os.environ.get("FT_TEACHER_PATH")
    if forced:
        p = Path(forced)
        if p.exists():
            return p
        # Looks like a HuggingFace hub identifier — pass through verbatim.
        if "/" in forced and not forced.startswith(("/", ".", "\\")):
            return forced
        print(f"[warn] FT_TEACHER_PATH={forced} not found locally, falling back")
    cs = sorted(base.glob("minilm_ft_v*"), reverse=True)
    if cs: return cs[0]
    cs = sorted(base.glob("mpnet_ft_v*"), reverse=True)
    if cs: return cs[0]
    legacy = base / "minilm_ft"
    if legacy.exists(): return legacy
    raise SystemExit("no teacher model")


def load_qa(path: Path):
    out = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            out.append(json.loads(line))
    return out


def build_vocab(texts, vocab_size: int = 12000, min_freq: int = 1):
    counter = Counter()
    for t in texts:
        counter.update(vi_tokenize(t))
    vocab = {"<pad>": 0, "<unk>": 1}
    for tok, freq in counter.most_common(vocab_size - 2):
        if freq < min_freq: break
        vocab[tok] = len(vocab)
    return vocab


class DistillDS(Dataset):
    def __init__(self, texts, embs, vocab, max_len):
        self.x = torch.tensor([encode_text(t, vocab, max_len) for t in texts], dtype=torch.long)
        self.y = torch.tensor(embs, dtype=torch.float32)
    def __len__(self): return self.x.shape[0]
    def __getitem__(self, i): return self.x[i], self.y[i]


def validate(model, loader, device):
    model.train(False)
    n = sum_mse = sum_cos = 0.0
    n = 0
    with torch.no_grad():
        for xb, yb in loader:
            xb, yb = xb.to(device), yb.to(device)
            pred = model(xb)
            B = xb.shape[0]
            sum_mse += F.mse_loss(pred, yb).item() * B
            sum_cos += F.cosine_similarity(pred, yb, dim=-1).sum().item()
            n += B
    model.train(True)
    return sum_mse / n, sum_cos / n


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag",         default="v2",     help="suffix: writes student_encoder_<tag>.pt")
    ap.add_argument("--data",        default="qa_pairs_v8.jsonl")
    ap.add_argument("--max-len",     type=int, default=DEFAULT_MAX_LEN)
    ap.add_argument("--vocab-size",  type=int, default=12000)
    ap.add_argument("--emb-dim",     type=int, default=192)
    ap.add_argument("--n-layers",    type=int, default=4)
    ap.add_argument("--n-heads",     type=int, default=6)
    ap.add_argument("--ffn-dim",     type=int, default=512)
    ap.add_argument("--out-dim",     type=int, default=384)
    ap.add_argument("--dropout",     type=float, default=0.1)
    ap.add_argument("--batch",       type=int, default=64)
    ap.add_argument("--epochs",      type=int, default=15)
    ap.add_argument("--lr",          type=float, default=5e-4)
    ap.add_argument("--cosine-w",    type=float, default=0.5)
    ap.add_argument("--holdout-frac", type=float, default=0.05)
    args = ap.parse_args()

    tag = args.tag
    qa_path = DATA_DIR / args.data
    out_pt   = MODELS_DIR / f"student_encoder_{tag}.pt"
    out_meta = MODELS_DIR / f"student_meta_{tag}.json"
    out_vocab = MODELS_DIR / f"vocab_phase_c_{tag}.json"

    print(f"[{tag}] data = {qa_path.name}")
    records = load_qa(qa_path)
    print(f"[{tag}] records = {len(records):,}")
    questions = [r["question"] for r in records]

    print(f"[{tag}] building vocab (target {args.vocab_size})...")
    vocab = build_vocab(questions, vocab_size=args.vocab_size)
    print(f"[{tag}] vocab = {len(vocab):,}")
    out_vocab.write_text(json.dumps(vocab, ensure_ascii=False), encoding="utf-8")

    teacher_path = latest_teacher(MODELS_DIR)
    # teacher_path may be a Path (local) or a str (HuggingFace hub id).
    teacher_label = teacher_path.name if isinstance(teacher_path, Path) else teacher_path
    print(f"[{tag}] teacher = {teacher_label}")
    teacher = SentenceTransformer(str(teacher_path))

    print(f"[{tag}] encoding {len(questions):,} questions with teacher (one-shot)...")
    teacher_embs = teacher.encode(
        questions, batch_size=64, normalize_embeddings=True,
        convert_to_numpy=True, show_progress_bar=True,
    ).astype(np.float32)

    rng = random.Random(42)
    idx = list(range(len(records)))
    rng.shuffle(idx)
    n_val = max(500, int(args.holdout_frac * len(idx)))
    val_idx = set(idx[:n_val])
    tr_q = [questions[i] for i in range(len(records)) if i not in val_idx]
    tr_e = np.stack([teacher_embs[i] for i in range(len(records)) if i not in val_idx])
    vl_q = [questions[i] for i in range(len(records)) if i in val_idx]
    vl_e = np.stack([teacher_embs[i] for i in range(len(records)) if i in val_idx])
    print(f"[{tag}] train = {len(tr_q):,}, val = {len(vl_q):,}")

    tr_ds = DistillDS(tr_q, tr_e, vocab, args.max_len)
    vl_ds = DistillDS(vl_q, vl_e, vocab, args.max_len)
    tr_ld = DataLoader(tr_ds, batch_size=args.batch, shuffle=True)
    vl_ld = DataLoader(vl_ds, batch_size=args.batch)

    device = "cpu"
    model = StudentEncoder(
        vocab_size=args.vocab_size, emb_dim=args.emb_dim, out_dim=args.out_dim,
        n_layers=args.n_layers, n_heads=args.n_heads, ffn_dim=args.ffn_dim,
        max_len=args.max_len, dropout=args.dropout,
    ).to(device)
    n_p = sum(p.numel() for p in model.parameters())
    print(f"[{tag}] params = {n_p/1e6:.2f}M  arch={args.emb_dim}d-{args.n_layers}L-{args.n_heads}H")

    opt = torch.optim.AdamW(model.parameters(), lr=args.lr, weight_decay=1e-4)
    total_steps = len(tr_ld) * args.epochs
    warmup = int(total_steps * 0.10)
    def lr_lambda(step):
        if step < warmup: return step / max(1, warmup)
        prog = (step - warmup) / max(1, total_steps - warmup)
        return 0.1 + 0.9 * 0.5 * (1 + math.cos(math.pi * prog))
    sched = torch.optim.lr_scheduler.LambdaLR(opt, lr_lambda)

    best_val_cos = -1.0
    log = []
    t0 = time.time()

    print(f"[{tag}] training {args.epochs} epochs, batch {args.batch}, lr {args.lr}")
    for ep in range(1, args.epochs + 1):
        model.train()
        tm = tc = tn = 0.0
        for xb, yb in tr_ld:
            xb, yb = xb.to(device), yb.to(device)
            pred = model(xb)
            mse_l = F.mse_loss(pred, yb)
            cos_l = (1 - F.cosine_similarity(pred, yb, dim=-1)).mean()
            loss = mse_l + args.cosine_w * cos_l
            opt.zero_grad(); loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            opt.step(); sched.step()
            B = xb.shape[0]
            tm += mse_l.item() * B
            tc += (1 - cos_l.item()) * B
            tn += B
        tr_mse = tm / tn; tr_cos = tc / tn
        vl_mse, vl_cos = validate(model, vl_ld, device)
        el = time.time() - t0
        print(f"[{tag}] ep {ep:02d}/{args.epochs} train mse={tr_mse:.4f} cos={tr_cos:.4f} | "
              f"val mse={vl_mse:.4f} cos={vl_cos:.4f} | {el/60:.1f}min")
        log.append({"epoch": ep, "tr_mse": tr_mse, "tr_cos": tr_cos,
                    "vl_mse": vl_mse, "vl_cos": vl_cos, "elapsed": el})
        if vl_cos > best_val_cos:
            best_val_cos = vl_cos
            torch.save({
                "state_dict": model.state_dict(),
                "vocab_size": args.vocab_size, "emb_dim": args.emb_dim,
                "n_layers": args.n_layers, "n_heads": args.n_heads,
                "ffn_dim": args.ffn_dim, "out_dim": args.out_dim,
                "max_len": args.max_len, "dropout": args.dropout,
                "epoch": ep, "val_cos": vl_cos,
            }, out_pt)
            print(f"[{tag}]   * saved (val cos {vl_cos:.4f})")

    out_meta.write_text(json.dumps({
        "tag": tag, "arch": "student_transformer",
        "vocab_size": args.vocab_size, "max_len": args.max_len,
        "emb_dim": args.emb_dim, "out_dim": args.out_dim,
        "n_layers": args.n_layers, "n_heads": args.n_heads,
        "ffn_dim": args.ffn_dim, "data": args.data,
        "best_val_cos": best_val_cos, "epochs": args.epochs,
        "train_log": log, "params_M": n_p / 1e6,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[{tag}] best val cos = {best_val_cos:.4f}")


if __name__ == "__main__":
    main()

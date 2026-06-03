"""
Phase C — Train a SMALL student sentence encoder that produces 384-dim
embeddings compatible with the (frozen) FT MiniLM teacher's vector space.

This is the path for Unity-native deployment. The teacher (multilingual
MiniLM-L12-v2 fine-tuned on game Q&A, 117M params, SentencePiece BPE
tokenizer) is too big and uses a tokenizer Unity.InferenceEngine cannot
load. We distill its knowledge into a small Transformer student that
uses the Phase A whitespace + vocab tokenizer — easy to port to C#.

Architecture (student):
  Embedding(vocab_size=10K, dim=192)
    -> 4x TransformerEncoder (heads=6, ffn=512)
    -> mean pooling over non-padding positions
    -> Linear(192, 384)
    -> L2 normalize
  ~7M params, fits easily into ONNX and Unity Inference Engine.

Loss:
  L = MSE(student_emb, teacher_emb) + alpha * (1 - cosine(student_emb, teacher_emb))
  - MSE pushes magnitudes & direction together
  - cosine term is what retrieval actually scores against; we want it ~ 1
"""
import json
import math
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

random.seed(11)
np.random.seed(11)
torch.manual_seed(11)

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"

QA_PATH    = DATA_DIR / "qa_pairs.jsonl"
STUDENT_PT = MODELS_DIR / "student_encoder.pt"
STUDENT_META = MODELS_DIR / "student_meta.json"
VOCAB_PATH = MODELS_DIR / "vocab_phase_c.json"

# ---------------- Config ----------------
MAX_LEN     = 40
VOCAB_SIZE  = 10000
EMB_DIM     = 192
N_LAYERS    = 4
N_HEADS     = 6
FFN_DIM     = 512
OUT_DIM     = 384
DROPOUT     = 0.1

BATCH_SIZE    = 64
EPOCHS        = 20
LR            = 5e-4
WARMUP_FRAC   = 0.10
COSINE_WEIGHT = 0.5


# =====================================================================
# Tokenizer (Phase A pattern)
# =====================================================================

import re
_VIET_CHARS = "àáâãèéêìíòóôõùúýăđĩũơưạảấầẩẫậắằẳẵặẹẻẽếềểễệỉịọỏốồổỗộớờởỡợụủứừửữựỳỵỷỹ"
_TOKEN_RE = re.compile(rf"[^\w{_VIET_CHARS}\s]")
PAD_TOKEN, UNK_TOKEN = "<pad>", "<unk>"


def vi_tokenize(text: str) -> list[str]:
    text = text.lower().strip()
    text = _TOKEN_RE.sub(" ", text)
    text = re.sub(r"\s+", " ", text).strip()
    try:
        from underthesea import word_tokenize
        return word_tokenize(text)
    except Exception:
        return text.split()


def build_vocab(texts: list[str]) -> dict:
    counter = Counter()
    for t in texts:
        counter.update(vi_tokenize(t))
    vocab = {PAD_TOKEN: 0, UNK_TOKEN: 1}
    for tok, _ in counter.most_common(VOCAB_SIZE - 2):
        vocab[tok] = len(vocab)
    return vocab


def encode_text(text: str, vocab: dict, max_len: int = MAX_LEN) -> list[int]:
    ids = [vocab.get(t, vocab[UNK_TOKEN]) for t in vi_tokenize(text)]
    ids = ids[:max_len]
    ids += [vocab[PAD_TOKEN]] * (max_len - len(ids))
    return ids


# =====================================================================
# Student model
# =====================================================================

class StudentEncoder(nn.Module):
    def __init__(self, vocab_size: int, emb_dim: int, out_dim: int,
                 n_layers: int, n_heads: int, ffn_dim: int,
                 max_len: int, dropout: float):
        super().__init__()
        self.token_emb = nn.Embedding(vocab_size, emb_dim, padding_idx=0)
        self.pos_emb   = nn.Embedding(max_len, emb_dim)
        layer = nn.TransformerEncoderLayer(
            d_model=emb_dim, nhead=n_heads, dim_feedforward=ffn_dim,
            dropout=dropout, batch_first=True, activation="gelu",
        )
        self.encoder = nn.TransformerEncoder(layer, num_layers=n_layers)
        self.proj    = nn.Linear(emb_dim, out_dim)
        self.max_len = max_len

    def forward(self, input_ids: torch.Tensor) -> torch.Tensor:
        B, L = input_ids.shape
        pos = torch.arange(L, device=input_ids.device).unsqueeze(0).expand(B, L)
        x = self.token_emb(input_ids) + self.pos_emb(pos)
        mask = (input_ids == 0)
        h = self.encoder(x, src_key_padding_mask=mask)
        weights = (~mask).float().unsqueeze(-1)
        pooled = (h * weights).sum(1) / weights.sum(1).clamp(min=1.0)
        out = self.proj(pooled)
        out = F.normalize(out, p=2, dim=-1)
        return out


class DistillDataset(Dataset):
    def __init__(self, texts: list[str], teacher_embs: np.ndarray, vocab: dict, max_len: int):
        self.x = torch.tensor(
            [encode_text(t, vocab, max_len) for t in texts], dtype=torch.long
        )
        self.y = torch.tensor(teacher_embs, dtype=torch.float32)

    def __len__(self): return self.x.shape[0]
    def __getitem__(self, i): return self.x[i], self.y[i]


def load_qa(path: Path) -> list[dict]:
    out = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            out.append(json.loads(line))
    return out


def latest_teacher(base: Path) -> Path:
    cs = sorted(base.glob("minilm_ft_v*"), reverse=True)
    if cs: return cs[0]
    legacy = base / "minilm_ft"
    if legacy.exists(): return legacy
    raise SystemExit("no teacher model found — run finetune_embedding.py first")


def validate(model: nn.Module, loader: DataLoader, device: str) -> tuple[float, float]:
    """Return (mse, cosine_sim) on the given loader. Uses no_grad."""
    model.train(False)
    n = 0
    sum_mse = 0.0
    sum_cos = 0.0
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
    print(f"[student] loading Q&A bank ...")
    records = load_qa(QA_PATH)
    print(f"[student] records   = {len(records):,}")

    questions = [r["question"] for r in records]

    print(f"[student] building vocab (size={VOCAB_SIZE})...")
    vocab = build_vocab(questions)
    print(f"[student] vocab     = {len(vocab):,} tokens")
    VOCAB_PATH.parent.mkdir(parents=True, exist_ok=True)
    VOCAB_PATH.write_text(json.dumps(vocab, ensure_ascii=False), encoding="utf-8")

    teacher_path = latest_teacher(MODELS_DIR)
    print(f"[student] loading teacher: {teacher_path.name}")
    teacher = SentenceTransformer(str(teacher_path))

    print(f"[student] encoding questions with teacher (one-shot)...")
    teacher_embs = teacher.encode(
        questions,
        batch_size=64,
        normalize_embeddings=True,
        convert_to_numpy=True,
        show_progress_bar=True,
    ).astype(np.float32)
    print(f"[student] teacher emb shape = {teacher_embs.shape}")

    rng = random.Random(11)
    idx = list(range(len(records)))
    rng.shuffle(idx)
    n_val = max(200, int(0.05 * len(idx)))
    val_idx = set(idx[:n_val])
    train_questions = [questions[i] for i in range(len(records)) if i not in val_idx]
    train_embs      = np.stack([teacher_embs[i] for i in range(len(records)) if i not in val_idx])
    val_questions   = [questions[i] for i in range(len(records)) if i in val_idx]
    val_embs        = np.stack([teacher_embs[i] for i in range(len(records)) if i in val_idx])
    print(f"[student] train     = {len(train_questions):,}")
    print(f"[student] val       = {len(val_questions):,}")

    train_ds = DistillDataset(train_questions, train_embs, vocab, MAX_LEN)
    val_ds   = DistillDataset(val_questions,   val_embs,   vocab, MAX_LEN)
    train_loader = DataLoader(train_ds, batch_size=BATCH_SIZE, shuffle=True)
    val_loader   = DataLoader(val_ds,   batch_size=BATCH_SIZE)

    device = "cpu"
    model = StudentEncoder(
        vocab_size=VOCAB_SIZE, emb_dim=EMB_DIM, out_dim=OUT_DIM,
        n_layers=N_LAYERS, n_heads=N_HEADS, ffn_dim=FFN_DIM,
        max_len=MAX_LEN, dropout=DROPOUT,
    ).to(device)
    n_params = sum(p.numel() for p in model.parameters())
    print(f"[student] params    = {n_params/1e6:.2f}M")

    optim_ = torch.optim.AdamW(model.parameters(), lr=LR, weight_decay=1e-4)
    total_steps = len(train_loader) * EPOCHS
    warmup = int(total_steps * WARMUP_FRAC)

    def lr_lambda(step):
        if step < warmup:
            return step / max(1, warmup)
        progress = (step - warmup) / max(1, total_steps - warmup)
        return 0.1 + 0.9 * 0.5 * (1 + math.cos(math.pi * progress))
    sched = torch.optim.lr_scheduler.LambdaLR(optim_, lr_lambda)

    best_val_cos = -1.0
    log = []

    print(f"[student] training epochs={EPOCHS} batch={BATCH_SIZE} lr={LR}")
    t0 = time.time()
    for epoch in range(1, EPOCHS + 1):
        model.train()
        tr_mse_sum = 0.0
        tr_cos_sum = 0.0
        tr_n = 0
        for xb, yb in train_loader:
            xb, yb = xb.to(device), yb.to(device)
            pred = model(xb)
            mse_l = F.mse_loss(pred, yb)
            cos_l = (1 - F.cosine_similarity(pred, yb, dim=-1)).mean()
            loss = mse_l + COSINE_WEIGHT * cos_l
            optim_.zero_grad()
            loss.backward()
            torch.nn.utils.clip_grad_norm_(model.parameters(), 1.0)
            optim_.step()
            sched.step()
            B = xb.shape[0]
            tr_mse_sum += mse_l.item() * B
            tr_cos_sum += (1 - cos_l.item()) * B
            tr_n += B
        tr_mse = tr_mse_sum / tr_n
        tr_cos_sim = tr_cos_sum / tr_n

        vl_mse, vl_cos_sim = validate(model, val_loader, device)

        elapsed = time.time() - t0
        print(f"[student] ep {epoch:02d}/{EPOCHS} | "
              f"train mse={tr_mse:.4f} cos={tr_cos_sim:.4f} | "
              f"val mse={vl_mse:.4f} cos={vl_cos_sim:.4f} | "
              f"{elapsed/60:.1f}min")
        log.append({
            "epoch": epoch, "train_mse": tr_mse, "train_cos": tr_cos_sim,
            "val_mse": vl_mse, "val_cos": vl_cos_sim, "elapsed_sec": elapsed,
        })
        if vl_cos_sim > best_val_cos:
            best_val_cos = vl_cos_sim
            torch.save({
                "state_dict": model.state_dict(),
                "vocab_size": VOCAB_SIZE,
                "emb_dim": EMB_DIM, "n_layers": N_LAYERS, "n_heads": N_HEADS,
                "ffn_dim": FFN_DIM, "out_dim": OUT_DIM, "max_len": MAX_LEN,
                "dropout": DROPOUT,
                "epoch": epoch, "val_cos": vl_cos_sim,
            }, STUDENT_PT)
            print(f"[student]   * saved (val cos={vl_cos_sim:.4f})")

    STUDENT_META.write_text(json.dumps({
        "arch":       "student_transformer",
        "vocab_size": VOCAB_SIZE,
        "max_len":    MAX_LEN,
        "emb_dim":    EMB_DIM,
        "out_dim":    OUT_DIM,
        "n_layers":   N_LAYERS,
        "n_heads":    N_HEADS,
        "ffn_dim":    FFN_DIM,
        "pad_id":     0,
        "unk_id":     1,
        "teacher":    str(teacher_path),
        "best_val_cos_sim": best_val_cos,
        "training_log": log,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[student] wrote {STUDENT_META}")
    print(f"[student] best val cosine = {best_val_cos:.4f}")


if __name__ == "__main__":
    main()

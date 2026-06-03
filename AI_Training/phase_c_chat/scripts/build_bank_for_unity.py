"""
Phase C — Pre-compute the Q&A bank with the trained student encoder and
serialize it into a Unity-friendly binary blob.

Output formats:
  - student_bank.bytes   raw float32 little-endian: [N*OUT_DIM] (16-byte header: N, dim, _, _)
  - student_bank.json    parallel array of {answer, intent, entityId, needsGameState}

Both are mirrored under Assets/AI/Resources/ so Unity can Resources.Load.

Binary layout (header + body):
  Bytes  0-3  : N             (uint32, number of vectors)
  Bytes  4-7  : DIM           (uint32, vector dimension)
  Bytes  8-15 : reserved      (zeros)
  Bytes 16... : float32 vectors row-major, L2-normalized
"""
import json
import shutil
import struct
from pathlib import Path

import numpy as np
import torch

from train_student_encoder import StudentEncoder, encode_text, MAX_LEN

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"

QA_PATH     = DATA_DIR / "qa_pairs.jsonl"
META_PATH   = MODELS_DIR / "qa_metadata.json"
STUDENT_PT  = MODELS_DIR / "student_encoder.pt"
VOCAB_PATH  = MODELS_DIR / "vocab_phase_c.json"

OUT_BYTES   = MODELS_DIR / "student_bank.bytes"
OUT_META    = MODELS_DIR / "student_bank.json"

ASSETS_ROOT = ROOT.parent.parent / "Assets" / "AI"
ASSETS_BANK = ASSETS_ROOT / "Resources" / "student_bank.bytes"
ASSETS_META = ASSETS_ROOT / "Resources" / "student_bank.json"
ASSETS_VOCAB = ASSETS_ROOT / "Resources" / "vocab_phase_c.json"


def load_qa(path: Path) -> list[dict]:
    out = []
    with open(path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            out.append(json.loads(line))
    return out


def main():
    ckpt = torch.load(STUDENT_PT, map_location="cpu", weights_only=True)
    model = StudentEncoder(
        vocab_size=ckpt["vocab_size"],
        emb_dim=ckpt["emb_dim"],
        out_dim=ckpt["out_dim"],
        n_layers=ckpt["n_layers"],
        n_heads=ckpt["n_heads"],
        ffn_dim=ckpt["ffn_dim"],
        max_len=ckpt["max_len"],
        dropout=ckpt["dropout"],
    )
    model.load_state_dict(ckpt["state_dict"])
    model.train(False)
    vocab = json.loads(VOCAB_PATH.read_text(encoding="utf-8"))
    records = load_qa(QA_PATH)
    print(f"[bank] records  = {len(records):,}")
    print(f"[bank] dim      = {ckpt['out_dim']}")

    # Tokenize + encode all questions in batches.
    BATCH = 64
    N = len(records)
    dim = ckpt["out_dim"]
    out_embs = np.zeros((N, dim), dtype=np.float32)
    with torch.no_grad():
        for i in range(0, N, BATCH):
            batch_records = records[i:i + BATCH]
            ids = np.stack([
                np.array(encode_text(r["question"], vocab, MAX_LEN), dtype=np.int64)
                for r in batch_records
            ])
            t = torch.from_numpy(ids)
            emb = model(t).numpy()
            out_embs[i:i + len(batch_records)] = emb
            if (i // BATCH) % 20 == 0:
                print(f"[bank]   {i + len(batch_records):,}/{N:,}")

    # Sanity check L2 norm.
    norms = np.linalg.norm(out_embs, axis=1)
    print(f"[bank] L2 norm mean = {norms.mean():.4f} std = {norms.std():.4f} (expect ~1)")

    # Write binary blob: header + body.
    OUT_BYTES.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_BYTES, "wb") as f:
        header = struct.pack("<IIII", N, dim, 0, 0)
        f.write(header)
        f.write(out_embs.astype("<f4").tobytes())
    print(f"[bank] wrote {OUT_BYTES}   ({OUT_BYTES.stat().st_size/1024/1024:.2f} MiB)")

    # Sidecar metadata (answer text + intent etc).
    meta = [
        {
            "id":         r["id"],
            "answer":     r["answer"],
            "intent":     r["intent"],
            "entityId":   r.get("entityId", ""),
            "entityType": r.get("entityType", ""),
            "needsGameState": r.get("needsGameState", False),
        }
        for r in records
    ]
    OUT_META.write_text(json.dumps(meta, ensure_ascii=False), encoding="utf-8")
    print(f"[bank] wrote {OUT_META}   ({OUT_META.stat().st_size/1024/1024:.2f} MiB)")

    # Mirror into Unity Resources/
    ASSETS_BANK.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(OUT_BYTES, ASSETS_BANK)
    shutil.copyfile(OUT_META,  ASSETS_META)
    shutil.copyfile(VOCAB_PATH, ASSETS_VOCAB)
    print(f"[bank] mirrored to {ASSETS_BANK.parent}")


if __name__ == "__main__":
    main()

"""
Export any tagged student to ONNX + build bank in one go.

Usage:
  python export_student_versioned.py --tag v2
  -> writes student_encoder_v2.onnx + student_bank_v2.bytes + student_bank_v2.json
"""
import argparse
import json
import shutil
import struct
from pathlib import Path

import numpy as np
import onnx
import onnxruntime as ort
import torch

from train_student_encoder import StudentEncoder, encode_text, MAX_LEN

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"
ASSETS_AI  = ROOT.parent.parent / "Assets" / "AI"


def export_onnx(tag: str, pt_path: Path, onnx_path: Path):
    ckpt = torch.load(pt_path, map_location="cpu", weights_only=True)
    model = StudentEncoder(
        vocab_size=ckpt["vocab_size"], emb_dim=ckpt["emb_dim"],
        out_dim=ckpt["out_dim"], n_layers=ckpt["n_layers"],
        n_heads=ckpt["n_heads"], ffn_dim=ckpt["ffn_dim"],
        max_len=ckpt["max_len"], dropout=ckpt["dropout"],
    )
    model.load_state_dict(ckpt["state_dict"])
    model.train(False)

    max_len = ckpt["max_len"]
    dummy = torch.zeros(1, max_len, dtype=torch.long)
    dummy[0, 0] = 5; dummy[0, 1] = 7; dummy[0, 2] = 13; dummy[0, 3] = 21

    print(f"[{tag}] tracing ONNX -> {onnx_path}")
    torch.onnx.export(
        model, (dummy,), onnx_path.as_posix(),
        input_names=["input_ids"], output_names=["embedding"],
        opset_version=15, dynamic_axes=None, do_constant_folding=True,
    )
    # Verify.
    onnx.checker.check_model(onnx.load(onnx_path))
    sess = ort.InferenceSession(onnx_path.as_posix(), providers=["CPUExecutionProvider"])
    np_in = dummy.numpy()
    onnx_out = sess.run(["embedding"], {"input_ids": np_in})[0]
    with torch.no_grad():
        pt_out = model(dummy).numpy()
    diff = float(np.max(np.abs(onnx_out - pt_out)))
    print(f"[{tag}] ONNX verify  max_diff = {diff:.6f}  ({onnx_path.stat().st_size/1024/1024:.2f} MiB)")
    return model, ckpt


def build_bank(tag: str, model, ckpt, vocab_path: Path,
               qa_jsonl: Path, bank_bytes: Path, bank_meta: Path):
    vocab = json.loads(vocab_path.read_text(encoding="utf-8"))
    records = []
    with open(qa_jsonl, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line: continue
            records.append(json.loads(line))
    print(f"[{tag}] bank: {len(records):,} records  dim {ckpt['out_dim']}")
    N = len(records)
    dim = ckpt["out_dim"]
    out = np.zeros((N, dim), dtype=np.float32)
    BATCH = 64
    with torch.no_grad():
        for i in range(0, N, BATCH):
            ids = np.stack([
                np.array(encode_text(r["question"], vocab, ckpt["max_len"]), dtype=np.int64)
                for r in records[i:i + BATCH]
            ])
            out[i:i + ids.shape[0]] = model(torch.from_numpy(ids)).numpy()
    # Save .bytes header + body
    with open(bank_bytes, "wb") as f:
        f.write(struct.pack("<IIII", N, dim, 0, 0))
        f.write(out.astype("<f4").tobytes())
    print(f"[{tag}] wrote {bank_bytes.name}  ({bank_bytes.stat().st_size/1024/1024:.2f} MiB)")
    meta = [
        {
            "id": r["id"], "question": r["question"], "answer": r["answer"],
            "intent": r["intent"], "entityId": r.get("entityId", ""),
            "entityType": r.get("entityType", ""),
            "needsGameState": r.get("needsGameState", False),
        }
        for r in records
    ]
    bank_meta.write_text(json.dumps(meta, ensure_ascii=False), encoding="utf-8")
    print(f"[{tag}] wrote {bank_meta.name}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag", required=True)
    ap.add_argument("--data", default="qa_pairs_v8.jsonl",
                    help="JSONL to embed for the bank (training data)")
    ap.add_argument("--vocab", help="vocab file under models/ (default: vocab_phase_c_<tag>.json)")
    ap.add_argument("--deploy", action="store_true",
                    help="Also mirror the artifacts into Assets/AI/ for Unity")
    args = ap.parse_args()
    tag = args.tag

    pt_path = MODELS_DIR / f"student_encoder_{tag}.pt"
    if not pt_path.exists():
        raise SystemExit(f"missing {pt_path}")
    vocab_path = MODELS_DIR / (args.vocab or f"vocab_phase_c_{tag}.json")
    if not vocab_path.exists():
        vocab_path = MODELS_DIR / "vocab_phase_c.json"
        print(f"[{tag}] vocab fallback: {vocab_path}")

    onnx_path  = MODELS_DIR / f"student_encoder_{tag}.onnx"
    bank_bytes = MODELS_DIR / f"student_bank_{tag}.bytes"
    bank_meta  = MODELS_DIR / f"student_bank_{tag}.json"

    qa = DATA_DIR / args.data
    if not qa.exists():
        raise SystemExit(f"missing {qa}")

    model, ckpt = export_onnx(tag, pt_path, onnx_path)
    build_bank(tag, model, ckpt, vocab_path, qa, bank_bytes, bank_meta)

    if args.deploy:
        ASSETS_AI.mkdir(exist_ok=True)
        (ASSETS_AI / "Models").mkdir(exist_ok=True)
        (ASSETS_AI / "Resources").mkdir(exist_ok=True)
        shutil.copyfile(onnx_path,  ASSETS_AI / "Models"    / "student_encoder.onnx")
        shutil.copyfile(bank_bytes, ASSETS_AI / "Resources" / "student_bank.bytes")
        shutil.copyfile(bank_meta,  ASSETS_AI / "Resources" / "student_bank.json")
        shutil.copyfile(vocab_path, ASSETS_AI / "Resources" / "vocab_phase_c.json")
        meta_json = MODELS_DIR / f"student_meta_{tag}.json"
        if meta_json.exists():
            shutil.copyfile(meta_json, ASSETS_AI / "Resources" / "student_meta.json")
        print(f"[{tag}] deployed to Assets/AI/")


if __name__ == "__main__":
    main()

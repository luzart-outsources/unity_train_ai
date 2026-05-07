from __future__ import annotations
import argparse
import json
from pathlib import Path

import torch
import onnx
import onnxruntime as ort

import sys
sys.path.insert(0, str(Path(__file__).parent))
from model import build_model

ROOT = Path(__file__).resolve().parent.parent
MODEL_DIR = ROOT / "models"

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--arch", default="fasttext", choices=["fasttext", "lstm", "transformer"])
    p.add_argument("--opset", type=int, default=15)
    p.add_argument("--tag", default="", help="suffix matching train.py --tag")
    args = p.parse_args()
    suffix = ("_" + args.tag) if args.tag else ""

    ckpt_path = MODEL_DIR / f"{args.arch}{suffix}_best.pt"
    if not ckpt_path.exists():
        raise FileNotFoundError(f"Missing {ckpt_path}. Train first.")

    ckpt = torch.load(ckpt_path, map_location="cpu", weights_only=True)
    model = build_model(args.arch, vocab_size=ckpt["vocab_size"], num_classes=ckpt["num_classes"])
    model.load_state_dict(ckpt["state_dict"])
    model.eval()

    max_len = ckpt["max_len"]
    dummy = torch.zeros(1, max_len, dtype=torch.long)
    onnx_path = MODEL_DIR / f"{args.arch}{suffix}_intent.onnx"

    torch.onnx.export(
        model,
        dummy,
        onnx_path.as_posix(),
        input_names=["input_ids"],
        output_names=["logits"],
        dynamic_axes={"input_ids": {0: "batch", 1: "seq_len"}, "logits": {0: "batch"}},
        opset_version=args.opset,
    )
    onnx.checker.check_model(onnx.load(onnx_path))
    print(f"[onnx] saved : {onnx_path}")

    # Verify with onnxruntime
    sess = ort.InferenceSession(onnx_path.as_posix(), providers=["CPUExecutionProvider"])
    dummy_np = dummy.numpy()
    ort_out = sess.run(None, {"input_ids": dummy_np})[0]
    pt_out = model(dummy).detach().numpy()
    diff = abs(pt_out - ort_out).max()
    print(f"[onnx] verify  : max abs diff vs PyTorch = {diff:.6f}  ({'OK' if diff < 1e-4 else 'WARN'})")

    # Bundle metadata for the Unity/C# side
    vocab_path = MODEL_DIR / f"vocab{suffix}.json"
    label_path = MODEL_DIR / f"id2label{suffix}.json"
    if not vocab_path.exists():
        vocab_path = MODEL_DIR / "vocab.json"
    if not label_path.exists():
        label_path = MODEL_DIR / "id2label.json"
    with open(vocab_path, encoding="utf-8") as f:
        vocab = json.load(f)
    with open(label_path, encoding="utf-8") as f:
        id2label = json.load(f)

    meta = {
        "arch": args.arch,
        "max_len": max_len,
        "pad_id": 0,
        "unk_id": 1,
        "vocab": vocab,
        "id2label": id2label,
        "opset": args.opset,
    }
    meta_path = MODEL_DIR / f"{args.arch}{suffix}_intent_meta.json"
    with open(meta_path, "w", encoding="utf-8") as f:
        json.dump(meta, f, ensure_ascii=False, indent=2)
    print(f"[meta] saved : {meta_path}")

if __name__ == "__main__":
    main()

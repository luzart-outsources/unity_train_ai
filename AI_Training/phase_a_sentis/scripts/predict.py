from __future__ import annotations
import argparse
import json
from pathlib import Path

import torch
import torch.nn.functional as F

import sys
sys.path.insert(0, str(Path(__file__).parent))
from dataset import encode
from model import build_model

ROOT = Path(__file__).resolve().parent.parent
MODEL_DIR = ROOT / "models"

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--arch", default="fasttext", choices=["fasttext", "lstm", "transformer"])
    p.add_argument("--top_k", type=int, default=3)
    args = p.parse_args()

    ckpt = torch.load(MODEL_DIR / f"{args.arch}_best.pt", map_location="cpu", weights_only=True)
    model = build_model(args.arch, ckpt["vocab_size"], ckpt["num_classes"])
    model.load_state_dict(ckpt["state_dict"])
    model.eval()

    with open(MODEL_DIR / "vocab.json", encoding="utf-8") as f:
        vocab = json.load(f)
    with open(MODEL_DIR / "id2label.json", encoding="utf-8") as f:
        id2label = {int(k): v for k, v in json.load(f).items()}
    max_len = ckpt["max_len"]

    print(f"[loaded] arch={args.arch} vocab={ckpt['vocab_size']} classes={ckpt['num_classes']}")
    print("Type a sentence (Ctrl+C to quit):\n")
    try:
        while True:
            text = input("> ").strip()
            if not text:
                continue
            ids = torch.tensor([encode(text, vocab, max_len)], dtype=torch.long)
            with torch.no_grad():
                logits = model(ids)
                probs = F.softmax(logits, dim=-1)[0]
            topk = probs.topk(min(args.top_k, len(id2label)))
            for prob, idx in zip(topk.values.tolist(), topk.indices.tolist()):
                print(f"{id2label[idx]:<14} {prob*100:5.1f}%")
            print()
    except (KeyboardInterrupt, EOFError):
        print("\nbye.")

if __name__ == "__main__":
    main()

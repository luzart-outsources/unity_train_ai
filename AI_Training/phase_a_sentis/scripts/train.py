from __future__ import annotations
import argparse
import json
import time
from pathlib import Path

import pandas as pd
import torch
import torch.nn as nn
from sklearn.model_selection import train_test_split
from torch.utils.data import DataLoader

# Allow running from project root or scripts/
import sys
sys.path.insert(0, str(Path(__file__).parent))
from dataset import IntentDataset, build_vocab, load_csv, save_json
from model import build_model

ROOT = Path(__file__).resolve().parent.parent  # phase_a_sentis/
DATA_PATH = ROOT / "data" / "intents.csv"
MODEL_DIR = ROOT / "models"

def evaluate(model: nn.Module, loader: DataLoader, device: str) -> tuple[float, float]:
    model.eval()
    total_loss, correct, total = 0.0, 0, 0
    crit = nn.CrossEntropyLoss(reduction="sum")
    with torch.no_grad():
        for x, y in loader:
            x, y = x.to(device), y.to(device)
            logits = model(x)
            total_loss += crit(logits, y).item()
            correct += (logits.argmax(dim=1) == y).sum().item()
            total += y.size(0)
    return total_loss / max(total, 1), correct / max(total, 1)

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--arch", default="fasttext", choices=["fasttext", "lstm", "transformer"])
    p.add_argument("--epochs", type=int, default=20)
    p.add_argument("--batch", type=int, default=16)
    p.add_argument("--lr", type=float, default=3e-3)
    p.add_argument("--max_len", type=int, default=32)
    p.add_argument("--seed", type=int, default=42)
    p.add_argument("--data", default=str(DATA_PATH))
    p.add_argument("--tag", default="", help="suffix for output filenames (e.g. 'v4' -> lstm_v4_best.pt)")
    p.add_argument("--vocab_size", type=int, default=10000)
    args = p.parse_args()
    tag = ("_" + args.tag) if args.tag else ""

    torch.manual_seed(args.seed)
    device = "cuda" if torch.cuda.is_available() else "cpu"
    print(f"[setup] device={device} arch={args.arch} epochs={args.epochs}")

    df = load_csv(args.data)
    print(f"[data ] total samples: {len(df)}")
    print(f"[data ] per intent  : {df['intent'].value_counts().to_dict()}")

    train_df, val_df = train_test_split(df, test_size=0.2, stratify=df["intent"], random_state=args.seed)
    vocab = build_vocab(train_df["text"].tolist(), max_size=args.vocab_size)
    labels = sorted(df["intent"].unique())
    label2id = {l: i for i, l in enumerate(labels)}
    id2label = {i: l for l, i in label2id.items()}
    print(f"[data ] vocab size : {len(vocab)} num_classes={len(labels)}")

    train_ds = IntentDataset(train_df, vocab, label2id, args.max_len)
    val_ds = IntentDataset(val_df, vocab, label2id, args.max_len)
    train_loader = DataLoader(train_ds, batch_size=args.batch, shuffle=True)
    val_loader = DataLoader(val_ds, batch_size=args.batch)

    model = build_model(args.arch, vocab_size=len(vocab), num_classes=len(labels)).to(device)
    n_params = sum(p.numel() for p in model.parameters())
    print(f"[model] {args.arch} params={n_params:,}")

    opt = torch.optim.Adam(model.parameters(), lr=args.lr)
    crit = nn.CrossEntropyLoss()

    MODEL_DIR.mkdir(parents=True, exist_ok=True)
    save_json(vocab, MODEL_DIR / f"vocab{tag}.json")
    save_json(label2id, MODEL_DIR / f"label2id{tag}.json")
    save_json(id2label, MODEL_DIR / f"id2label{tag}.json")

    best_acc, log = 0.0, []
    t0 = time.time()
    for epoch in range(1, args.epochs + 1):
        model.train()
        running = 0.0
        for x, y in train_loader:
            x, y = x.to(device), y.to(device)
            opt.zero_grad()
            loss = crit(model(x), y)
            loss.backward()
            opt.step()
            running += loss.item() * y.size(0)
        train_loss = running / len(train_ds)
        val_loss, val_acc = evaluate(model, val_loader, device)
        log.append({"epoch": epoch, "train_loss": train_loss, "val_loss": val_loss, "val_acc": val_acc})
        flag = ""
        if val_acc > best_acc:
            best_acc = val_acc
            torch.save({
                "state_dict": model.state_dict(),
                "arch": args.arch,
                "vocab_size": len(vocab),
                "num_classes": len(labels),
                "max_len": args.max_len,
            }, MODEL_DIR / f"{args.arch}{tag}_best.pt")
            flag = "  [SAVED]"
        print(f"[epoch {epoch:>3}] train_loss={train_loss:.4f} val_loss={val_loss:.4f} val_acc={val_acc:.4f}{flag}")

    save_json({"log": log, "best_val_acc": best_acc, "elapsed_sec": time.time() - t0}, MODEL_DIR / f"training_log{tag}.json")
    print(f"\n[done] best val_acc = {best_acc:.4f} elapsed = {time.time()-t0:.1f}s")
    print(f"[done] checkpoint   : {MODEL_DIR / f'{args.arch}{tag}_best.pt'}")

if __name__ == "__main__":
    main()

"""
Eval any tagged student checkpoint on the 49-case hardset.

Usage:
  python eval_student_versioned.py --tag v2
"""
import argparse
import json
from pathlib import Path
from collections import defaultdict

import numpy as np
import torch

from train_student_encoder import StudentEncoder, encode_text, MAX_LEN
from eval_hardset import CASES, case_passes

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
META_PATH  = MODELS_DIR / "qa_metadata.json"  # uses base bank meta


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag", required=True)
    ap.add_argument("--vocab")    # optional override
    ap.add_argument("--bank")     # optional alt bank
    args = ap.parse_args()

    pt    = MODELS_DIR / f"student_encoder_{args.tag}.pt"
    vocab_path = MODELS_DIR / (args.vocab or f"vocab_phase_c_{args.tag}.json")
    if not pt.exists():
        raise SystemExit(f"missing {pt}")
    if not vocab_path.exists():
        # fall back to default vocab
        vocab_path = MODELS_DIR / "vocab_phase_c.json"
        print(f"[eval-{args.tag}] vocab fallback: {vocab_path}")

    ckpt = torch.load(pt, map_location="cpu", weights_only=True)
    model = StudentEncoder(
        vocab_size=ckpt["vocab_size"], emb_dim=ckpt["emb_dim"],
        out_dim=ckpt["out_dim"], n_layers=ckpt["n_layers"],
        n_heads=ckpt["n_heads"], ffn_dim=ckpt["ffn_dim"],
        max_len=ckpt["max_len"], dropout=ckpt["dropout"],
    )
    model.load_state_dict(ckpt["state_dict"])
    model.train(False)

    vocab = json.loads(vocab_path.read_text(encoding="utf-8"))
    meta  = json.loads(META_PATH.read_text(encoding="utf-8"))

    # Re-embed the bank with this student.
    print(f"[eval-{args.tag}] embedding {len(meta):,} bank items...")
    BATCH = 64
    N = len(meta)
    bank = np.zeros((N, ckpt["out_dim"]), dtype=np.float32)
    with torch.no_grad():
        for i in range(0, N, BATCH):
            ids = np.stack([
                np.array(encode_text(m["question"], vocab, MAX_LEN), dtype=np.int64)
                for m in meta[i:i + BATCH]
            ])
            bank[i:i + ids.shape[0]] = model(torch.from_numpy(ids)).numpy()

    def retrieve(q, k=3):
        ids = np.array([encode_text(q, vocab, MAX_LEN)], dtype=np.int64)
        with torch.no_grad():
            v = model(torch.from_numpy(ids)).numpy()[0]
        sims = bank @ v
        idx = np.argsort(-sims)[:k]
        return [(float(sims[i]), meta[i]) for i in idx]

    HIGH_TH = 0.88  # must match EmbeddingChatBrain.cs production threshold

    results = []
    by_cat = defaultdict(list)
    print(f"\n[eval-{args.tag}] {len(CASES)} cases  (ORIGINAL_BUG strict: score < {HIGH_TH})\n")
    for i, (q, ei, ee, ep, cat) in enumerate(CASES, 1):
        hits = retrieve(q, k=3)
        top_score, top = hits[0]
        if ei is None and cat == "ORIGINAL_BUG":
            # Want low confidence — top score must NOT exceed production HIGH threshold
            ok = top_score < HIGH_TH
        elif ei is None:
            ok = True
        else:
            ok = case_passes(top_score, top, ei, ee, ep)
        results.append({
            "case_num": i, "query": q, "category": cat,
            "expected_intent": ei, "expected_entity": ee,
            "top_intent": top["intent"], "top_entity": top["entityId"],
            "top_score": top_score, "passed": ok,
        })
        by_cat[cat].append(ok)
        flag = "OK" if ok else "FAIL"
        print(f"  [{flag:4s}] {cat:14s} score={top_score:.2f} | {top['intent']:18s} | {top['entityId']:15s} | {q}")
        if not ok and ei is not None:
            print(f"          expected {ei} {ee}; got {top['intent']} {top['entityId']}")

    # Strict: ORIGINAL_BUG cases also count (must fall below HIGH_TH).
    n_total  = sum(1 for _, ei, _, _, cat in CASES if ei is not None or cat == "ORIGINAL_BUG")
    n_passed = sum(1 for r in results if r["passed"]
                    and (r["expected_intent"] is not None or r["category"] == "ORIGINAL_BUG"))
    print(f"\n[eval-{args.tag}] OVERALL {n_passed}/{n_total} = {n_passed/n_total*100:.1f}%")
    for cat, oks in sorted(by_cat.items()):
        n = sum(1 for ok in oks if ok)
        print(f"  {cat:14s} {n}/{len(oks)} = {n/len(oks)*100:.1f}%")

    out = MODELS_DIR / f"eval_hardset_{args.tag}.json"
    out.write_text(json.dumps({
        "tag": args.tag, "model": str(pt),
        "overall": {"passed": n_passed, "total": n_total, "acc": n_passed/n_total},
        "byCategory": {c: {"passed": sum(1 for ok in oks if ok), "total": len(oks)} for c, oks in by_cat.items()},
        "results": results,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"\nwrote {out}")


if __name__ == "__main__":
    main()

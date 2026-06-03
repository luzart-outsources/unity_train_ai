"""
Phase C — Evaluate the distilled student encoder on the 49-case hardset.

Mirrors eval_hardset.py but loads the PyTorch student + Phase A tokenizer
rather than the SentenceTransformer teacher. Pre-computes the bank with
the student so cosine similarity is symmetric.
"""
import json
from pathlib import Path
from collections import defaultdict

import numpy as np
import torch

from train_student_encoder import StudentEncoder, encode_text, MAX_LEN
from eval_hardset import CASES, case_passes

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"

STUDENT_PT = MODELS_DIR / "student_encoder.pt"
VOCAB_PATH = MODELS_DIR / "vocab_phase_c.json"
META_PATH  = MODELS_DIR / "qa_metadata.json"


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
    meta = json.loads(META_PATH.read_text(encoding="utf-8"))

    # Re-embed the bank with the student.
    print(f"[eval-student] embedding bank ({len(meta):,}) ...")
    N = len(meta)
    bank = np.zeros((N, ckpt["out_dim"]), dtype=np.float32)
    BATCH = 64
    with torch.no_grad():
        for i in range(0, N, BATCH):
            ids = np.stack([
                np.array(encode_text(m["question"], vocab, MAX_LEN), dtype=np.int64)
                for m in meta[i:i + BATCH]
            ])
            t = torch.from_numpy(ids)
            bank[i:i + ids.shape[0]] = model(t).numpy()

    def retrieve(query: str, k: int = 5):
        ids = np.array([encode_text(query, vocab, MAX_LEN)], dtype=np.int64)
        with torch.no_grad():
            q = model(torch.from_numpy(ids)).numpy()[0]
        sims = bank @ q
        idx = np.argsort(-sims)[:k]
        return [(float(sims[i]), meta[i]) for i in idx]

    results = []
    by_cat = defaultdict(list)
    print(f"\n[eval-student] running {len(CASES)} cases ...\n")
    for i, (q, ei, ee, ep, cat) in enumerate(CASES, 1):
        hits = retrieve(q, k=3)
        top_score, top = hits[0]
        ok = case_passes(top_score, top, ei, ee, ep) if ei is not None else True
        results.append({
            "case_num": i, "query": q, "category": cat,
            "expected_intent": ei, "expected_entity": ee, "expected_phrase": ep,
            "top_intent": top["intent"], "top_entity": top["entityId"],
            "top_score": top_score, "top_answer": top["answer"],
            "passed": ok,
        })
        by_cat[cat].append(ok)
        flag = "OK" if ok else "FAIL"
        print(f"  [{flag:4s}] {cat:14s} score={top_score:.2f} | {top['intent']:18s} | {top['entityId']:15s} | Q: {q}")
        if not ok:
            print(f"          expected intent={ei} entity={ee} phrase={ep}")
            print(f"          got      intent={top['intent']} entity={top['entityId']}")
            print(f"          answer  : {top['answer']}")

    n_total  = sum(1 for _, ei, *_ in CASES if ei is not None)
    n_passed = sum(1 for r in results if r["passed"] and r["expected_intent"] is not None)
    print(f"\n[eval-student] OVERALL: {n_passed}/{n_total} = {n_passed/n_total*100:.1f}%")
    print("[eval-student] By category:")
    for cat, oks in sorted(by_cat.items()):
        n = sum(1 for ok in oks if ok)
        print(f"  {cat:14s} {n}/{len(oks)} = {n/len(oks)*100:.1f}%")

    out_path = MODELS_DIR / "eval_hardset_student.json"
    out_path.write_text(json.dumps({
        "model":    "student_encoder.pt",
        "overall":  {"passed": n_passed, "total": n_total, "acc": n_passed / n_total},
        "byCategory": {cat: {"passed": sum(1 for ok in oks if ok), "total": len(oks)} for cat, oks in by_cat.items()},
        "results":  results,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"\n[eval-student] wrote {out_path}")


if __name__ == "__main__":
    main()

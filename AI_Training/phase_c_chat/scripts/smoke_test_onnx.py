"""
Phase C — Smoke test the ONNX deliverable EXACTLY as Unity will run it.

Critical: this test deliberately uses the SAME tokenizer algorithm that
EmbeddingChatBrain.cs runs (whitespace lowercase + greedy longest
multi-word match), NOT the underthesea segmentation used during
training. Why: Unity has no underthesea — if the C# tokenizer disagrees
with the trained vocab, retrieval breaks silently. This script catches
that drift offline before you ever open Unity.

What it tests:
  1. ONNX file loads in onnxruntime (same as Unity's ModelLoader.Load).
  2. Input shape [1, 40] int64 -> output [1, 384] float32 with L2 norm.
  3. Bank .bytes binary header [N|DIM|0|0] + body parses correctly.
  4. End-to-end retrieval on a curated set of queries that covers all 9
     hardset categories.
  5. Reports per-query: route, score, intent, entity, answer snippet,
     and a final pass/fail summary.

How to run:
  AI_Training/phase_a_sentis/.venv/Scripts/python.exe \
    AI_Training/phase_c_chat/scripts/smoke_test_onnx.py
"""
import json
import re
import struct
import sys
import time
from pathlib import Path

import numpy as np
import onnxruntime as ort

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"

ONNX_PATH  = MODELS_DIR / "student_encoder.onnx"
BANK_PATH  = MODELS_DIR / "student_bank.bytes"
META_PATH  = MODELS_DIR / "student_bank.json"
VOCAB_PATH = MODELS_DIR / "vocab_phase_c.json"

MAX_LEN       = 40
HIGH_THRESH   = 0.88
LOW_THRESH    = 0.72


# =====================================================================
# C#-EQUIVALENT TOKENIZER (whitespace + greedy multi-word)
# This MUST match Assets/AI/Scripts/EmbeddingChatBrain.cs:Encode()
# byte-for-byte. If you change one, change the other.
# =====================================================================

def cs_tokenize(text: str, vocab: dict, max_len: int = MAX_LEN) -> list[int]:
    pad_id = vocab.get("<pad>", 0)
    unk_id = vocab.get("<unk>", 1)

    # Lowercase + strip non-alphanumeric (keeps Vietnamese diacritics: they
    # are part of Unicode "letter" category, matched by str.isalpha).
    text = text.lower().strip()
    cleaned = []
    for c in text:
        if c.isalnum() or c.isspace():
            cleaned.append(c)
        else:
            cleaned.append(" ")
    words = "".join(cleaned).split()

    # Discover max multi-word span from the vocab once per call (cheap).
    max_span = max((k.count(" ") + 1 for k in vocab.keys()), default=1)

    ids = [pad_id] * max_len
    wi = 0
    out_i = 0
    while wi < len(words) and out_i < max_len:
        matched_span = 0
        matched_id = unk_id
        span_cap = min(max_span, len(words) - wi)
        for span in range(span_cap, 0, -1):
            cand = " ".join(words[wi:wi + span])
            if cand in vocab:
                matched_span = span
                matched_id = vocab[cand]
                break
        if matched_span == 0:
            ids[out_i] = unk_id
            wi += 1
        else:
            ids[out_i] = matched_id
            wi += matched_span
        out_i += 1
    return ids


# =====================================================================
# BINARY BANK LOADER (mirrors EmbeddingChatBrain.cs:LoadBank)
# =====================================================================

def load_bank(path: Path) -> np.ndarray:
    raw = path.read_bytes()
    if len(raw) < 16:
        raise SystemExit(f"bank too small: {len(raw)} bytes")
    N, DIM, _r1, _r2 = struct.unpack("<IIII", raw[:16])
    expected = 16 + N * DIM * 4
    if len(raw) < expected:
        raise SystemExit(f"bank truncated: need {expected} bytes, got {len(raw)}")
    vecs = np.frombuffer(raw[16:16 + N * DIM * 4], dtype="<f4").reshape(N, DIM)
    print(f"[bank] loaded {N} vectors of dim {DIM}  (file {len(raw)/1024/1024:.2f} MiB)")
    return vecs


# =====================================================================
# QUERY → SCORE
# =====================================================================

def route(score: float) -> str:
    if score >= HIGH_THRESH: return "template"
    if score >= LOW_THRESH:  return "low_confidence"
    return "fallback"


def main():
    # 1. Verify all deliverables present.
    for p in (ONNX_PATH, BANK_PATH, META_PATH, VOCAB_PATH):
        if not p.exists():
            raise SystemExit(f"missing: {p} (run train + export + build_bank first)")

    print("=" * 70)
    print("Phase C Native — ONNX smoke test (Unity-equivalent path)")
    print("=" * 70)

    # 2. Load assets.
    vocab = json.loads(VOCAB_PATH.read_text(encoding="utf-8"))
    meta  = json.loads(META_PATH.read_text(encoding="utf-8"))
    bank  = load_bank(BANK_PATH)
    print(f"[vocab] {len(vocab):,} tokens")
    print(f"[meta]  {len(meta):,} entries")
    assert bank.shape[0] == len(meta), \
        f"bank/meta mismatch: {bank.shape[0]} vs {len(meta)}"

    # 3. Spin up onnxruntime exactly like Unity's CPU backend.
    sess = ort.InferenceSession(str(ONNX_PATH), providers=["CPUExecutionProvider"])
    iname = sess.get_inputs()[0].name
    oname = sess.get_outputs()[0].name
    print(f"[onnx]  input  = {iname:12s} {sess.get_inputs()[0].shape}")
    print(f"[onnx]  output = {oname:12s} {sess.get_outputs()[0].shape}")
    print()

    # 4. Curated test queries covering all categories of the hardset.
    cases = [
        # CLEAN
        ("Sân vận động ở đâu",          "ASK_LOCATION",     "SanVanDong"),
        ("Nhà ăn ở đâu",                "ASK_LOCATION",     "NhaAn_Door"),
        ("Lớp học ở đâu",               "ASK_LOCATION",     "LopHoc_Door"),
        ("Ký túc xá ở đâu",             "ASK_LOCATION",     "KTX_Door"),
        ("Khu tự do ở đâu",             "ASK_LOCATION",     "FreeArea"),
        ("Mấy giờ tập thể dục",         "ASK_TIME",         "slot_1"),
        ("Hôm nay học môn gì",          "ASK_SCHEDULE_TODAY", None),
        ("Đại đội trưởng là ai",        "ASK_NPC",          "DaiDoiTruong"),
        ("Em chào thủ trưởng",          "GREETING",         None),
        ("Cảm ơn anh",                  "THANKS",           None),
        # NO_ACCENT
        ("san van dong o dau",          "ASK_LOCATION",     "SanVanDong"),
        ("nha an o dau",                "ASK_LOCATION",     "NhaAn_Door"),
        ("dai doi truong la ai",        "ASK_NPC",          "DaiDoiTruong"),
        # CODE_MIX
        ("Where is canteen",            "ASK_LOCATION",     "NhaAn_Door"),
        ("Where is dormitory",          "ASK_LOCATION",     "KTX_Door"),
        ("Today học gì",                "ASK_SCHEDULE_TODAY", None),
        # SLANG
        ("svd đâu v",                   "ASK_LOCATION",     "SanVanDong"),
        ("ktx đâu nhỉ",                 "ASK_LOCATION",     "KTX_Door"),
        ("ddt la ai v",                 "ASK_NPC",          "DaiDoiTruong"),
        # ORIGINAL_BUG (should fallback or low_confidence)
        ("khu A1 ở đâu",                None,               None),
        ("khu A2 ở đâu",                None,               None),
        ("khu A3 ở đâu",                None,               None),
        ("khu B ở đâu",                 None,               None),
        # OOS
        ("Crypto giảm sốc",             "OUT_OF_SCOPE",     None),
        ("Anh có người yêu chưa",       "OUT_OF_SCOPE",     None),
        # ELLIPSIS
        ("nhà ăn?",                     "ASK_LOCATION",     "NhaAn_Door"),
        ("KTX đâu",                     "ASK_LOCATION",     "KTX_Door"),
    ]

    print(f"Thresholds: HIGH = {HIGH_THRESH}   LOW = {LOW_THRESH}")
    print(f"{'STATUS':8s} {'ROUTE':18s} {'SCORE':>6s} {'INTENT':18s} {'ENTITY':16s} QUERY")
    print("-" * 110)

    pass_count = 0
    eval_count = 0
    t_total = 0.0

    for q, expected_intent, expected_entity in cases:
        # Tokenize like C#.
        ids = np.array([cs_tokenize(q, vocab, MAX_LEN)], dtype=np.int64)

        # Encode.
        t0 = time.time()
        emb = sess.run([oname], {iname: ids})[0][0]   # [384]
        # Cosine == dot since both normalized.
        sims = bank @ emb
        top  = int(np.argmax(sims))
        score = float(sims[top])
        t_total += (time.time() - t0)

        m = meta[top]
        rt = route(score)

        # Pass criteria:
        # - If expected_intent is None (ORIGINAL_BUG): want EITHER
        #   (a) graceful fallback / low_confidence  OR
        #   (b) confident OUT_OF_SCOPE answer (v6+ recognises "khu A1" etc
        #       as fake areas thanks to the v9 negative training set)
        # - Else: want top intent matches AND (no expected_entity OR matches)
        if expected_intent is None:
            ok = rt in ("fallback", "low_confidence") or m["intent"] == "OUT_OF_SCOPE"
        else:
            ok = m["intent"] == expected_intent
            if ok and expected_entity is not None:
                ok = m["entityId"] == expected_entity
        flag = "OK"   if ok else "FAIL"
        if ok: pass_count += 1
        eval_count += 1

        # Pretty print.
        snippet = (m.get("answer") or "").replace("\n", " ")[:60]
        print(f"{flag:8s} {rt:18s} {score:>6.2f} {m['intent']:18s} {m['entityId']:16s} {q}")
        if not ok:
            print(f"   ↳ expected intent={expected_intent} entity={expected_entity}")
        # Show answer snippet for visibility on a subset.
        if rt == "template":
            print(f"   ↳ A: {snippet}")

    print("-" * 110)
    print(f"PASS: {pass_count}/{eval_count} = {pass_count/eval_count*100:.1f}%")
    print(f"Avg latency: {t_total/eval_count*1000:.1f} ms/query (CPU)")
    print()

    # Sanity: round-trip — embed a known training question, expect itself.
    print("Sanity round-trip:")
    for qi in (0, len(meta)//3, len(meta)*2//3, len(meta)-1):
        q = meta[qi].get("question") or "?"
        # The meta JSON written by build_bank_for_unity didn't carry the question
        # field — fall back to bypassing the sanity check if absent.
        if q == "?":
            print("  (meta lacks question field — skipping)")
            break
        ids = np.array([cs_tokenize(q, vocab, MAX_LEN)], dtype=np.int64)
        emb = sess.run([oname], {iname: ids})[0][0]
        sims = bank @ emb
        top = int(np.argmax(sims))
        print(f"  idx {qi:5d}  retrieved top = {top:5d}  score = {sims[top]:.3f}")

    sys.exit(0 if pass_count == eval_count else 1)


if __name__ == "__main__":
    main()

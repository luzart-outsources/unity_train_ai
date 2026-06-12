"""
Reference inference pipeline for the Phase C NPC chat deliverable.

Single-file, self-contained, 100 LOC. Reads everything from the same
directory as this script. Run interactively:

    python reference_inference.py

Then type Vietnamese queries. Type 'exit' to leave.

Dependencies:
    pip install onnxruntime numpy
"""
import json
import struct
import sys
from pathlib import Path

import numpy as np
import onnxruntime as ort

HERE = Path(__file__).resolve().parent

MAX_LEN     = 40
HIGH_THRESH = 0.88
LOW_THRESH  = 0.72
FALLBACK = (
    "Tôi chưa hiểu rõ câu hỏi. Đồng chí có thể hỏi về: lịch hôm nay, giờ ăn, "
    "vị trí (sân vận động / nhà ăn / lớp học / ký túc xá / khu dọn vệ sinh / khu tự do), "
    "ba môn học, hoặc thông tin Đại đội trưởng."
)


def tokenize(text: str, vocab: dict) -> list[int]:
    pad_id, unk_id = vocab.get("<pad>", 0), vocab.get("<unk>", 1)
    text = text.lower().strip()
    cleaned = [c if c.isalnum() or c.isspace() else " " for c in text]
    words = "".join(cleaned).split()
    max_span = max((k.count(" ") + 1 for k in vocab), default=1)
    ids = [pad_id] * MAX_LEN
    wi = out_i = 0
    while wi < len(words) and out_i < MAX_LEN:
        matched_span = 0
        matched_id   = unk_id
        for span in range(min(max_span, len(words) - wi), 0, -1):
            cand = " ".join(words[wi:wi + span])
            if cand in vocab:
                matched_span, matched_id = span, vocab[cand]
                break
        ids[out_i] = matched_id
        wi  += matched_span if matched_span else 1
        out_i += 1
    return ids


def load_bank(path: Path) -> np.ndarray:
    raw = path.read_bytes()
    N, DIM, _, _ = struct.unpack("<IIII", raw[:16])
    return np.frombuffer(raw[16:16 + N * DIM * 4], dtype="<f4").reshape(N, DIM)


def schedule_today(day: int, game: dict) -> str:
    lines = []
    for slot_str in sorted(game["routine"].keys(), key=int):
        info = game["routine"][slot_str]
        area = game["areas"].get(info["areaId"] or "", {}).get("displayName", info["areaId"] or "?")
        subj = ""
        if info.get("subjects"):
            idx = (day - 1 + (int(slot_str) - 4)) % len(info["subjects"])
            sid = info["subjects"][idx]
            subj = " (" + game["subjects"][sid]["displayName"] + ")"
        lines.append(f"  {info['startTime']}-{info['endTime']}: {info['displayVi']}{subj} @ {area}")
    return f"Lịch ngày {day}:\n" + "\n".join(lines)


def main(current_day: int = 1):
    print(f"Loading from {HERE} ...")
    vocab = json.loads((HERE / "vocab_phase_c.json").read_text(encoding="utf-8"))
    meta  = json.loads((HERE / "student_bank.json").read_text(encoding="utf-8"))
    game  = json.loads((HERE / "game_entities.json").read_text(encoding="utf-8"))
    bank  = load_bank(HERE / "student_bank.bytes")
    sess  = ort.InferenceSession(str(HERE / "student_encoder.onnx"),
                                 providers=["CPUExecutionProvider"])
    iname = sess.get_inputs()[0].name
    oname = sess.get_outputs()[0].name
    print(f"  vocab = {len(vocab):,}   bank = {bank.shape[0]:,} x {bank.shape[1]}")
    print(f"Day = {current_day}. Type Vietnamese, or 'exit'.\n")

    while True:
        try:
            line = input("> ").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break
        if not line:
            continue
        if line in ("exit", "quit", "q"):
            break

        ids = np.array([tokenize(line, vocab)], dtype=np.int64)
        emb = sess.run([oname], {iname: ids})[0][0]
        sims = bank @ emb
        top  = int(np.argmax(sims))
        score = float(sims[top])
        top_meta = meta[top]

        if score >= HIGH_THRESH:
            answer = top_meta["answer"]
            route  = "template"
        else:
            answer = FALLBACK
            route  = "low_confidence" if score >= LOW_THRESH else "fallback"

        if top_meta.get("needsGameState") and "{__SCHEDULE_TODAY__}" in answer:
            answer = answer.replace("{__SCHEDULE_TODAY__}", schedule_today(current_day, game))

        print(f"  [{route} score={score:.2f} intent={top_meta['intent']} entity={top_meta['entityId']}]")
        for ln in answer.split("\n"):
            print(f"  A: {ln}")
        print()


if __name__ == "__main__":
    day = int(sys.argv[1]) if len(sys.argv) > 1 else 1
    main(day)

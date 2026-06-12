"""
Phase C — Interactive REPL for typing your own Vietnamese queries
against the ONNX student encoder + bank.

Usage:
  python AI_Training/phase_c_chat/scripts/chat_repl.py
  > Sân vận động ở đâu
  > khu A1 ở đâu
  > svd đâu v
  > exit

Each turn shows:
  - route (template / low_confidence / fallback)
  - top-3 retrieved Q&A with scores
  - canonical answer for top-1 (resolves {__SCHEDULE_TODAY__} if present)

Special commands:
  exit / quit / q   -> leave
  !day <N>          -> set current_day (for ASK_SCHEDULE_TODAY queries)
  !th <H> <L>       -> tune thresholds (high low)
  !top <K>          -> show top-K hits (default 3)
  !who              -> print model info (vocab size, bank size)
  !sim <A> | <B>    -> show cosine(A, B) between two phrases
  (empty line)      -> noop
"""
import json
import struct
import sys
from pathlib import Path

import numpy as np
import onnxruntime as ort

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
DATA_DIR   = ROOT / "data"

ONNX_PATH  = MODELS_DIR / "student_encoder.onnx"
BANK_PATH  = MODELS_DIR / "student_bank.bytes"
META_PATH  = MODELS_DIR / "student_bank.json"
VOCAB_PATH = MODELS_DIR / "vocab_phase_c.json"
GAME_PATH  = DATA_DIR / "game_entities.json"

MAX_LEN     = 40
HIGH_THRESH = 0.88
LOW_THRESH  = 0.72


# =====================================================================
# Tokenizer (matches EmbeddingChatBrain.cs)
# =====================================================================

def cs_tokenize(text: str, vocab: dict, max_len: int = MAX_LEN) -> list[int]:
    pad_id = vocab.get("<pad>", 0)
    unk_id = vocab.get("<unk>", 1)
    text = text.lower().strip()
    cleaned = []
    for c in text:
        if c.isalnum() or c.isspace(): cleaned.append(c)
        else: cleaned.append(" ")
    words = "".join(cleaned).split()

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
            ids[out_i] = unk_id; wi += 1
        else:
            ids[out_i] = matched_id; wi += matched_span
        out_i += 1
    return ids


def load_bank(path: Path) -> np.ndarray:
    raw = path.read_bytes()
    N, DIM, _r1, _r2 = struct.unpack("<IIII", raw[:16])
    return np.frombuffer(raw[16:16 + N * DIM * 4], dtype="<f4").reshape(N, DIM)


def resolve_schedule_today(answer: str, day: int, game: dict) -> str:
    """Substitute {__SCHEDULE_TODAY__} with a generated routine summary."""
    if "{__SCHEDULE_TODAY__}" not in answer:
        return answer
    routine = game["routine"]
    lines = []
    for slot_str in sorted(routine.keys(), key=int):
        info = routine[slot_str]
        area_id = info["areaId"] or ""
        area_name = game["areas"].get(area_id, {}).get("displayName", area_id or "?")
        subj = ""
        if info.get("subjects"):
            idx = (day - 1 + (int(slot_str) - 4)) % len(info["subjects"])
            sid = info["subjects"][idx]
            subj = " (" + game["subjects"][sid]["displayName"] + ")"
        lines.append(f"  {info['startTime']}-{info['endTime']}: {info['displayVi']}{subj} @ {area_name}")
    summary = f"Lịch ngày {day}:\n" + "\n".join(lines)
    return answer.replace("{__SCHEDULE_TODAY__}", summary)


# =====================================================================
# Pretty colors (ANSI)
# =====================================================================
class C:
    RESET  = "\033[0m"
    DIM    = "\033[2m"
    BOLD   = "\033[1m"
    GREEN  = "\033[32m"
    YELLOW = "\033[33m"
    RED    = "\033[31m"
    BLUE   = "\033[34m"
    CYAN   = "\033[36m"
    MAG    = "\033[35m"

    @staticmethod
    def route_color(route: str) -> str:
        if route == "template":       return C.GREEN
        if route == "low_confidence": return C.YELLOW
        return C.RED


# =====================================================================
# REPL
# =====================================================================

def main():
    # Enable ANSI on Windows.
    if sys.platform == "win32":
        try:
            import ctypes
            kernel32 = ctypes.windll.kernel32
            kernel32.SetConsoleMode(kernel32.GetStdHandle(-11), 7)
        except Exception:
            pass

    for p in (ONNX_PATH, BANK_PATH, META_PATH, VOCAB_PATH):
        if not p.exists():
            sys.exit(f"missing: {p}")

    print(f"{C.BOLD}Phase C Native — Interactive REPL{C.RESET}")
    print(f"{C.DIM}Loading...{C.RESET}")

    vocab = json.loads(VOCAB_PATH.read_text(encoding="utf-8"))
    meta  = json.loads(META_PATH.read_text(encoding="utf-8"))
    bank  = load_bank(BANK_PATH)
    game  = json.loads(GAME_PATH.read_text(encoding="utf-8")) if GAME_PATH.exists() else {}

    sess = ort.InferenceSession(str(ONNX_PATH), providers=["CPUExecutionProvider"])
    iname = sess.get_inputs()[0].name
    oname = sess.get_outputs()[0].name

    # Mutable session state
    state = {
        "high": HIGH_THRESH,
        "low":  LOW_THRESH,
        "topk": 3,
        "day":  1,
    }

    print(f"  ONNX     : {ONNX_PATH.name}")
    print(f"  Vocab    : {len(vocab):,} tokens")
    print(f"  Bank     : {bank.shape[0]:,} vectors x {bank.shape[1]}-dim")
    print(f"  Day      : {state['day']}")
    print(f"  Threshold: high={state['high']}, low={state['low']}")
    print()
    print(f"{C.DIM}Commands: !day <N>  !th <H> <L>  !top <K>  !sim <A> | <B>  !who  exit{C.RESET}")
    print(f"{C.DIM}Type a Vietnamese question and press Enter.{C.RESET}")
    print()

    def encode(text: str) -> np.ndarray:
        ids = np.array([cs_tokenize(text, vocab, MAX_LEN)], dtype=np.int64)
        return sess.run([oname], {iname: ids})[0][0]

    def route_of(score: float) -> str:
        if score >= state["high"]: return "template"
        if score >= state["low"]:  return "low_confidence"
        return "fallback"

    fallback_answer = (
        "Tôi chưa hiểu rõ câu hỏi. Đồng chí có thể hỏi về: lịch hôm nay, giờ ăn, "
        "vị trí (sân vận động / nhà ăn / lớp học / ký túc xá / khu dọn vệ sinh / khu tự do), "
        "ba môn học, hoặc thông tin Đại đội trưởng."
    )

    while True:
        try:
            line = input(f"{C.CYAN}> {C.RESET}").strip()
        except (EOFError, KeyboardInterrupt):
            print()
            break

        if not line:
            continue

        # Commands
        if line in ("exit", "quit", "q", ":q"):
            break
        if line.startswith("!day"):
            try:
                state["day"] = int(line.split()[1])
                print(f"{C.DIM}  day -> {state['day']}{C.RESET}")
            except (IndexError, ValueError):
                print(f"{C.RED}  usage: !day <N>{C.RESET}")
            continue
        if line.startswith("!th"):
            try:
                _, h, l = line.split()
                state["high"], state["low"] = float(h), float(l)
                print(f"{C.DIM}  thresholds -> high={state['high']}, low={state['low']}{C.RESET}")
            except (ValueError, IndexError):
                print(f"{C.RED}  usage: !th <high> <low>{C.RESET}")
            continue
        if line.startswith("!top"):
            try:
                state["topk"] = int(line.split()[1])
                print(f"{C.DIM}  topk -> {state['topk']}{C.RESET}")
            except (IndexError, ValueError):
                print(f"{C.RED}  usage: !top <K>{C.RESET}")
            continue
        if line.startswith("!sim"):
            try:
                rest = line[4:].strip()
                a, b = [s.strip() for s in rest.split("|", 1)]
                va, vb = encode(a), encode(b)
                cos = float(np.dot(va, vb))
                color = C.GREEN if cos >= 0.88 else (C.YELLOW if cos >= 0.72 else C.RED)
                print(f"  {color}cos = {cos:.3f}{C.RESET}    {a!r}  vs  {b!r}")
            except Exception as e:
                print(f"{C.RED}  usage: !sim <phrase A> | <phrase B>  ({e}){C.RESET}")
            continue
        if line == "!who":
            print(f"  ONNX={ONNX_PATH.name}  vocab={len(vocab):,}  bank={bank.shape[0]:,}x{bank.shape[1]}")
            print(f"  day={state['day']} high={state['high']} low={state['low']} topk={state['topk']}")
            continue

        # --- Actual query ---
        import time
        t0 = time.time()
        q = encode(line)
        sims = bank @ q
        dt_ms = (time.time() - t0) * 1000

        topk_idx = np.argsort(-sims)[:state["topk"]]
        top = meta[topk_idx[0]]
        top_score = float(sims[topk_idx[0]])
        rt = route_of(top_score)
        rc = C.route_color(rt)

        # Print top-K
        print(f"  {C.DIM}top-{state['topk']}:{C.RESET}")
        for rank, i in enumerate(topk_idx, 1):
            m = meta[i]
            s = float(sims[i])
            star = f"{C.BOLD}*{C.RESET}" if rank == 1 else " "
            sc_col = (C.GREEN if s >= state["high"] else
                      C.YELLOW if s >= state["low"]  else C.RED)
            print(f"   {star} #{rank} {sc_col}{s:.3f}{C.RESET}  "
                  f"[{C.MAG}{m['intent']:18s}{C.RESET}|{C.BLUE}{m['entityId']:15s}{C.RESET}]  "
                  f"{m['question']}")

        # Decide reply (matches EmbeddingChatBrain.cs default
        # surfaceLowConfidenceCandidate=false behaviour).
        if rt == "template":
            ans = top["answer"]
        elif rt == "low_confidence":
            ans = fallback_answer   # play safe — match Unity offline default
        else:
            ans = fallback_answer

        if top.get("needsGameState"):
            ans = resolve_schedule_today(ans, state["day"], game)

        print(f"  {rc}route = {rt}{C.RESET}    score = {top_score:.2f}    {C.DIM}({dt_ms:.1f}ms){C.RESET}")
        # Pretty-print multi-line answer.
        for ln in ans.split("\n"):
            print(f"  {C.DIM}A:{C.RESET} {ln}")
        print()


if __name__ == "__main__":
    main()

from __future__ import annotations
import datetime as dt
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

# CONFIG — edit DEADLINE if user wants different stop time
DEADLINE = dt.datetime(2026, 5, 7, 19, 0, 0)  # 7 PM 7/5/2026 (~12h from 7 AM)

PHASE_A_EPOCHS = 25
PHASE_A_TARGET = 5000             # samples per intent -> 40k total (2.5x v3)
PHASE_A_ARCHS = ["lstm", "fasttext", "transformer"]   # LSTM first (proven winner)

PHASE_B_STEPS = 1_000_000         # 2x v3 (was 500k)
PHASE_B_DEVICE = "cpu"
PHASE_B_NET = "128,128"
PHASE_B_ENT = 0.01

MIN_TIME_FOR_HEAVY = 35 * 60      # 35 min: 1M PPO is ~25 min plus margin
MIN_TIME_FOR_LIGHT = 8 * 60       # 8 min: 40k gen + cached train
SPIN_GUARD_SLEEP = 30
PHASE_A_TRAIN_TIMEOUT = 1800      # 30 min

ROOT = Path(__file__).resolve().parent
PHASE_A = ROOT / "phase_a_sentis"
PHASE_B = ROOT / "phase_b_movement"
PYTHON = PHASE_A / ".venv/Scripts/python.exe"
DELIVERABLES = ROOT / "deliverables"
DELIVERABLES.mkdir(exist_ok=True)
LOG_PATH = ROOT / "overnight_v3.log"
STATE_PATH = ROOT / "overnight_v3_state.json"

env = os.environ.copy()
env["PYTHONIOENCODING"] = "utf-8"

def now() -> dt.datetime:
    return dt.datetime.now()

def time_left() -> float:
    return (DEADLINE - now()).total_seconds()

def log(msg: str) -> None:
    line = f"[{now().strftime('%H:%M:%S')}] {msg}"
    print(line, flush=True)
    with open(LOG_PATH, "a", encoding="utf-8") as f:
        f.write(line + "\n")

def save_state(state: dict) -> None:
    with open(STATE_PATH, "w", encoding="utf-8") as f:
        json.dump(state, f, ensure_ascii=False, indent=2)

def load_state() -> dict:
    if STATE_PATH.exists():
        with open(STATE_PATH, encoding="utf-8") as f:
            return json.load(f)
    return {
        "started_at": now().isoformat(),
        "iter": 0,
        # Phase A: track best per arch + overall (fasttext expected to win on this data)
        "best_phase_a": {"acc": 0.0, "iter": 0, "data_seed": None, "arch": None},
        "best_phase_a_per_arch": {a: {"acc": 0.0, "iter": 0, "data_seed": None}
                                  for a in PHASE_A_ARCHS},
        "best_phase_b": {"reward": -1e9, "iter": 0, "seed": None, "tag": None},
        "history": [],
    }

def run(cmd: list[str], cwd: Path | None = None, timeout: int | None = None) -> tuple[int, str]:
    try:
        p = subprocess.run(cmd, cwd=str(cwd) if cwd else None,
                           env=env, capture_output=True, text=True, timeout=timeout,
                           encoding="utf-8", errors="replace")
        out = (p.stdout or "") + (p.stderr or "")
        tail = "\n".join(out.strip().splitlines()[-30:])
        return p.returncode, tail
    except subprocess.TimeoutExpired:
        return -1, "TIMEOUT"
    except Exception as e:
        return -2, f"EXC {type(e).__name__}: {e}"

# Phase A iteration: regenerate v3 + train one arch + eval
def phase_a_iter(seed: int, arch: str, state: dict) -> dict:
    log(f"[A] iter — seed={seed} arch={arch} target={PHASE_A_TARGET}/intent")
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/generate_dataset_v3.py"),
                 "--seed", str(seed), "--per_intent", str(PHASE_A_TARGET),
                 "--out", "data/intents_v3.csv"],
                cwd=PHASE_A, timeout=240)
    if rc != 0:
        log(f"[A] generate failed rc={rc}")
        return {"ok": False, "stage": "generate"}

    # Train chosen arch — epochs depend on arch (Transformer needs more)
    epochs = 30 if arch == "fasttext" else (35 if arch == "lstm" else 40)
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/train.py"),
                 "--arch", arch, "--epochs", str(epochs),
                 "--data", "data/intents_v3.csv", "--seed", str(seed)],
                cwd=PHASE_A, timeout=PHASE_A_TRAIN_TIMEOUT)
    if rc != 0:
        log(f"[A] train {arch} failed rc={rc}")
        return {"ok": False, "stage": "train"}

    eval_path = PHASE_A / "models" / f"eval_iter{state['iter']}_{arch}.json"
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/eval_realworld.py"),
                 "--archs", arch, "--save", str(eval_path)],
                cwd=PHASE_A, timeout=120)
    if rc != 0 or not eval_path.exists():
        log(f"[A] eval failed rc={rc}")
        return {"ok": False, "stage": "eval"}

    with open(eval_path, encoding="utf-8") as f:
        ev = json.load(f)
    if arch not in ev:
        log(f"[A] no eval entry for {arch}")
        return {"ok": False, "stage": "eval_parse"}
    acc = float(ev[arch]["accuracy"])
    log(f"[A] {arch} real-world acc = {acc*100:.1f}% ({ev[arch]['correct']}/{ev[arch]['total']})")

    # Per-arch best
    if acc > state["best_phase_a_per_arch"][arch]["acc"]:
        state["best_phase_a_per_arch"][arch] = {
            "acc": acc, "iter": state["iter"], "data_seed": seed
        }
        # Export ONNX for this arch & save into deliverables under arch-prefixed name
        rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/export_onnx.py"),
                     "--arch", arch], cwd=PHASE_A, timeout=120)
        if rc == 0:
            for fname in (f"{arch}_intent.onnx", f"{arch}_intent_meta.json"):
                src = PHASE_A / "models" / fname
                if src.exists():
                    shutil.copy2(src, DELIVERABLES / fname)
            log(f"[A] {arch} new BEST -> deliverables/{arch}_intent.onnx")

    # Overall best across archs (any arch)
    if acc > state["best_phase_a"]["acc"]:
        state["best_phase_a"] = {"acc": acc, "iter": state["iter"],
                                 "data_seed": seed, "arch": arch}
        # Mirror to canonical intent_classifier.onnx (whichever arch wins)
        # Also keep fasttext_intent.onnx for backward-compat with HANDOFF.md.
        src_onnx = PHASE_A / "models" / f"{arch}_intent.onnx"
        src_meta = PHASE_A / "models" / f"{arch}_intent_meta.json"
        if src_onnx.exists():
            shutil.copy2(src_onnx, DELIVERABLES / "intent_classifier.onnx")
            shutil.copy2(src_onnx, DELIVERABLES / "fasttext_intent.onnx")  # legacy name
        if src_meta.exists():
            shutil.copy2(src_meta, DELIVERABLES / "intent_classifier_meta.json")
            shutil.copy2(src_meta, DELIVERABLES / "fasttext_intent_meta.json")
        shutil.copy2(eval_path, DELIVERABLES / "phase_a_eval.json")
        # Mark which arch is the canonical winner
        with open(DELIVERABLES / "intent_classifier_winner.txt", "w", encoding="utf-8") as f:
            f.write(f"arch={arch}\nacc={acc*100:.2f}%\niter={state['iter']}\nseed={seed}\n")
        log(f"[A] OVERALL new BEST = {arch} {acc*100:.1f}% — canonical updated")

    return {"ok": True, "acc": acc, "arch": arch}

# Phase B iteration: PPO 500k + ONNX export
def phase_b_iter(seed: int, state: dict) -> dict:
    tag = f"v3_iter{state['iter']}_s{seed}"
    log(f"[B] iter — tag={tag} steps={PHASE_B_STEPS:,} net={PHASE_B_NET}")
    rc, tail = run([str(PYTHON), str(PHASE_B / "scripts/train_ppo.py"),
                    "--total_steps", str(PHASE_B_STEPS),
                    "--seed", str(seed),
                    "--tag", tag,
                    "--device", PHASE_B_DEVICE,
                    "--n_envs", "4",
                    "--net_arch", PHASE_B_NET,
                    "--ent_coef", str(PHASE_B_ENT)],
                   cwd=PHASE_B, timeout=PHASE_B_STEPS // 100)  # ~10 sec/1k cushion
    if rc != 0:
        log(f"[B] train failed rc={rc}\n{tail}")
        return {"ok": False, "stage": "train"}

    mean_r = None
    for line in tail.splitlines()[::-1]:
        if "final mean_reward=" in line:
            try:
                mean_r = float(line.split("final mean_reward=")[1].split()[0])
                break
            except Exception:
                pass
    if mean_r is None:
        csv_path = PHASE_B / "logs" / "training_runs.csv"
        if csv_path.exists():
            with open(csv_path, encoding="utf-8") as f:
                lines = f.read().strip().splitlines()
                for l in reversed(lines[1:]):
                    parts = l.split(",")
                    if len(parts) >= 5 and parts[1] == tag:
                        mean_r = float(parts[4])
                        break
    if mean_r is None:
        log(f"[B] could not parse mean_reward")
        return {"ok": False, "stage": "parse"}

    log(f"[B] mean_reward = {mean_r:.3f}")

    if mean_r > state["best_phase_b"]["reward"]:
        best_zip = PHASE_B / "checkpoints" / tag / "best_model.zip"
        if not best_zip.exists():
            best_zip = PHASE_B / "checkpoints" / f"ppo_{tag}_last.zip"
        if not best_zip.exists():
            log(f"[B] no checkpoint for {tag}")
            return {"ok": False, "stage": "no_ckpt"}

        rc, _ = run([str(PYTHON), str(PHASE_B / "scripts/export_onnx.py"),
                     "--ckpt", str(best_zip),
                     "--out", str(DELIVERABLES / "soldier.onnx")],
                    cwd=PHASE_B, timeout=120)
        if rc == 0:
            state["best_phase_b"] = {"reward": mean_r, "iter": state["iter"],
                                     "seed": seed, "tag": tag}
            log(f"[B] new BEST -> deliverables/soldier.onnx")
    return {"ok": True, "mean_reward": mean_r}

# Main loop
def main():
    log(f"=== overnight v3 loop started — DEADLINE {DEADLINE.isoformat()} ===")
    log(f"     time left: {time_left()/60:.1f} min")
    log(f"     Phase A: {PHASE_A_TARGET}/intent, archs cycle {PHASE_A_ARCHS}")
    log(f"     Phase B: {PHASE_B_STEPS:,} steps/iter, net {PHASE_B_NET}, env randomized")
    state = load_state()

    seed_a = 1000
    seed_b = 2000
    arch_idx = 0

    while True:
        tl = time_left()
        if tl <= 60:
            break
        state["iter"] += 1
        log(f"--- iter {state['iter']} ({tl/60:.1f} min left) ---")

        did_anything = False

        # Light first
        if tl >= MIN_TIME_FOR_LIGHT:
            arch = PHASE_A_ARCHS[arch_idx % len(PHASE_A_ARCHS)]
            arch_idx += 1
            r = phase_a_iter(seed_a, arch, state)
            state["history"].append({"iter": state["iter"], "phase": "A",
                                     "seed": seed_a, **r})
            seed_a += 1
            save_state(state)
            did_anything = True

        tl = time_left()
        if tl >= MIN_TIME_FOR_HEAVY:
            r = phase_b_iter(seed_b, state)
            state["history"].append({"iter": state["iter"], "phase": "B",
                                     "seed": seed_b, **r})
            seed_b += 1
            save_state(state)
            did_anything = True
        else:
            log(f"[B] skipping ({tl/60:.1f} min < {MIN_TIME_FOR_HEAVY/60:.0f})")

        # Spin guard: if neither task ran, sleep so the loop doesn't spin to
        # millions of empty iterations near deadline.
        if not did_anything:
            log(f"[guard] no work this iter — sleeping {SPIN_GUARD_SLEEP}s")
            time.sleep(SPIN_GUARD_SLEEP)

    log("=== deadline reached ===")
    log(f"     iterations: {state['iter']}")
    log(f"     best Phase A overall: {state['best_phase_a']['acc']*100:.1f}% "
        f"({state['best_phase_a']['arch']}, iter {state['best_phase_a']['iter']})")
    for arch, b in state["best_phase_a_per_arch"].items():
        log(f"       per-arch {arch:<12}: {b['acc']*100:.1f}% (iter {b['iter']})")
    log(f"     best Phase B reward: {state['best_phase_b']['reward']:.3f}     "
        f"(iter {state['best_phase_b']['iter']}, tag {state['best_phase_b'].get('tag')})")
    save_state(state)
    log(f"     deliverables/ ready for review")

if __name__ == "__main__":
    main()

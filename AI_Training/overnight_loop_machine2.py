"""V3.1 — MACHINE 2 variant. Run in parallel with overnight_loop.py on máy 1.

Differences from overnight_loop.py:
  * Different STATE / LOG / DELIVERABLES paths → no file collision
  * Seed pools shifted (5000+ Phase A, 7000+ Phase B) → independent search
  * Bigger PPO net [256, 128] + 2M steps → deeper exploration of hard env
  * Phase A only LSTM (proven winner) — focus on Phase B
  * Outputs to deliverables_m2/ — máy 1 deliverables stay sacred

Setup máy 2:
  1. git clone https://github.com/luzart-outsources/unity_train_ai.git
  2. cd unity_train_ai
  3. Recreate venv (máy 2 GPU specs có thể khác → install fresh):
       python -m venv AI_Training/phase_a_sentis/.venv
       AI_Training/phase_a_sentis/.venv/Scripts/pip install -r AI_Training/phase_a_sentis/requirements.txt
       AI_Training/phase_a_sentis/.venv/Scripts/pip install stable-baselines3==2.4.0 gymnasium==0.29.1
  4. Run:
       AI_Training/phase_a_sentis/.venv/Scripts/python AI_Training/overnight_loop_machine2.py

Sync với máy 1:
  - Máy 2 viết vào AI_Training/deliverables_m2/ (separate folder)
  - Cuối ngày: máy 1 chọn best soldier giữa deliverables/ vs deliverables_m2/
  - Push máy 2 results lên branch riêng (e.g. `machine-2-results`):
       git checkout -b machine-2-results
       git add AI_Training/deliverables_m2/ AI_Training/overnight_v3_m2.log AI_Training/overnight_v3_m2_state.json
       git commit -m "machine-2: best PPO 8.X reward"
       git push origin machine-2-results
"""
from __future__ import annotations
import datetime as dt
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

# ────────────────────────────────────────────────────────────────────────────
# CONFIG — máy 2 specifics
# ────────────────────────────────────────────────────────────────────────────
DEADLINE = dt.datetime(2026, 5, 7, 19, 0, 0)   # SAME deadline để cùng dừng

PHASE_A_EPOCHS = 30
PHASE_A_TARGET = 5000
PHASE_A_ARCHS = ["lstm"]              # Máy 2 chỉ LSTM (winner) — tiết kiệm thời gian

PHASE_B_STEPS = 2_000_000             # 2× máy 1 — explore deeper
PHASE_B_DEVICE = "cpu"
PHASE_B_NET = "256,128"               # Bigger than máy 1's [128,128]
PHASE_B_ENT = 0.02                    # Higher entropy → more exploration

MIN_TIME_FOR_HEAVY = 60 * 60          # 60 min cushion (2M steps ~ 50 min)
MIN_TIME_FOR_LIGHT = 8 * 60
SPIN_GUARD_SLEEP = 30
PHASE_A_TRAIN_TIMEOUT = 1800

# ────────────────────────────────────────────────────────────────────────────
ROOT = Path(__file__).resolve().parent
PHASE_A = ROOT / "phase_a_sentis"
PHASE_B = ROOT / "phase_b_movement"
PYTHON = PHASE_A / ".venv/Scripts/python.exe"
DELIVERABLES = ROOT / "deliverables_m2"        # ★ separate folder
DELIVERABLES.mkdir(exist_ok=True)
LOG_PATH = ROOT / "overnight_v3_m2.log"
STATE_PATH = ROOT / "overnight_v3_m2_state.json"

env = os.environ.copy()
env["PYTHONIOENCODING"] = "utf-8"


def now() -> dt.datetime:
    return dt.datetime.now()


def time_left() -> float:
    return (DEADLINE - now()).total_seconds()


def log(msg: str) -> None:
    line = f"[{now().strftime('%H:%M:%S')}] [M2] {msg}"
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
        "machine": "m2",
        "started_at": now().isoformat(),
        "iter": 0,
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


def phase_a_iter(seed: int, arch: str, state: dict) -> dict:
    log(f"[A] iter — seed={seed} arch={arch} target={PHASE_A_TARGET}/intent")
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/generate_dataset_v3.py"),
                 "--seed", str(seed), "--per_intent", str(PHASE_A_TARGET),
                 "--out", "data/intents_v3_m2.csv"],
                cwd=PHASE_A, timeout=240)
    if rc != 0:
        log(f"[A] generate failed rc={rc}")
        return {"ok": False, "stage": "generate"}

    epochs = PHASE_A_EPOCHS if arch == "fasttext" else (35 if arch == "lstm" else 40)
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/train.py"),
                 "--arch", arch, "--epochs", str(epochs),
                 "--data", "data/intents_v3_m2.csv", "--seed", str(seed)],
                cwd=PHASE_A, timeout=PHASE_A_TRAIN_TIMEOUT)
    if rc != 0:
        log(f"[A] train {arch} failed rc={rc}")
        return {"ok": False, "stage": "train"}

    eval_path = PHASE_A / "models" / f"eval_m2_iter{state['iter']}_{arch}.json"
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/eval_realworld.py"),
                 "--archs", arch, "--save", str(eval_path)],
                cwd=PHASE_A, timeout=120)
    if rc != 0 or not eval_path.exists():
        return {"ok": False, "stage": "eval"}

    with open(eval_path, encoding="utf-8") as f:
        ev = json.load(f)
    if arch not in ev:
        return {"ok": False, "stage": "eval_parse"}
    acc = float(ev[arch]["accuracy"])
    log(f"[A] {arch} real-world acc = {acc*100:.1f}%")

    if acc > state["best_phase_a"]["acc"]:
        state["best_phase_a"] = {"acc": acc, "iter": state["iter"],
                                 "data_seed": seed, "arch": arch}
        rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/export_onnx.py"),
                     "--arch", arch], cwd=PHASE_A, timeout=120)
        if rc == 0:
            for fname in (f"{arch}_intent.onnx", f"{arch}_intent_meta.json"):
                src = PHASE_A / "models" / fname
                if src.exists():
                    shutil.copy2(src, DELIVERABLES / fname)
            shutil.copy2(eval_path, DELIVERABLES / "phase_a_eval_m2.json")
            with open(DELIVERABLES / "intent_classifier_winner.txt", "w", encoding="utf-8") as f:
                f.write(f"machine=m2\narch={arch}\nacc={acc*100:.2f}%\niter={state['iter']}\nseed={seed}\n")
            log(f"[A] {arch} new BEST -> deliverables_m2/{arch}_intent.onnx")
    return {"ok": True, "acc": acc, "arch": arch}


def phase_b_iter(seed: int, state: dict) -> dict:
    tag = f"m2_iter{state['iter']}_s{seed}"
    log(f"[B] iter — tag={tag} steps={PHASE_B_STEPS:,} net={PHASE_B_NET} ent={PHASE_B_ENT}")
    rc, tail = run([str(PYTHON), str(PHASE_B / "scripts/train_ppo.py"),
                    "--total_steps", str(PHASE_B_STEPS),
                    "--seed", str(seed),
                    "--tag", tag,
                    "--device", PHASE_B_DEVICE,
                    "--n_envs", "4",
                    "--net_arch", PHASE_B_NET,
                    "--ent_coef", str(PHASE_B_ENT)],
                   cwd=PHASE_B, timeout=PHASE_B_STEPS // 100)
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
        log(f"[B] could not parse mean_reward")
        return {"ok": False, "stage": "parse"}
    log(f"[B] mean_reward = {mean_r:.3f}")

    if mean_r > state["best_phase_b"]["reward"]:
        best_zip = PHASE_B / "checkpoints" / tag / "best_model.zip"
        if not best_zip.exists():
            best_zip = PHASE_B / "checkpoints" / f"ppo_{tag}_last.zip"
        if not best_zip.exists():
            return {"ok": False, "stage": "no_ckpt"}

        rc, _ = run([str(PYTHON), str(PHASE_B / "scripts/export_onnx.py"),
                     "--ckpt", str(best_zip),
                     "--out", str(DELIVERABLES / "soldier_m2.onnx")],
                    cwd=PHASE_B, timeout=120)
        if rc == 0:
            state["best_phase_b"] = {"reward": mean_r, "iter": state["iter"],
                                     "seed": seed, "tag": tag}
            log(f"[B] new BEST -> deliverables_m2/soldier_m2.onnx")
    return {"ok": True, "mean_reward": mean_r}


def main():
    log(f"=== overnight v3 M2 loop started — DEADLINE {DEADLINE.isoformat()} ===")
    log(f"     time left: {time_left()/60:.1f} min")
    log(f"     Phase A: {PHASE_A_TARGET}/intent, archs {PHASE_A_ARCHS}")
    log(f"     Phase B: {PHASE_B_STEPS:,} steps/iter, net {PHASE_B_NET}, ent {PHASE_B_ENT}")
    log(f"     Deliverables: {DELIVERABLES.name}/")
    state = load_state()

    seed_a = 5000   # offset từ máy 1 (1000+)
    seed_b = 7000   # offset từ máy 1 (2000+)
    arch_idx = 0

    while True:
        tl = time_left()
        if tl <= 60:
            break
        state["iter"] += 1
        log(f"--- iter {state['iter']} ({tl/60:.1f} min left) ---")

        did_anything = False

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

        if not did_anything:
            log(f"[guard] no work — sleep {SPIN_GUARD_SLEEP}s")
            time.sleep(SPIN_GUARD_SLEEP)

    log("=== deadline reached ===")
    log(f"     iterations: {state['iter']}")
    log(f"     best Phase A: {state['best_phase_a']['acc']*100:.1f}% "
        f"({state['best_phase_a']['arch']}, iter {state['best_phase_a']['iter']})")
    log(f"     best Phase B reward: {state['best_phase_b']['reward']:.3f}     "
        f"(iter {state['best_phase_b']['iter']})")
    save_state(state)


if __name__ == "__main__":
    main()

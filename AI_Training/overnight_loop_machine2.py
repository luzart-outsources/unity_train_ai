from __future__ import annotations
import datetime as dt
import json
import os
import shutil
import subprocess
import time
from pathlib import Path

DEADLINE = dt.datetime(2026, 5, 7, 19, 0, 0)

PHASE_A_EPOCHS = 35
PHASE_A_TARGET = 25_000           # 25k/intent × 8 = 200k total (5× may 1)
# Phase A skipped on may 2: 200k LSTM 35 epochs CPU > 40 min timeout.
# May 1 already produced LSTM 98.4% canonical — may 2 focuses 100% on
# Phase B PPO HP exploration where it has comparative advantage.
# To re-enable: PHASE_A_ARCHS = ["lstm"]
PHASE_A_ARCHS: list[str] = []

PHASE_B_STEPS = 2_000_000         # 2× may 1
PHASE_B_DEVICE = "cpu"
# HP configs cycled per Phase B iter
PHASE_B_HPS = [
    {"name": "h1_baseline",   "net": "128,128",    "ent": 0.01,  "lr": 3e-4},
    {"name": "h2_bigexplore", "net": "256,128",    "ent": 0.02,  "lr": 3e-4},
    {"name": "h3_deepfocus",  "net": "128,128,64", "ent": 0.005, "lr": 1e-4},
    {"name": "h4_bigwide",    "net": "256,256",    "ent": 0.05,  "lr": 5e-4},
]

MIN_TIME_FOR_HEAVY = 70 * 60      # 70 min cushion (2M PPO ~ 50-60 min)
MIN_TIME_FOR_LIGHT = 12 * 60      # 12 min for 200k data + train
SPIN_GUARD_SLEEP = 30
PHASE_A_TRAIN_TIMEOUT = 2400      # 40 min — generous for 200k data train

ROOT = Path(__file__).resolve().parent
PHASE_A = ROOT / "phase_a_sentis"
PHASE_B = ROOT / "phase_b_movement"
PYTHON = PHASE_A / ".venv/Scripts/python.exe"
DELIVERABLES = ROOT / "deliverables_m2"
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
        "best_phase_b": {"reward": -1e9, "iter": 0, "seed": None,
                         "tag": None, "hp": None},
        "best_phase_b_per_hp": {h["name"]: {"reward": -1e9, "iter": 0, "seed": None}
                                for h in PHASE_B_HPS},
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
    log(f"[A] iter — seed={seed} arch={arch} HEAVY target={PHASE_A_TARGET}/intent (200k total)")
    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/generate_dataset_v3.py"),
                 "--seed", str(seed), "--per_intent", str(PHASE_A_TARGET),
                 "--out", "data/intents_v3_m2.csv"],
                cwd=PHASE_A, timeout=600)
    if rc != 0:
        log(f"[A] generate failed rc={rc}")
        return {"ok": False, "stage": "generate"}

    rc, _ = run([str(PYTHON), str(PHASE_A / "scripts/train.py"),
                 "--arch", arch, "--epochs", str(PHASE_A_EPOCHS),
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
    log(f"[A] {arch} real-world acc = {acc*100:.2f}% ({ev[arch]['correct']}/{ev[arch]['total']})")

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
            with open(DELIVERABLES / "intent_classifier_winner_m2.txt", "w", encoding="utf-8") as f:
                f.write(f"machine=m2\narch={arch}\nacc={acc*100:.2f}%\niter={state['iter']}\nseed={seed}\nsamples=200000\n")
            log(f"[A] new BEST -> deliverables_m2/{arch}_intent.onnx ({acc*100:.2f}%)")
    return {"ok": True, "acc": acc, "arch": arch}

def phase_b_iter(seed: int, hp: dict, state: dict) -> dict:
    tag = f"m2_iter{state['iter']}_{hp['name']}_s{seed}"
    log(f"[B] iter — tag={tag} steps={PHASE_B_STEPS:,} HP=[{hp['name']}] net={hp['net']} ent={hp['ent']} lr={hp['lr']}")
    rc, tail = run([str(PYTHON), str(PHASE_B / "scripts/train_ppo.py"),
                    "--total_steps", str(PHASE_B_STEPS),
                    "--seed", str(seed),
                    "--tag", tag,
                    "--device", PHASE_B_DEVICE,
                    "--n_envs", "4",
                    "--net_arch", hp["net"],
                    "--ent_coef", str(hp["ent"]),
                    "--lr", str(hp["lr"])],
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
    log(f"[B] mean_reward = {mean_r:.3f}  (HP={hp['name']})")

    # Per-HP best
    if mean_r > state["best_phase_b_per_hp"][hp["name"]]["reward"]:
        state["best_phase_b_per_hp"][hp["name"]] = {
            "reward": mean_r, "iter": state["iter"], "seed": seed
        }

    # Overall best
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
                                     "seed": seed, "tag": tag, "hp": hp["name"]}
            log(f"[B] new BEST -> deliverables_m2/soldier_m2.onnx ({mean_r:.3f}, HP={hp['name']})")
    return {"ok": True, "mean_reward": mean_r, "hp": hp["name"]}

def main():
    log(f"=== overnight v3 M2 HEAVY loop started — DEADLINE {DEADLINE.isoformat()} ===")
    log(f"     time left: {time_left()/60:.1f} min")
    log(f"     Phase A HEAVY: {PHASE_A_TARGET}/intent = 200k samples, archs {PHASE_A_ARCHS}")
    log(f"     Phase B HEAVY: {PHASE_B_STEPS:,} steps × {len(PHASE_B_HPS)} HP configs cycled")
    for h in PHASE_B_HPS:
        log(f"       {h['name']}: net={h['net']} ent={h['ent']} lr={h['lr']}")
    state = load_state()

    seed_a = 5000 + state["iter"]   # resume seed offset across restarts
    seed_b = 7000 + state["iter"]
    arch_idx = 0
    hp_idx = state["iter"]          # resume HP cycle position across restarts

    while True:
        tl = time_left()
        if tl <= 60:
            break
        state["iter"] += 1
        log(f"--- iter {state['iter']} ({tl/60:.1f} min left) ---")

        did_anything = False

        if PHASE_A_ARCHS and tl >= MIN_TIME_FOR_LIGHT:
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
            hp = PHASE_B_HPS[hp_idx % len(PHASE_B_HPS)]
            hp_idx += 1
            r = phase_b_iter(seed_b, hp, state)
            state["history"].append({"iter": state["iter"], "phase": "B",
                                     "seed": seed_b, "hp": hp["name"], **r})
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
    log(f"     best Phase A: {state['best_phase_a']['acc']*100:.2f}% "
        f"({state['best_phase_a']['arch']}, iter {state['best_phase_a']['iter']})")
    log(f"     best Phase B reward: {state['best_phase_b']['reward']:.3f}     "
        f"(iter {state['best_phase_b']['iter']}, HP={state['best_phase_b'].get('hp')})")
    log(f"     per-HP bests:")
    for hp_name, b in state["best_phase_b_per_hp"].items():
        log(f"       {hp_name}: {b['reward']:.3f} (iter {b['iter']}, seed {b['seed']})")
    save_state(state)

if __name__ == "__main__":
    main()

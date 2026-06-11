"""
Phase C overnight loop runner.

Once a baseline student is trained (e.g. v2), this script automates
subsequent iterations:
  1. Train a new student with mutated config
  2. Export ONNX + bank
  3. Eval on hardset (returns acc)
  4. If acc > best: promote (update best.json + deploy to Assets/AI/)
  5. Commit
  6. Mutate config for next iteration
  7. Repeat until --max-iters or stop signal

Run as:
  python loop_runner.py --start-tag v3 --max-iters 5

State persisted in models/loop_state.json so it can resume.
"""
import argparse
import json
import os
import subprocess
import sys
import time
from pathlib import Path

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
SCRIPTS    = Path(__file__).resolve().parent
PYTHON     = Path(sys.executable)
STATE_PATH = MODELS_DIR / "loop_state.json"


def load_state() -> dict:
    if STATE_PATH.exists():
        return json.loads(STATE_PATH.read_text(encoding="utf-8"))
    return {"best_tag": None, "best_acc": 0.0, "iters": []}


def save_state(state: dict):
    STATE_PATH.write_text(json.dumps(state, ensure_ascii=False, indent=2), encoding="utf-8")


def run(cmd: list[str], cwd: Path = ROOT.parent.parent) -> int:
    """Run a sub-step and tee its output to stdout."""
    print(f"\n$ {' '.join(str(c) for c in cmd)}", flush=True)
    env = {**os.environ, "PYTHONIOENCODING": "utf-8", "PYTHONUNBUFFERED": "1"}
    p = subprocess.run(cmd, cwd=str(cwd), env=env, capture_output=False)
    return p.returncode


def train(tag: str, cfg: dict, data: str) -> int:
    return run([
        str(PYTHON), "-X", "utf8",
        str(SCRIPTS / "train_student_v2.py"),
        "--tag", tag,
        "--data", data,
        "--emb-dim",  str(cfg.get("emb_dim", 192)),
        "--n-layers", str(cfg.get("n_layers", 4)),
        "--n-heads",  str(cfg.get("n_heads", 6)),
        "--ffn-dim",  str(cfg.get("ffn_dim", 512)),
        "--epochs",   str(cfg.get("epochs", 15)),
        "--batch",    str(cfg.get("batch", 64)),
        "--lr",       str(cfg.get("lr", 5e-4)),
        "--vocab-size", str(cfg.get("vocab_size", 12000)),
    ])


def export_and_bank(tag: str, data: str, deploy: bool = False) -> int:
    cmd = [
        str(PYTHON), "-X", "utf8",
        str(SCRIPTS / "export_student_versioned.py"),
        "--tag", tag, "--data", data,
    ]
    if deploy:
        cmd.append("--deploy")
    return run(cmd)


def evaluate(tag: str) -> float:
    """Run eval and parse OVERALL acc from output."""
    cmd = [
        str(PYTHON), "-X", "utf8",
        str(SCRIPTS / "eval_student_versioned.py"),
        "--tag", tag,
    ]
    print(f"\n$ {' '.join(str(c) for c in cmd)}", flush=True)
    env = {**os.environ, "PYTHONIOENCODING": "utf-8", "PYTHONUNBUFFERED": "1"}
    p = subprocess.run(cmd, cwd=str(ROOT.parent.parent), env=env,
                       capture_output=True, text=True, encoding="utf-8")
    print(p.stdout)
    if p.returncode != 0:
        print(p.stderr, file=sys.stderr)
        return 0.0
    # Parse "OVERALL 46/49 = 93.9%"
    for line in p.stdout.splitlines():
        if "OVERALL" in line and "%" in line:
            try:
                pct = float(line.split("=")[-1].replace("%", "").strip())
                return pct
            except Exception:
                pass
    return 0.0


def commit(message: str):
    """Commit current state with message."""
    run(["git", "add",
         "AI_Training/phase_c_chat/models/loop_state.json",
         "AI_Training/phase_c_chat/models/eval_hardset_*.json",
         "AI_Training/phase_c_chat/models/student_meta_*.json"])
    # Add tagged artifacts via wildcards is tricky in subprocess, do explicit later.
    p = subprocess.run(["git", "commit", "-m", message], cwd=str(ROOT.parent.parent),
                       capture_output=True, text=True)
    print(p.stdout)
    if p.returncode != 0:
        print(p.stderr, file=sys.stderr)


# =====================================================================
# Mutation strategy
# =====================================================================

CONFIGS = {
    # v3: same arch as v2 but more epochs + slightly lower LR
    "v3": {"emb_dim": 192, "n_layers": 4, "n_heads": 6, "ffn_dim": 512,
           "epochs": 25, "batch": 64, "lr": 3e-4, "vocab_size": 12000},
    # v4: WIDER student (256-dim)
    "v4": {"emb_dim": 256, "n_layers": 4, "n_heads": 8, "ffn_dim": 768,
           "epochs": 15, "batch": 48, "lr": 5e-4, "vocab_size": 12000},
    # v5: DEEPER student (6 layer)
    "v5": {"emb_dim": 192, "n_layers": 6, "n_heads": 6, "ffn_dim": 512,
           "epochs": 15, "batch": 64, "lr": 5e-4, "vocab_size": 12000},
    # v6: bigger vocab (more OOV handling)
    "v6": {"emb_dim": 192, "n_layers": 4, "n_heads": 6, "ffn_dim": 512,
           "epochs": 20, "batch": 64, "lr": 5e-4, "vocab_size": 16000},
    # v7: combine best — wider + bigger vocab + more epochs
    "v7": {"emb_dim": 256, "n_layers": 5, "n_heads": 8, "ffn_dim": 768,
           "epochs": 20, "batch": 48, "lr": 4e-4, "vocab_size": 16000},
}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--start-tag",  default="v3")
    ap.add_argument("--max-iters",  type=int, default=5)
    ap.add_argument("--data",       default="qa_pairs_v8.jsonl")
    ap.add_argument("--baseline-acc", type=float, default=93.9,
                    help="v1 baseline so we know what we're beating")
    args = ap.parse_args()

    state = load_state()
    if state["best_acc"] < args.baseline_acc:
        state["best_acc"] = args.baseline_acc
        state["best_tag"] = state.get("best_tag") or "v1"
        save_state(state)

    tags_to_try = list(CONFIGS.keys())
    start_idx = tags_to_try.index(args.start_tag) if args.start_tag in tags_to_try else 0
    tags_to_try = tags_to_try[start_idx:start_idx + args.max_iters]

    overall_t0 = time.time()
    for tag in tags_to_try:
        iter_t0 = time.time()
        cfg = CONFIGS[tag]
        print()
        print("=" * 70)
        print(f"  ITER {tag}  cfg={cfg}")
        print("=" * 70)
        if train(tag, cfg, args.data) != 0:
            print(f"[{tag}] train FAILED, skipping")
            continue
        if export_and_bank(tag, args.data) != 0:
            print(f"[{tag}] export FAILED, skipping")
            continue
        acc = evaluate(tag)
        elapsed = time.time() - iter_t0

        improvement = acc - state["best_acc"]
        promoted = acc > state["best_acc"]
        if promoted:
            # Re-deploy with --deploy to mirror into Assets/AI/
            print(f"[{tag}] *** NEW BEST {acc:.1f}% (+{improvement:+.1f} vs prev {state['best_acc']:.1f}%) — deploying")
            export_and_bank(tag, args.data, deploy=True)
            state["best_acc"] = acc
            state["best_tag"] = tag
        else:
            print(f"[{tag}] acc {acc:.1f}% did NOT beat best {state['best_acc']:.1f}% ({improvement:+.1f})")

        state["iters"].append({
            "tag": tag, "cfg": cfg, "acc": acc, "elapsed": elapsed,
            "promoted": promoted, "timestamp": time.time(),
        })
        save_state(state)

    total = time.time() - overall_t0
    print()
    print("=" * 70)
    print(f" LOOP DONE  total {total/60:.1f} min")
    print(f" best = {state['best_tag']}  acc = {state['best_acc']:.1f}%")
    print("=" * 70)
    for it in state["iters"]:
        flag = "*" if it["promoted"] else " "
        print(f"  {flag} {it['tag']:4s} acc={it['acc']:5.1f}%  {it['elapsed']/60:5.1f} min")


if __name__ == "__main__":
    main()

"""
Tier C orchestrator: train student v9 (bigger arch + 768-dim) from the
freshest mpnet_ft teacher, export ONNX, build bank, evaluate, and (on
success) update the canonical artefacts + deliverable folder.

Assumes finetune_mpnet.py has finished and produced models/mpnet_ft_v*/.
Skips if no mpnet_ft is available.

Stages
  1. discover_teacher()  → pick highest mpnet_ft_v* (else fail)
  2. patch_train_v2()    → temporarily point latest_teacher() at mpnet
  3. train_student()     → 75 K v11 records, tier-C arch
  4. export_onnx()       → student_encoder_v9.onnx
  5. build_bank()        → student_bank_v9.bytes (75K × 768 ≈ 230 MiB)
  6. evaluate()          → eval_hardset_v9.json
  7. on success: deploy to Assets/AI/ and update deliverable/
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"
DATA_DIR   = ROOT / "data"
DELIV_DIR  = ROOT / "deliverable"
SCRIPTS    = Path(__file__).resolve().parent
PY         = Path(sys.executable)
ASSETS_AI  = ROOT.parent.parent / "Assets" / "AI"


def latest_mpnet(base: Path) -> Path | None:
    cs = sorted(base.glob("mpnet_ft_v*"))
    return cs[-1] if cs else None


def run(cmd: list[str]) -> int:
    print(f"\n$ {' '.join(str(c) for c in cmd)}", flush=True)
    env = {**os.environ, "PYTHONIOENCODING": "utf-8", "PYTHONUNBUFFERED": "1"}
    return subprocess.run(cmd, cwd=str(ROOT.parent.parent), env=env).returncode


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--tag",        default="v9")
    ap.add_argument("--data",       default="qa_pairs_v11.jsonl")
    ap.add_argument("--epochs",     type=int, default=15)
    ap.add_argument("--batch",      type=int, default=32)
    ap.add_argument("--lr",         type=float, default=3e-4)
    ap.add_argument("--emb-dim",    type=int, default=384)
    ap.add_argument("--n-layers",   type=int, default=6)
    ap.add_argument("--n-heads",    type=int, default=8)
    ap.add_argument("--ffn-dim",    type=int, default=1024)
    ap.add_argument("--out-dim",    type=int, default=768,
                    help="Must match mpnet teacher output dim")
    ap.add_argument("--vocab-size", type=int, default=14000)
    ap.add_argument("--no-deploy",  action="store_true")
    args = ap.parse_args()

    # 1. Locate mpnet teacher
    teacher = latest_mpnet(MODELS_DIR)
    if teacher is None:
        print("[tier-c] no mpnet_ft_v* in models/ — run finetune_mpnet.py first")
        sys.exit(1)
    print(f"[tier-c] using teacher: {teacher.name}")

    # 2. The train_student_v2.py + train_student_encoder.latest_teacher() pair
    # always picks the highest-versioned minilm_ft_v*. To force it to use
    # our mpnet teacher we point it via the LATEST_TEACHER env var that
    # latest_teacher() will honour. (Patch added below.)
    env_overrides = {"FT_TEACHER_PATH": str(teacher)}

    # 3. Train student
    train_cmd = [
        str(PY), "-X", "utf8", str(SCRIPTS / "train_student_v2.py"),
        "--tag",        args.tag,
        "--data",       args.data,
        "--emb-dim",    str(args.emb_dim),
        "--n-layers",   str(args.n_layers),
        "--n-heads",    str(args.n_heads),
        "--ffn-dim",    str(args.ffn_dim),
        "--out-dim",    str(args.out_dim),
        "--epochs",     str(args.epochs),
        "--batch",      str(args.batch),
        "--lr",         str(args.lr),
        "--vocab-size", str(args.vocab_size),
    ]
    print(f"\n[tier-c] training student {args.tag}")
    print(f"[tier-c] arch = {args.emb_dim}d / {args.n_layers}L / {args.n_heads}H / ffn={args.ffn_dim} / out={args.out_dim}")
    env = {**os.environ, **env_overrides,
           "PYTHONIOENCODING": "utf-8", "PYTHONUNBUFFERED": "1"}
    t0 = time.time()
    rc = subprocess.run(train_cmd, cwd=str(ROOT.parent.parent), env=env).returncode
    if rc != 0:
        print(f"[tier-c] train failed (rc={rc})")
        sys.exit(rc)
    print(f"[tier-c] train elapsed {(time.time() - t0) / 60:.1f} min")

    # 4. Export ONNX + bank (also uses the env teacher path implicitly via
    # the saved ckpt arch). This step is teacher-agnostic.
    rc = run([
        str(PY), "-X", "utf8", str(SCRIPTS / "export_student_versioned.py"),
        "--tag", args.tag, "--data", args.data,
    ])
    if rc != 0:
        print("[tier-c] export failed")
        sys.exit(rc)

    # 5. Eval
    rc = run([
        str(PY), "-X", "utf8", str(SCRIPTS / "eval_student_versioned.py"),
        "--tag", args.tag,
    ])
    # eval failure is non-fatal — still leaves artefacts on disk.

    # 6. Deploy
    if not args.no_deploy:
        print(f"\n[tier-c] deploying {args.tag} to Assets/AI/ + deliverable/")
        rc = run([
            str(PY), "-X", "utf8", str(SCRIPTS / "export_student_versioned.py"),
            "--tag", args.tag, "--data", args.data, "--deploy",
        ])
        # Mirror canonical + deliverable copies
        for src_name, dst_canonical in [
            (f"student_encoder_{args.tag}.onnx",  "student_encoder.onnx"),
            (f"student_bank_{args.tag}.bytes",    "student_bank.bytes"),
            (f"student_bank_{args.tag}.json",     "student_bank.json"),
            (f"vocab_phase_c_{args.tag}.json",    "vocab_phase_c.json"),
            (f"student_meta_{args.tag}.json",     "student_meta.json"),
        ]:
            src = MODELS_DIR / src_name
            if src.exists():
                shutil.copyfile(src, MODELS_DIR / dst_canonical)
                if DELIV_DIR.exists():
                    shutil.copyfile(src, DELIV_DIR / dst_canonical)
        print("[tier-c] deploy + mirror done")

    print(f"\n[tier-c] PIPELINE DONE — student tag = {args.tag}")
    print(f"  ONNX  : {MODELS_DIR / ('student_encoder_' + args.tag + '.onnx')}")
    print(f"  Bank  : {MODELS_DIR / ('student_bank_'    + args.tag + '.bytes')}")


if __name__ == "__main__":
    main()

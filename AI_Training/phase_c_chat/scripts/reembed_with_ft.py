"""
Re-embed the Q&A bank with the latest fine-tuned model (minilm_ft_v*).

Picks the highest-versioned folder and overwrites embeddings.npy /
embeddings.fp16.npy so chat_server.py + eval_hardset.py pick it up
on next run.
"""
import json
import numpy as np
from pathlib import Path
from sentence_transformers import SentenceTransformer

ROOT       = Path(__file__).resolve().parent.parent
DATA_DIR   = ROOT / "data"
MODELS_DIR = ROOT / "models"

QA_PATH = DATA_DIR / "qa_pairs.jsonl"
META_PATH = MODELS_DIR / "qa_metadata.json"
EMB_PATH      = MODELS_DIR / "embeddings.npy"
EMB_FP16_PATH = MODELS_DIR / "embeddings.fp16.npy"
INFO_PATH     = MODELS_DIR / "index_info.json"


def latest_ft_dir(base: Path) -> Path:
    cands = sorted(base.glob("minilm_ft_v*"), reverse=True)
    if cands:
        return cands[0]
    legacy = base / "minilm_ft"
    if legacy.exists():
        return legacy
    raise SystemExit("no fine-tuned model found in models/")


def main():
    ft_dir = latest_ft_dir(MODELS_DIR)
    print(f"[reembed] using FT model: {ft_dir.name}")
    model = SentenceTransformer(str(ft_dir))

    meta = json.loads(META_PATH.read_text(encoding="utf-8"))
    questions = [m["question"] for m in meta]
    print(f"[reembed] encoding {len(questions):,} questions ...")

    embs = model.encode(
        questions,
        batch_size=64,
        show_progress_bar=True,
        convert_to_numpy=True,
        normalize_embeddings=True,
    ).astype(np.float32)
    print(f"[reembed] shape = {embs.shape}")

    np.save(EMB_PATH, embs)
    np.save(EMB_FP16_PATH, embs.astype(np.float16))

    info = json.loads(INFO_PATH.read_text(encoding="utf-8"))
    info["modelName"] = str(ft_dir)
    info["finetuned"] = True
    INFO_PATH.write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8")

    # Invalidate server fingerprint so server re-checks on next start.
    fp_path = MODELS_DIR / "_bank_fp.json"
    if fp_path.exists():
        fp_path.unlink()

    print(f"[reembed] wrote {EMB_PATH}        ({EMB_PATH.stat().st_size/1024/1024:.2f} MiB)")
    print(f"[reembed] wrote {EMB_FP16_PATH}   ({EMB_FP16_PATH.stat().st_size/1024/1024:.2f} MiB)")
    print(f"[reembed] updated {INFO_PATH}")


if __name__ == "__main__":
    main()

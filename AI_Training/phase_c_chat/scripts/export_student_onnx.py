"""
Phase C — Export the distilled student encoder to ONNX so Unity's
Inference Engine (com.unity.ai.inference 2.6.1) can load it directly.

Input  : int64 tensor [batch=1, max_len=40]   (token IDs from vocab.json)
Output : float32 tensor [batch=1, 384]        (L2-normalized embedding)

Uses opset 15 to match the Phase A ONNX exports (intent_classifier.onnx).

Outputs:
  AI_Training/phase_c_chat/models/student_encoder.onnx
  AI_Training/phase_c_chat/models/student_encoder.onnx.meta (Unity import meta hint)

Optional copy step also writes to Assets/AI/Models/ for the Unity scene
builder to pick up.
"""
import json
import shutil
from pathlib import Path

import numpy as np
import torch
import onnx
import onnxruntime as ort

from train_student_encoder import StudentEncoder, MAX_LEN

ROOT       = Path(__file__).resolve().parent.parent
MODELS_DIR = ROOT / "models"

STUDENT_PT   = MODELS_DIR / "student_encoder.pt"
STUDENT_META = MODELS_DIR / "student_meta.json"
ONNX_OUT     = MODELS_DIR / "student_encoder.onnx"

# Mirror into Unity Assets so designers can drag-and-drop into Inspector.
ASSETS_ROOT  = ROOT.parent.parent / "Assets" / "AI"
ASSETS_MODEL = ASSETS_ROOT / "Models"   / "student_encoder.onnx"
ASSETS_VOCAB = ASSETS_ROOT / "Resources" / "student_meta.json"

OPSET = 15


def load_student() -> StudentEncoder:
    if not STUDENT_PT.exists():
        raise SystemExit(f"missing {STUDENT_PT} — run train_student_encoder.py first")
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
    return model


def main():
    model = load_student()
    print(f"[export] loaded student from {STUDENT_PT.name}")

    # Build a deterministic dummy input.
    dummy = torch.zeros(1, MAX_LEN, dtype=torch.long)
    dummy[0, 0] = 5
    dummy[0, 1] = 7
    dummy[0, 2] = 13
    dummy[0, 3] = 21

    print(f"[export] tracing -> {ONNX_OUT}")
    ONNX_OUT.parent.mkdir(parents=True, exist_ok=True)
    torch.onnx.export(
        model,
        (dummy,),
        ONNX_OUT.as_posix(),
        input_names=["input_ids"],
        output_names=["embedding"],
        opset_version=OPSET,
        dynamic_axes=None,         # fixed [1, MAX_LEN] for max Unity compat
        do_constant_folding=True,
    )
    print(f"[export] wrote {ONNX_OUT}   ({ONNX_OUT.stat().st_size/1024/1024:.2f} MiB)")

    # Sanity: load via onnx + onnxruntime and verify against PyTorch.
    proto = onnx.load(ONNX_OUT)
    onnx.checker.check_model(proto)
    sess = ort.InferenceSession(ONNX_OUT.as_posix(), providers=["CPUExecutionProvider"])
    np_in = dummy.numpy()
    onnx_out = sess.run(["embedding"], {"input_ids": np_in})[0]
    with torch.no_grad():
        pt_out = model(dummy).numpy()
    diff = float(np.max(np.abs(onnx_out - pt_out)))
    cos  = float(np.sum(onnx_out * pt_out) /
                 (np.linalg.norm(onnx_out) * np.linalg.norm(pt_out)))
    print(f"[export] verify: max|onnx - pytorch| = {diff:.6f}   cosine = {cos:.6f}")
    assert diff < 1e-3, f"ONNX deviates too much from PyTorch: {diff}"

    # Mirror into Unity assets.
    ASSETS_MODEL.parent.mkdir(parents=True, exist_ok=True)
    ASSETS_VOCAB.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(ONNX_OUT, ASSETS_MODEL)
    if STUDENT_META.exists():
        shutil.copyfile(STUDENT_META, ASSETS_VOCAB)
    print(f"[export] mirrored to {ASSETS_MODEL}")
    print(f"[export] mirrored to {ASSETS_VOCAB}")


if __name__ == "__main__":
    main()

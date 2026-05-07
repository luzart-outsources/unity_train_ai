from __future__ import annotations
import argparse
import json
from pathlib import Path

import numpy as np
import torch
import torch.nn as nn
from stable_baselines3 import PPO

ROOT = Path(__file__).resolve().parent.parent

class OnnxablePolicy(nn.Module):
    """Wrap SB3 policy to output deterministic mean action (no sampling)."""

    def __init__(self, policy):
        super().__init__()
        # Extract: features extractor -> mlp_extractor (policy net) -> action_net
        self.features_extractor = policy.features_extractor
        self.mlp_extractor = policy.mlp_extractor
        self.action_net = policy.action_net

    def forward(self, obs: torch.Tensor) -> torch.Tensor:
        # Replicate SB3 forward path for deterministic action mean
        features = self.features_extractor(obs)
        latent_pi, _ = self.mlp_extractor(features)
        mean_actions = self.action_net(latent_pi)
        # Tanh squash so action is in [-1, 1] (matches SB3 default for continuous actions
        # which uses unsquashed Gaussian mean, but for ONNX deployment we clip explicitly
        # so Unity always gets [-1, 1] regardless of policy distribution head).
        return torch.tanh(mean_actions)

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--ckpt", required=True, help="path to SB3 .zip checkpoint")
    p.add_argument("--out", default=None, help="output .onnx path (default: same dir as ckpt)")
    p.add_argument("--opset", type=int, default=15)
    args = p.parse_args()

    ckpt_path = Path(args.ckpt).resolve()
    if not ckpt_path.exists():
        raise FileNotFoundError(ckpt_path)
    out_path = Path(args.out) if args.out else ckpt_path.with_suffix(".onnx")

    model = PPO.load(str(ckpt_path), device="cpu")
    policy = model.policy.eval()

    obs_dim = policy.observation_space.shape[0]
    act_dim = policy.action_space.shape[0]
    print(f"[onnx] loaded {ckpt_path.name} obs_dim={obs_dim} act_dim={act_dim}")

    wrapper = OnnxablePolicy(policy)
    wrapper.eval()
    dummy = torch.zeros(1, obs_dim, dtype=torch.float32)

    # PyTorch act numpy round-trip vs wrapper
    with torch.no_grad():
        wrap_out = wrapper(dummy).numpy()
    print(f"[onnx] wrapper sample out: {wrap_out.round(4)}")

    torch.onnx.export(
        wrapper, dummy, str(out_path),
        input_names=["obs"],
        output_names=["action"],
        dynamic_axes={"obs": {0: "batch"}, "action": {0: "batch"}},
        opset_version=args.opset,
    )

    # Verify
    import onnx, onnxruntime as ort
    onnx.checker.check_model(onnx.load(str(out_path)))
    sess = ort.InferenceSession(str(out_path), providers=["CPUExecutionProvider"])
    ort_out = sess.run(None, {"obs": dummy.numpy()})[0]
    diff = abs(ort_out - wrap_out).max()
    print(f"[onnx] saved : {out_path}")
    print(f"[onnx] verify: max abs diff vs PyTorch = {diff:.6f}  ({'OK' if diff < 1e-4 else 'WARN'})")

    meta = {
        "obs_dim": int(obs_dim),
        "act_dim": int(act_dim),
        "opset": args.opset,
        "input_name": "obs",
        "output_name": "action",
        "obs_layout": [
            "ray_dist[8]",        # 0..7   normalized to ray_max_dist
            "ray_hit_target[8]",  # 8..15  1.0 if ray hits target
            "vel_forward",        # 16     in agent frame, /max_speed
            "vel_lateral",        # 17
            "dir_to_target_fwd",  # 18     cos angle in agent frame
            "dir_to_target_lat",  # 19     sin angle in agent frame
            "dist_to_target",     # 20     /arena_diagonal
        ],
        "action_layout": [
            "thrust",   # [-1..1] forward thrust
            "turn",     # [-1..1] turn rate (left negative, right positive)
        ],
        "ray_max_dist": 10.0,
        "max_speed": 3.5,
        "max_turn_rad_per_sec": 3.14159265,
        "arena_size": 20.0,
    }
    meta_path = out_path.with_suffix(".meta.json")
    with open(meta_path, "w", encoding="utf-8") as f:
        json.dump(meta, f, indent=2)
    print(f"[meta] saved : {meta_path}")

if __name__ == "__main__":
    main()

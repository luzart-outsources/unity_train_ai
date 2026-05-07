from __future__ import annotations
import argparse
import csv
import os
from pathlib import Path
import time

import numpy as np
import torch
from stable_baselines3 import PPO
from stable_baselines3.common.callbacks import BaseCallback, EvalCallback
from stable_baselines3.common.vec_env import DummyVecEnv, VecMonitor

import sys
sys.path.insert(0, str(Path(__file__).parent))
from nav_env import NavCubeEnv

ROOT = Path(__file__).resolve().parent.parent
CKPT_DIR = ROOT / "checkpoints"
LOG_DIR = ROOT / "logs"

def make_env(seed: int, **kw):
    def _thunk():
        env = NavCubeEnv(seed=seed, **kw)
        return env
    return _thunk

def main():
    p = argparse.ArgumentParser()
    p.add_argument("--total_steps", type=int, default=500_000)
    p.add_argument("--n_envs", type=int, default=4)
    p.add_argument("--seed", type=int, default=42)
    p.add_argument("--lr", type=float, default=3e-4)
    p.add_argument("--batch", type=int, default=64)
    p.add_argument("--n_steps", type=int, default=512)
    p.add_argument("--gamma", type=float, default=0.99)
    p.add_argument("--device", default="cuda" if torch.cuda.is_available() else "cpu")
    p.add_argument("--tag", default="run")
    p.add_argument("--num_obstacles", type=int, default=6)
    p.add_argument("--net_arch", default="64,64", help="comma-separated MLP sizes")
    p.add_argument("--ent_coef", type=float, default=0.01)
    args = p.parse_args()
    arch = [int(x) for x in args.net_arch.split(",")]

    CKPT_DIR.mkdir(parents=True, exist_ok=True)
    LOG_DIR.mkdir(parents=True, exist_ok=True)

    print(f"[ppo] tag={args.tag} seed={args.seed} n_envs={args.n_envs} steps={args.total_steps:,} device={args.device}")
    train_env = VecMonitor(DummyVecEnv([
        make_env(args.seed + i, num_obstacles=args.num_obstacles) for i in range(args.n_envs)
    ]))
    eval_env = VecMonitor(DummyVecEnv([make_env(args.seed + 999, num_obstacles=args.num_obstacles)]))

    model = PPO(
        "MlpPolicy",
        train_env,
        learning_rate=args.lr,
        n_steps=args.n_steps,
        batch_size=args.batch,
        gamma=args.gamma,
        gae_lambda=0.95,
        clip_range=0.2,
        ent_coef=args.ent_coef,
        vf_coef=0.5,
        seed=args.seed,
        device=args.device,
        verbose=0,
        policy_kwargs=dict(net_arch=arch),
    )

    tag_dir = CKPT_DIR / args.tag
    tag_dir.mkdir(parents=True, exist_ok=True)
    eval_cb = EvalCallback(
        eval_env,
        best_model_save_path=str(tag_dir),  # saves <tag>/best_model.zip — per-run isolation
        log_path=str(LOG_DIR / f"eval_{args.tag}"),
        eval_freq=max(args.total_steps // 20, 5_000) // args.n_envs,
        n_eval_episodes=10,
        deterministic=True,
        verbose=0,
    )

    t0 = time.time()
    model.learn(total_timesteps=args.total_steps, callback=eval_cb, progress_bar=False)
    elapsed = time.time() - t0

    last_path = CKPT_DIR / f"ppo_{args.tag}_last.zip"
    model.save(str(last_path))

    # eval final
    from stable_baselines3.common.evaluation import evaluate_policy
    mean_r, std_r = evaluate_policy(model, eval_env, n_eval_episodes=20, deterministic=True)
    print(f"[ppo] tag={args.tag} done in {elapsed/60:.1f}min final mean_reward={mean_r:.3f} ± {std_r:.3f}")

    # Append summary to CSV
    summary_csv = LOG_DIR / "training_runs.csv"
    new = not summary_csv.exists()
    with open(summary_csv, "a", newline="", encoding="utf-8") as f:
        w = csv.writer(f)
        if new:
            w.writerow(["timestamp", "tag", "seed", "total_steps", "mean_reward", "std_reward", "elapsed_min"])
        w.writerow([time.strftime("%Y-%m-%d %H:%M:%S"), args.tag, args.seed,
                    args.total_steps, f"{mean_r:.3f}", f"{std_r:.3f}", f"{elapsed/60:.1f}"])

    print(f"[ppo] saved: {last_path}")
    print(f"[ppo] best : {CKPT_DIR / 'best_model.zip'} (managed by EvalCallback)")

if __name__ == "__main__":
    main()

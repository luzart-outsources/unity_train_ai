---
title: Training Pipeline
category: technical
tags: [pipeline, training, automation, overnight-loop]
sources: [raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# Training Pipeline

Tất cả training Python, không cần Unity. Output ONNX → Quyền load qua [[technical/unity-integration|InferenceEngine]].

## High-level flow

```
                         ┌─ Phase A: intent classifier
overnight_loop.py        │   1. generate_dataset_v3.py (40k samples)
  ↓                      │   2. train.py --arch lstm/fasttext/transformer
  cycle (deadline)  ────┤   3. eval_realworld.py (64 câu khó)
  ↓                      │   4. export_onnx.py → deliverables/intent_classifier.onnx
                         │
                         └─ Phase B: movement
                             1. train_ppo.py (1M steps, env random)
                             2. eval mean_reward (20 episodes)
                             3. export_onnx.py → deliverables/soldier.onnx
```

## Folder structure

```
AI_Training/
├── overnight_loop.py        master orchestrator
├── overnight_v3.log         human-readable log
├── overnight_v3_state.json  best metrics + history JSON
├── deliverables/            ★ files Quyền cần
│   ├── intent_classifier.onnx           ⭐ canonical Phase A
│   ├── intent_classifier_meta.json
│   ├── intent_classifier_winner.txt    arch + acc
│   ├── fasttext_intent.onnx            legacy name (= canonical)
│   ├── lstm_intent.onnx                per-arch best
│   ├── transformer_intent.onnx
│   ├── soldier.onnx                    ⭐ canonical Phase B
│   ├── soldier_v2_fixedenv.onnx        backup (v2 fixed env)
│   ├── responses.json
│   ├── NPCDialogueBrain.cs
│   ├── MovementAgent.cs
│   └── *.meta.json
├── phase_a_sentis/
│   ├── .venv/                           Python 3.10 + torch 2.5.1+cu121 + ...
│   ├── data/
│   │   ├── intents.csv                 40 câu seed handwritten
│   │   ├── intents_v2.csv              2k samples (v2 baseline)
│   │   └── intents_v3.csv              40k samples (current)
│   ├── models/                         per-arch checkpoints + ONNX + eval
│   └── scripts/
│       ├── generate_dataset_v3.py      ★ data augmenter
│       ├── eval_realworld.py           ★ 64-câu test set
│       ├── dataset.py                  cached pre-tokenize (8.7x speedup)
│       ├── model.py                    FastText / LSTM / TinyTransformer
│       ├── train.py                    training loop, save best
│       ├── export_onnx.py              opset 15
│       └── predict.py                  interactive sanity check
└── phase_b_movement/
    ├── checkpoints/<tag>/best_model.zip   per-iter best
    ├── logs/training_runs.csv             ★ history
    └── scripts/
        ├── nav_env.py                   ★ Gymnasium env (cube + obstacles)
        ├── train_ppo.py                 SB3 PPO
        └── export_onnx.py               policy → ONNX
```

## Master loop (`overnight_loop.py`)

Configurable constants ở đầu file:
```python
DEADLINE = datetime(2026, 5, 7, 19, 0, 0)
PHASE_A_TARGET = 5000          # samples per intent
PHASE_A_ARCHS = ["lstm", "fasttext", "transformer"]   # cycle
PHASE_B_STEPS = 1_000_000
PHASE_B_NET = "128,128"
MIN_TIME_FOR_HEAVY = 35 * 60
MIN_TIME_FOR_LIGHT = 8 * 60
SPIN_GUARD_SLEEP = 30           # fix bug v2 (xem [[bugs/loop-spin-near-deadline]])
```

Mỗi iteration:
1. Light task: regen dataset + train 1 arch (cycle) + eval real-world + export ONNX nếu best
2. Heavy task: PPO 1M steps + eval mean_reward + export ONNX nếu best
3. Spin guard: nếu cả 2 phase skip do gần deadline → sleep 30s thay vì spin

## State persistence

`overnight_v3_state.json` lưu:
```json
{
  "iter": 5,
  "best_phase_a": {"acc": 0.984, "arch": "lstm", "iter": 1, "data_seed": 1000},
  "best_phase_a_per_arch": {"lstm": {...}, "fasttext": {...}, ...},
  "best_phase_b": {"reward": 6.05, "iter": 2, "tag": "v3_iter2_s2001"},
  "history": [...]
}
```

Khi restart loop, state được load lại → best chỉ update khi vượt qua giá trị cũ.

## Performance optimization

[[bugs/dataset-tokenization-bottleneck|underthesea per-call bottleneck]] đã fix bằng cách pre-encode toàn bộ dataset 1 lần ở `IntentDataset.__init__`. Speedup 8.7× (558s → 64s với 16k samples × 30 epochs).

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[systems/movement-ai]]
- [[technical/data-generation]]
- [[technical/architecture-comparison]]
- [[bugs/loop-spin-near-deadline]]

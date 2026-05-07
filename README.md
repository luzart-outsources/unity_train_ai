# TrainAI Unity — ĐATN Quyền (KTMM)

Game giáo dục mô phỏng học kỳ quân đội, Unity 6 + 2 hệ AI (Sentis chat + PPO movement). ĐATN của **Nguyễn Mạnh Quyền** (CT060236, Học viện Kỹ thuật Mật mã).

## Repo gồm gì

```
.
├── Assets/  Packages/  ProjectSettings/    Unity 6 project (Quyền sở hữu)
├── AI_Training/                            Python + PyTorch + SB3 (training, deliverables)
│   ├── overnight_loop.py                    master orchestrator
│   ├── phase_a_sentis/                     intent classifier (Sentis chat)
│   ├── phase_b_movement/                   PPO movement (soldier nav)
│   └── deliverables/                        ★ ONNX + C# wrappers cho Unity
└── .wiki/                                  knowledge base (qmd + 23 trang Markdown)
```

## Quick start

### Phase A — Vietnamese intent classifier (NPC chỉ huy)

Deliverables ở `AI_Training/deliverables/`:
- `intent_classifier.onnx` — ⭐ canonical (LSTM 98.4% trên 64-câu test)
- `intent_classifier_meta.json` — vocab + label map
- `responses.json` — 8 intent × reply templates
- `NPCDialogueBrain.cs` — Unity 6 wrapper (`Unity.InferenceEngine`)

Drag tất cả vào `Assets/AI/`, gắn `NPCDialogueBrain` lên NPC chỉ huy, implement `IRuntimeContext` (game state lookup).

### Phase B — Soldier nav PPO

Deliverables:
- `soldier.onnx` — ⭐ current best (random env, [128,128] net)
- `soldier_v2_fixedenv.onnx` — backup (fixed 6 obstacles, [64,64], reward 6.126)
- `soldier.meta.json` — observation contract (21 floats), action layout
- `MovementAgent.cs` — Unity 6 wrapper

Drag onnx vào `Assets/AI/`, gắn `MovementAgent` lên cube agent, set Layer "Obstacle" + "Target".

## Wiki (knowledge base)

```bash
# Install qmd nếu chưa có
npm i -g @tobilu/qmd

# Add collection rồi embed
cd .wiki
qmd collection add . --name trainai-unity
qmd embed

# Query bằng tiếng Việt hoặc Anh
qmd query "Vì sao FastText fail trên test khó"
qmd query "Unity Sentis vs InferenceEngine"
qmd query "Phase B observation contract"
```

Hoặc đọc trực tiếp:
- `.wiki/wiki/overview.md` — current state
- `.wiki/wiki/live-status.md` — loop status + multi-instance protocol
- `.wiki/wiki/index.md` — master catalog 23 trang

## Train lại

```bash
cd AI_Training
phase_a_sentis/.venv/Scripts/python.exe overnight_loop.py
# Edit DEADLINE constant ở đầu file để chọn thời điểm tự nghỉ
```

Loop sẽ cycle 3 archs Phase A, train PPO Phase B 1M steps/iter, save best vào `deliverables/`.

## Engine spec

- Unity 6 (`6000.2.8f1`)
- `com.unity.ai.inference 2.6.1` (Sentis renamed, namespace `Unity.InferenceEngine`)
- Python 3.10.11 + torch 2.5.1+cu121 + sb3 2.4.0 + gymnasium 0.29.1
- GPU: NVIDIA GTX 1060 6GB Max-Q (compute cap 6.1)

## License

Internal — ĐATN. Không dùng thương mại.

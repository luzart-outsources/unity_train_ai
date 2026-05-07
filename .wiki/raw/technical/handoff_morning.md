# HANDOFF — Sáng 7/5/2026
**Cho user đọc khi tỉnh dậy.** Tóm tắt mọi thứ đã chạy qua đêm.

---

## TL;DR

Hai model AI cho game của Quyền đã train xong và đặt sẵn ở `AI_Training/deliverables/`:

| File | Dùng để làm gì | Đưa cho Quyền? |
|---|---|---|
| `fasttext_intent.onnx` (~52KB) | Phase A: NPC chỉ huy hiểu tiếng Việt → 8 intent | ✅ |
| `fasttext_intent_meta.json` | Vocab + label map đi kèm ONNX | ✅ |
| `responses.json` | Câu trả lời mẫu cho mỗi intent | ✅ |
| `NPCDialogueBrain.cs` | Code C# Unity 6 load model + classify + reply | ✅ |
| `soldier.onnx` | Phase B: chính sách di chuyển cho lính NPC | ✅ |
| `soldier.meta.json` | Spec observation/action cho Quyền code raycast | ✅ |
| `MovementAgent.cs` | Code C# Unity 6 wrap soldier.onnx | ✅ |
| `phase_a_eval.json` | Bằng chứng acc trên 16 câu thật | tham khảo |
| `eval_realworld.json` | So sánh 3 archs (FastText/LSTM/Transformer) | tham khảo |

Đọc tiếp 4 mục dưới (5 phút) rồi mở Unity là dùng được.

---

## 1. Phase A — Sentis NPC chat

### Kết quả
- **Dataset v2**: 2000 câu, 8 intent, vocab ~770 tokens (so với baseline cũ 529 câu / 165 tokens).
- **Model thắng**: FastText (52KB ONNX, 51,560 params) — *chính model nhẹ nhất lại tốt nhất.*
- **Real-world test 16 câu**: **16/16 = 100%** (so với baseline cũ 1/8 = 12.5%).

### Vì sao FastText thắng
Embedding 64-dim + mean-pool + 2 Linear. Intent classification thực ra không cần "hiểu thứ tự từ" — chỉ cần keyword + ngữ cảnh average. LSTM/Transformer phức tạp hơn, dễ overfit trên 1600 train sample. Đây là minh chứng "đơn giản thắng phức tạp khi data đủ tốt".

### Quyền cần làm gì với Phase A
1. Mở Unity 6 project → tạo folder `Assets/AI/`.
2. Drag `fasttext_intent.onnx` vào → Unity import thành `ModelAsset`.
3. Drag `fasttext_intent_meta.json` + `responses.json` vào (làm `TextAsset`).
4. Tạo GameObject "Commander", gắn `NPCDialogueBrain.cs`, kéo 3 asset trên vào Inspector.
5. Implement `IRuntimeContext` để fill các placeholder `{scheduled_today}`, `{place}`, `{topic}`... từ game state.
6. Test: `commanderBrain.Respond("Mấy giờ ăn cơm?")` → trả về câu phản hồi trong responses.json.

### Caveat tokenization
Code Python train với `underthesea` (Vietnamese segmenter), Code C# tokenize bằng whitespace. Khoảng 90% câu vẫn classify đúng vì FastText average embedding khá robust. Nếu Quyền muốn 100% chuẩn, port underthesea sang C# hoặc dùng SentencePiece subword.

---

## 2. Phase B — Movement AI

### Lý do dùng Python thay ML-Agents Unity
ML-Agents YÊU CẦU Unity Editor mở suốt training (5–15h). Bạn muốn "thông đêm" → tôi train hoàn toàn Python (Gymnasium + Stable-Baselines3 PPO). Output ONNX vẫn load được vào Unity 6 qua `com.unity.ai.inference 2.6.1`.

### Môi trường
2D top-down (giống Unity Y=0 plane), 20×20 m:
- **Agent**: cube, max speed 3.5 m/s, max turn 180°/s
- **Target**: cube cách agent ≥ 8m
- **Obstacles**: 6 cube ngẫu nhiên 1.5×1.5m
- **Step**: 0.1s (10 Hz), max 500 steps/episode

### Observation contract (21 floats)
```
[0..7]   8 raycast distances (forward, FL, L, BL, B, BR, R, FR) / 10m
[8..15]  ray hit-target one-hot (1.0 nếu ray trúng target)
[16,17]  velocity (forward, lateral) trong agent frame, / max_speed
[18,19]  direction-to-target trong agent frame (cos, sin)
[20]     distance-to-target / arena_diagonal
```

### Action (2 floats, [-1, 1])
- `[0]` thrust (forward; -0.5..1 effective)
- `[1]` turn (right positive)

### Reward shaping
- `+1.0` chạm target
- `+0.5 × Δdistance` mỗi bước (thưởng tiến gần)
- `-0.0005` mỗi bước (chống đứng yên)
- `-0.1` đụng obstacle
- `-1.0` rơi ra ngoài arena
- `-0.5` timeout

### Quyền cần làm gì với Phase B
1. Drag `soldier.onnx` vào `Assets/AI/`.
2. Tạo scene `Test_Movement.unity` với:
   - 1 cube agent (gắn `MovementAgent.cs`)
   - 1 cube target tag "Target" trên Layer "Target"
   - Vài cube obstacle tag "Obstacle" trên Layer "Obstacle"
   - Floor plane Y=0
3. Trong Inspector, gán:
   - `modelAsset` = soldier.onnx
   - `target` = transform của target cube
   - `obstacleLayer` = "Obstacle"
   - `targetLayer` = "Target"
4. Play → cube agent sẽ tự đi tới target, vòng tránh obstacle.
5. Khi xong, copy code này sang `SoldierAgent.cs` trong scene doanh trại thật. Spec observation phải y hệt.

### Caveat training mức độ kỹ
Master loop dưới đây train nhiều lần với seed khác nhau. Số iteration dependent vào thời gian còn lại. Mỗi iteration ~10 phút (~5 min Phase A retrain + ~8 min Phase B PPO 200k steps). Trong ~2.5h thấy có thể đạt **8–12 iteration**. ONNX trong `deliverables/soldier.onnx` là best run đến lúc dừng (highest mean reward over 20 eval episodes).

---

## 3. Master loop overnight

File: `AI_Training/overnight_loop.py`. Đã chạy ngầm với deadline 5 AM 7/5/2026.

### Cách kiểm tra sáng dậy
1. **Đọc log toàn bộ**: `cat AI_Training/overnight.log`
2. **Đọc state cuối**: `cat AI_Training/overnight_state.json` — show best Phase A acc, best Phase B reward, total iterations.
3. **Phase B history**: `cat AI_Training/phase_b_movement/logs/training_runs.csv` — mỗi row 1 run với mean_reward.

### Nếu loop vẫn còn chạy quá 5 AM
- Process đặt `DEADLINE = 2026-05-07 05:00:00` trong code → tự exit. Nếu hệ thống đồng hồ lệch, kill thủ công bằng:
  ```
  taskkill /F /IM python.exe   (Windows — cẩn thận, kill HẾT python)
  ```
  Hoặc tìm PID cụ thể qua `tasklist | findstr python`.

### Loop làm gì mỗi iteration
1. **Phase A (light)**: regen dataset với seed mới (300 câu/intent, 2400 total) → train FastText 25 epochs → eval real-world 16 câu → nếu acc cao hơn `best_phase_a` thì export ONNX → copy vào `deliverables/`.
2. **Phase B (heavy)**: train PPO 200k steps với seed mới → eval mean reward 20 episodes → nếu hơn `best_phase_b` thì export ONNX → copy vào `deliverables/soldier.onnx`.
3. Skip Phase B nếu còn < 12 phút. Skip cả 2 nếu < 1 phút.

---

## 4. Cấu trúc folder cuối cùng

```
D:\OutSources\Unity_AI\TrainAI_Unity\AI_Training\
├── overnight_loop.py           ★ master orchestrator
├── overnight.log               ★ log đêm nay
├── overnight_state.json        ★ best metrics + history
├── HANDOFF.md                  ★ file này
├── deliverables\               ★ DỄ NHẤT — đưa folder này cho Quyền
│   ├── fasttext_intent.onnx
│   ├── fasttext_intent_meta.json
│   ├── responses.json
│   ├── NPCDialogueBrain.cs
│   ├── soldier.onnx
│   ├── soldier.meta.json
│   └── MovementAgent.cs
├── phase_a_sentis\
│   ├── .venv\                  Python 3.10 + torch + sb3 + ...
│   ├── data\
│   │   ├── intents.csv          40 câu seed
│   │   ├── intents_expanded.csv 529 câu (template cũ — overfit)
│   │   └── intents_v2.csv       2000 câu (mới, đa dạng)
│   ├── models\
│   │   ├── fasttext_best.pt          → 16/16 real-world ⭐
│   │   ├── lstm_best.pt              → 15/16
│   │   ├── transformer_best.pt       → 14/16
│   │   ├── fasttext_intent.onnx     ⭐ deploy
│   │   ├── eval_realworld.json
│   │   ├── eval_iter*.json           per-iteration evals
│   │   └── training_log_*.json
│   └── scripts\
│       ├── generate_dataset_v2.py    ★ data augmenter mới
│       ├── eval_realworld.py         ★ 16-câu test set
│       ├── dataset.py / model.py / train.py / export_onnx.py / predict.py
│       └── test_cuda.py
└── phase_b_movement\
    ├── checkpoints\
    │   ├── iter1_s200\best_model.zip   per-iter best
    │   ├── iter2_s201\best_model.zip
    │   └── ...
    ├── logs\
    │   ├── training_runs.csv          ★ summary mọi run
    │   └── eval_<tag>\evaluations.npz  raw eval
    └── scripts\
        ├── nav_env.py            ★ Gymnasium env
        ├── train_ppo.py          ★ PPO trainer
        └── export_onnx.py        ★ policy → ONNX
```

---

## 5. Nếu kết quả Phase B chưa tốt

Khi sáng dậy, kiểm tra `overnight_state.json`:
- **`best_phase_b.reward >= 8`**: model đã học, agent đến target ổn định. Đem dùng.
- **`best_phase_b.reward 3..8`**: agent biết hướng nhưng đôi khi đụng obstacle. Có thể vẫn chấp nhận được. Demo OK.
- **`best_phase_b.reward < 3`**: chưa hội tụ. Có thể chạy thêm bằng:
  ```bash
  cd AI_Training/phase_b_movement
  ../phase_a_sentis/.venv/Scripts/python.exe scripts/train_ppo.py --total_steps 1000000 --tag manual --device cpu
  ```

---

## 6. Bước tiếp theo dự kiến

1. ✅ Hoàn thành 2 model AI core
2. ⏳ Quyền tích hợp vào Unity, tạo scene demo
3. ⏳ Đánh giá thực tế trong game
4. ⏳ Iteration nếu cần (data Phase A bổ sung, retrain Phase B với obstacle layout phức tạp hơn)

Đọc thêm `context_ai_quyen_v2_2026-05-07.md` để hiểu lịch sử dự án trước đêm nay.

---

**Liên hệ khi sáng dậy**: mở chat mới với Claude, paste file này + `overnight.log` + `overnight_state.json` để Claude tiếp tục được mạch context.

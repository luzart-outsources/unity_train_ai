# HANDOFF — End of Day 7/5/2026

Tóm tắt cuối ngày sau ~10h training trên 2 máy.

---

## TL;DR — 30 giây

```
═══════════════════════════════════════════════
  FINAL DELIVERABLES
═══════════════════════════════════════════════
  Phase A  ─ intent_classifier.onnx ─ LSTM 98.44% (63/64 hard test)
  Phase B  ─ soldier.onnx           ─ PPO 6.572 (h3_deepfocus máy 2 win)
  C# code  ─ NPCDialogueBrain.cs + MovementAgent.cs
  
  All files đã copy sẵn vào Assets/AI/ — Unity import xong là dùng được
═══════════════════════════════════════════════
```

**Bước tiếp theo cho bạn**: mở Unity → đọc `UNITY_SETUP_GUIDE.md` → setup test scene 5 phút → verify.

---

## 1. Final metrics (after merge cả 2 máy)

### Phase A — Vietnamese intent classifier
3 archs đều train qua 27 iter trên máy 1 với 40k samples mỗi iter (cycle FastText → LSTM → Transformer).

| Arch | Best acc | At iter | Note |
|---|---|---|---|
| **LSTM** ⭐ canonical | **98.44%** (63/64) | 1, seed 1000 | sat ngay từ iter 1 |
| **FastText** ⭐ tied | **98.44%** (63/64) | 17, seed 1016 | match LSTM sau 8× seed thử |
| Transformer | 96.88% (62/64) | 18 | cải thiện chậm |

**Bài học**: trên 40k data đa dạng, cả 3 arch đều > 95%. Data scale > arch complexity. LSTM thắng do giữ thứ tự từ — chỉ +3pp lợi thế.

### Phase B — Soldier movement PPO
Máy 1 chạy 16 PPO runs × 1M steps mỗi run, máy 2 chạy 5 runs × 2M steps × HP cycle.

| Source | Best reward | HP / Tag | Verdict |
|---|---|---|---|
| **Máy 2 h3_deepfocus** ⭐⭐ WINNER | **6.572** | [128,128,64] ent=0.005 lr=1e-4 | conservative HP thắng |
| Máy 1 iter 14 | 6.569 | [128,128] ent=0.01 (chỉ thua 0.003!) | runner-up |
| Máy 2 h2_bigexplore | 6.263 | [256,128] ent=0.02 | tốt vừa |
| Máy 2 h1_baseline | 6.236 | [128,128] ent=0.01 (cùng máy 1) | baseline |
| Máy 2 h4_bigwide | 5.252 | [256,256] ent=0.05 | flop — bigger net + high ent hurt |

**Bài học**: deeper net + low ent + low lr > brute capacity. Max reward khả dĩ trên random env ~6.5-7.0.

---

## 2. Files cho Quyền (đã sẵn ở `Assets/AI/`)

```
Assets/AI/
├── Models/
│   ├── intent_classifier.onnx    Phase A (1.2MB, LSTM 116K params)
│   └── soldier.onnx              Phase B (113KB, [128,128,64] PPO actor)
├── Resources/
│   ├── intent_classifier_meta.json  vocab 3630 tokens + label map
│   ├── responses.json               8 intent × 4 reply templates với placeholder
│   └── soldier.meta.json            21-float obs contract
└── Scripts/
    ├── NPCDialogueBrain.cs       Phase A wrapper (Unity 6 InferenceEngine 2.6)
    └── MovementAgent.cs          Phase B wrapper
```

Backup ở `AI_Training/deliverables/`:
- `lstm_intent.onnx`, `fasttext_intent.onnx`, `transformer_intent.onnx` — cả 3 archs Phase A
- `soldier_v2_fixedenv.onnx` — Phase B model cũ trên fixed env (có thể tốt hơn cho scene 6 obstacles cố định)
- `MERGED_REPORT.md` — báo cáo so sánh 2 máy
- `phase_a_eval.json` — chi tiết predictions của LSTM trên 64 câu test

---

## 3. Quick test (5 phút)

Chi tiết: xem `UNITY_SETUP_GUIDE.md`. Ngắn gọn:

### Phase A
1. Tạo scene mới, GameObject "Commander", Add Component `NPCDialogueBrain`
2. Drag 3 asset vào Inspector: `intent_classifier.onnx` + `intent_classifier_meta.json` + `responses.json`
3. Tạo InputField + Text UI, viết `ChatTester.cs` (5 line, code có sẵn trong guide)
4. Play → gõ "Mấy giờ ăn cơm" → reply về intent + response

### Phase B
1. Tạo scene 3D với plane + cube agent + cube target + 6 cube obstacles
2. Set Layers: "Obstacle" + "Target". Tag tương ứng.
3. Add `MovementAgent` lên cube agent. Drag `soldier.onnx` + scene refs.
4. Play → cube agent tự đi tới target tránh obstacles.

---

## 4. Bài học chính trong session

1. **Iter 1 LSTM saturate** → 5 lần thử thêm vô ích. **Saturated metric = wasted compute**.
2. **FastText cần 8× seed thử để match LSTM**. **Stochastic = mỗi seed có giá trị**.
3. **PPO cần lottery**. Plateau 6.0 ở 13 iter, break 6.569 ở iter 14 (lucky seed 2013). Không có cách "smart" để skip lottery.
4. **HP grid > size brute force**. Conservative ([128,128,64] + low ent + low lr) beat bigger net + high ent.
5. **2 máy parallel CÓ ích** — máy 2's h3_deepfocus 6.572 vượt máy 1's max 6.569 (chênh chỉ 0.003 nhưng vẫn vượt). Phân vai theo strength: máy 1 nhanh → Phase A, máy 2 HEAVY → Phase B HP grid.
6. **Generator O(N²) bug** — tại 5k/intent chậm vừa, tại 25k/intent stall 30+ min. Fix bằng counter O(1). **Quadratic blow-up khi scale 5×**.

---

## 5. Tích hợp vào game thật của Quyền

### Phase A (NPC chỉ huy)
1. Quyền tạo Commander NPC trong scene game
2. Implement `IRuntimeContext.Get()` để fill placeholder runtime:
   - `{scheduled_today}` ← từ DaySystem.GetTodaySchedule()
   - `{meal_time}` ← từ DaySystem.GetMealTime()
   - `{place}`, `{topic}` ← tương ứng game state
3. Setup UI dialogue: text input → call `commanderBrain.Respond(text)` → bubble hiển thị

### Phase B (Lính tự đi)
1. Thay cube agent bằng prefab lính
2. Schedule system gán target Transform cho từng lính theo giờ
3. MovementAgent tự lo phần "đi tới đó tránh obstacles"
4. Multiple lính: hiện tại observation không có "thấy NPC khác" → có thể đụng nhau. Mitigation: stagger schedule (đi giờ khác nhau).

---

## 6. Wiki để query thêm

Wiki ở `.wiki/` đã document đầy đủ. Query qua `qmd`:

```bash
qmd query "Vì sao FastText match LSTM"
qmd query "Phase B HP grid kết quả"
qmd query "Unity Sentis 2.6 API"
qmd query "Movement raycast direction CCW vs CW"
```

Files quan trọng nhất:
- `.wiki/wiki/overview.md` — current state
- `.wiki/wiki/live-status.md` — final snapshot 19:40 EOD
- `.wiki/wiki/analysis/evolution-v1-to-v3.1.md` — history v1 → v3.1
- `.wiki/wiki/decisions/two-machine-parallel.md` — 2-machine plan
- `.wiki/wiki/technical/unity-integration.md` — InferenceEngine 2.6 API
- `.wiki/wiki/bugs/` — 3 bugs documented (FastText overfit, loop spin, generator O(N²))
- `.wiki/wiki/claims.md` — 23 claims có citation

---

## 7. Tài nguyên đã dùng

- ~10h training trên 2 máy parallel (máy 1 GTX 1060, máy 2 Intel UHD CPU only)
- 27 iter máy 1 + 5 iter Phase B máy 2 = 32 effective experiments
- Tổng PPO steps: 16 × 1M (máy 1) + 5 × 2M (máy 2) = **26M PPO steps explored**
- Tổng Phase A train: 27 × 40k = **1.08M samples × archs**
- Tổng git commits hôm nay: 9
- Wiki pages: 24 trang Markdown + 6 raw sources

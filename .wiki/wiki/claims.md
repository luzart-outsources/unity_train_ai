---
title: Claims Ledger
category: meta
created: 2026-05-07
updated: 2026-05-07
---

# Claims — Cross-page facts với citation

Mỗi claim có ID stable `c-YYYYMMDD-NN`. Khi GDD revision sửa giá trị → KHÔNG xóa, append vào [[contradictions]].

## Active claims

### c-20260507-01 — Unity engine version
- **Claim**: Project dùng Unity 6 (`6000.2.8f1`), KHÔNG phải Unity 2022.3.62f2 như v2 context dự kiến.
- **Sources**: `D:\OutSources\Unity_AI\TrainAI_Unity\ProjectSettings\ProjectVersion.txt`
- **Used by**: [[overview]], [[technical/unity-integration]], [[systems/sentis-chat]], [[systems/movement-ai]]
- **Status**: active

### c-20260507-02 — Sentis package renamed
- **Claim**: Sentis trong Unity 6 = `com.unity.ai.inference 2.6.1`, namespace `Unity.InferenceEngine` (KHÔNG còn `Unity.Sentis`).
- **Sources**: `Packages/manifest.json`
- **Used by**: [[technical/unity-integration]], [[entities/commander-npc]], [[entities/soldier-npc]]
- **Status**: active

### c-20260507-03 — Phase A baseline overfit
- **Claim**: V2 baseline FastText train 30 epochs → val_acc 1.00 nhưng real-world test 1/8 = 12.5%.
- **Sources**: [[analysis/evolution-v1-to-v3.1]], `raw/gdd/context_v2.md` mục 6
- **Used by**: [[bugs/fasttext-overfit-narrow-test]], [[decisions/eval-set-must-be-real]]
- **Status**: active (historical)

### c-20260507-04 — Phase A 8 intent (chưa Quyền duyệt)
- **Claim**: 8 intent đang dùng: HOI_LICH, HOI_GIO_AN, HOI_VI_TRI, HOI_KIEN_THUC, BAO_CAO, XIN_PHEP, TAM_BIET, OUT_OF_SCOPE.
- **Sources**: `raw/gdd/context_v2.md` mục 5; `phase_a_sentis/data/intents.csv`
- **Used by**: [[systems/sentis-chat]], [[entities/commander-npc]]
- **Status**: ⚠️ chờ Quyền confirm. Xem [[open-questions]].

### c-20260507-05 — FastText 25%, LSTM 95.3%, Transformer 18.8% trên test 64 câu
- **Claim**: Trên eval set 64 câu (slang/telex/compound/OOD), FastText fail (25%), LSTM thắng (95.3%), Transformer cũng fail (18.8%).
- **Sources**: Eval run 09:08 7/5/2026, `phase_a_sentis/models/eval_realworld.json`
- **Used by**: [[technical/architecture-comparison]], [[decisions/lstm-canonical-not-fasttext]], [[bugs/fasttext-overfit-narrow-test]]
- **Status**: active

### c-20260507-06 — LSTM 98.4% với 40k samples (v3.1 iter 1)
- **Claim**: Phase A LSTM train trên dataset v3.1 (40,000 samples, 5000/intent) đạt 63/64 = 98.4% trên 64-câu test.
- **Sources**: `overnight_v3.log` iter 1 09:06:35, `phase_a_sentis/models/eval_iter1_lstm.json`
- **Used by**: [[overview]], [[systems/sentis-chat]], [[analysis/evolution-v1-to-v3.1]]
- **Status**: active (best so far)

### c-20260507-07 — Phase B v2 best 6.126 (fixed env)
- **Claim**: V2 PPO best mean_reward = 6.126 ở iter 8 (200k steps, fixed 6 obstacles, net [64,64]).
- **Sources**: `phase_b_movement/logs/training_runs.csv` row `iter8_s207`
- **Used by**: [[systems/movement-ai]], [[analysis/evolution-v1-to-v3.1]]
- **Status**: active (legacy benchmark — env khác v3 nên không thể so sánh trực tiếp)

### c-20260507-08 — Phase B obs contract 21 floats
- **Claim**: Phase B observation = 21 floats, layout cố định trong `deliverables/soldier.meta.json`. Bất kỳ thay đổi obs nào trong Python phải retrain + cập nhật meta.
- **Sources**: `phase_b_movement/scripts/nav_env.py::_obs()`, `phase_b_movement/scripts/export_onnx.py::main()`
- **Used by**: [[systems/movement-ai]], [[entities/soldier-npc]], [[technical/unity-integration]]
- **Status**: active

### c-20260507-09 — Loop spin bug v2
- **Claim**: V2 overnight loop spin 493,895 iter trong 1 phút cuối deadline do thiếu sleep guard.
- **Sources**: `raw/technical/overnight_v2.log`
- **Used by**: [[bugs/loop-spin-near-deadline]]
- **Status**: fixed in v3.1 loop (`SPIN_GUARD_SLEEP = 30`)

### c-20260507-10 — Cached pre-tokenize 8.7× speedup
- **Claim**: Optimize `dataset.py::IntentDataset.__init__` pre-encode toàn bộ texts → train time giảm từ 558s → 64s với 16k samples × 30 epochs FastText.
- **Sources**: Smoke runs trước/sau optimize ở 07:42 vs 07:36 (overnight session)
- **Used by**: [[technical/training-pipeline]]
- **Status**: active

### c-20260507-11 — FastText với 40k data leo lên 95.3%
- **Claim**: FastText train trên dataset v3.1 (40k samples, 5000/intent) đạt 61/64 = 95.31% trên 64-câu test, **huge jump từ 25% với 16k data**. Vẫn dưới LSTM 98.4% nhưng không còn "broken".
- **Sources**: `overnight_v3.log` iter 2 09:42:49, `phase_a_sentis/models/eval_iter2_fasttext.json`
- **Used by**: [[technical/architecture-comparison]], [[bugs/fasttext-overfit-narrow-test]]
- **Status**: active
- **Notes**: Bài học — FastText không "fundamentally broken", chỉ cần đủ vocab diversity. Mean-pool có ceiling vì mất thứ tự, nhưng ceiling không phải 25%.

### c-20260507-12 — File name collision `fasttext_intent.onnx`
- **Claim**: 2 chỗ trong loop ghi vào `deliverables/fasttext_intent.onnx`: per-arch fasttext block (FastText weights) và OVERALL block khi LSTM win (LSTM weights via legacy alias). File flip-flop giữa 2 nội dung.
- **Sources**: `overnight_loop.py::phase_a_iter()` block per-arch (line ~165) và OVERALL block (line ~180)
- **Used by**: [[live-status]]
- **Mitigation**: dùng `intent_classifier.onnx` (canonical name, không bị conflict)
- **Status**: known issue, low priority — fix sau v3.1 done

### c-20260507-13 — Transformer cũng đạt 95.3% với 40k data
- **Claim**: Transformer trained on v3.1 40k data (iter 3) đạt 61/64 = 95.31% — HUGE jump từ 18.8% với 16k. Cùng kết quả với FastText. LSTM vẫn nhỉnh hơn (98.44%).
- **Sources**: `overnight_v3.log` iter 3 10:30:48, `phase_a_sentis/models/eval_iter3_transformer.json`
- **Used by**: [[technical/architecture-comparison]], [[analysis/evolution-v1-to-v3.1]]
- **Status**: active
- **Notes**: Confirms data-scale-over-arch hypothesis. Cả 3 archs đều benefit ngang nhau từ data scale, chỉ LSTM có +3pp lợi thế architecture (giữ thứ tự từ).

### c-20260507-14 — Phase B PPO break 6.011 (iter 2)
- **Claim**: Phase B v3.1 iter 2 (1M steps, [128,128] net, random env 3-12 obstacles) đạt mean_reward = 6.011, vượt iter 1 = 5.572.
- **Sources**: `overnight_v3.log` 10:14:58, `training_runs.csv` row v3_iter2_s2001
- **Used by**: [[systems/movement-ai]], [[live-status]]
- **Status**: active (current best)

### c-20260507-15 — 2-machine parallel HEAVY plan
- **Claim**: Plan máy 2 chạy HEAVY config: 200k samples (5× máy 1) + 2M PPO × cycle 4 HP configs (h1-h4). Output vào `deliverables_m2/`. End-of-day `merge_machine_results.py` auto-pick winner.
- **Sources**: `overnight_loop_machine2.py`, `merge_machine_results.py`
- **Used by**: [[decisions/two-machine-parallel]], [[live-status]]
- **Status**: ✅ deployed on máy 2 từ 11:19:07 (sau khi setup uv venv + fix generator bug, xem c-20260507-16). Iter 1 đang chạy.

### c-20260507-16 — Generator O(N²) blocker
- **Claim**: `generate_dataset_v3.py:746` dùng `len([r for r in rows if r["intent"] == intent])` trong while condition → quadratic theo `args.per_intent`. Tại 5000/intent ~30s acceptable; tại 25000/intent (HEAVY máy 2) **30+ min**, vượt timeout 600s. Fix bằng counter `kept` O(1): 200k samples 30+min → 3sec.
- **Sources**: Smoke test máy 2 11:09 (stall 7+ min) → 11:18 fix → 11:19 verify
- **Used by**: [[bugs/generator-on2-quadratic]], [[live-status]], [[decisions/two-machine-parallel]]
- **Status**: fixed, pushed commit `1cf36ff`. Máy 1 được hưởng sau git pull.
### c-20260507-17 — Phase B máy 2 h1_baseline = 6.236 (intermediate)
- **Claim**: Phase B HEAVY trên máy 2 (h1_baseline iter 1, 2M PPO steps, net [128,128] ent 0.01 lr 3e-4, seed 7000) đạt mean_reward = **6.236**, vượt máy 1's best 6.011. Đầu tiên prove "2M PPO > 1M PPO" với cùng HP.
- **Sources**: `overnight_v3_m2_state.json` best_phase_b_per_hp.h1_baseline, log `[12:41:45] [B] mean_reward = 6.236`
- **Used by**: [[systems/movement-ai]], [[live-status]], [[decisions/two-machine-parallel]]
- **Status**: superseded by c-20260507-19 (h3_deepfocus 6.572). Vẫn là per-HP best của h1_baseline.
- **Notes**: h1 không phải HP tốt nhất ở scale 2M PPO. Cần tuning sâu hơn (xem c-19).

### c-20260507-18 — Skip Phase A trên máy 2 (decision)
- **Claim**: Phase A trên máy 2 luôn timeout: 200k samples × 35 epochs LSTM CPU > `PHASE_A_TRAIN_TIMEOUT=2400`s (40 min). Iter 1-3 đều fail rc=-1. Quyết định 14:03: set `PHASE_A_ARCHS=[]` + guard `if PHASE_A_ARCHS:` trong loop, máy 2 100% focus Phase B HP exploration. Restart loop từ state.iter=2 với resume HP cycle position. Edit cùng làm cho seed_a, seed_b, hp_idx resume được giữa các restart.
- **Sources**: 3 entries failed in `state.json["history"]`, log timestamps `[11:59:10]`, `[13:21:50]`, `[14:01:47]` đều `[A] train lstm failed rc=-1`
- **Used by**: [[live-status]], [[decisions/two-machine-parallel]]
- **Status**: active (loop đang chạy iter 3 h3_deepfocus với config mới)
- **Notes**: Lý do: máy 1 đã có canonical LSTM 98.4% từ iter 1, máy 2 không cần cạnh tranh Phase A. Lợi ích: từ ~4 iter → ~7 iter trong còn lại deadline.

### c-20260507-19 — Phase B PPO máy 1 best: 6.569 (iter 14)
- **Claim**: Phase B PPO máy 1 iter 14 (1M steps, [128,128] net, ent=0.01, lr=3e-4, seed 2013, tag `v3_iter14_s2013`) đạt mean_reward = **6.569** — vượt plateau ~6.0 đã giữ qua 13 iter trước. Jump 0.5 reward sau 14 lần thử seed.
- **Sources**: `overnight_v3.log` 17:14:55, `training_runs.csv` row v3_iter14_s2013
- **Used by**: [[systems/movement-ai]], [[live-status]], [[analysis/evolution-v1-to-v3.1]]
- **Status**: active (best Phase B máy 1, runner-up overall — chỉ thua máy 2 = 6.572 chênh 0.003)
- **Notes**: Lottery ticket effect — RL stochastic, nhiều seed thử dễ trúng.

### c-20260507-20 — FastText match LSTM ở 98.44% (máy 1 iter 17)
- **Claim**: Phase A FastText train iter 17 (40k samples seed 1016) đạt 63/64 = 98.44% — match LSTM iter 1's 98.44%. Trước đó FastText giữ 95.31% (iter 2) → 96.88% (iter 5) → **98.44% (iter 17)**. Cần 8 lần seed thử để FastText leo cùng mức.
- **Sources**: `overnight_v3.log` ~17:00 entries, `eval_iter17_fasttext.json`
- **Used by**: [[systems/sentis-chat]], [[technical/architecture-comparison]], [[analysis/evolution-v1-to-v3.1]], [[bugs/fasttext-overfit-narrow-test]]
- **Status**: active
- **Notes**: Final confirmation "data > arch given enough seed exploration". FastText (51K params) đủ để match LSTM (116K) khi data đa dạng + nhiều seed. Mean-pool architecture KHÔNG fundamentally limited tới 95% — chỉ stochastic.

### c-20260507-21 — Loop máy 1 kết thúc clean ở 18:59 (27 iters)
- **Claim**: Loop overnight_loop.py máy 1 chạy 09:00 → 18:59:21, total 27 iters. Spin guard (30s sleep) đã handle các iter near-deadline đúng — không spin millions như v2 bug. Process exit clean, no zombie python.
- **Sources**: `overnight_v3.log` `=== deadline reached === iterations: 27`
- **Used by**: [[live-status]], [[bugs/loop-spin-near-deadline]]
- **Status**: active (final)

### c-20260507-22 — Phase B máy 2 FINAL ⭐ WINNER OVERALL: h3_deepfocus = 6.572
- **Claim**: Sau full HP cycle (4 configs × 2 seeds), best Phase B máy 2 = **6.572** từ h3_deepfocus iter 7 seed 7006: net [128,128,64] (3-layer deeper), ent 0.005 (low exploration), lr 1e-4 (slow learning rate). Vượt máy 1 final 6.569 chỉ 0.003 (siêu sát) và máy 1 1M baseline 6.011 ~9.3%. Per-HP final máy 2: h3=6.572 ⭐ > h2=6.263 > h1=6.236 > h4=5.252.
- **Sources**: `overnight_v3_m2_state.json` best_phase_b = `{reward:6.572, hp:h3_deepfocus, iter:7, seed:7006, tag:m2_iter7_h3_deepfocus_s7006}`, log entry `[17:48] [B] mean_reward = 6.572`
- **Used by**: [[systems/movement-ai]], [[live-status]], [[decisions/two-machine-parallel]]
- **Status**: active (FINAL best across both machines, máy 2 winner thành canonical sau merge_machine_results.py)
- **Notes — Bài học HP**:
  - Conservative HP thắng: low ent + low lr + deeper net hơn brute-force capacity
  - Bigger net không bằng tuning HP cẩn thận: h4 [256,256] với ent=0.05 → flop 5.252
  - Variance cao đáng kể: h2 lần 1 (iter 2) = 5.675 → lần 2 (iter 6) = 6.263, chênh +0.59 chỉ do seed
  - ≥2 seeds/HP cần thiết cho HP grid đáng tin
  - 5 iter Phase B × 2M PPO steps = 10M total steps trong ~5h CPU (máy 2 Intel UHD)

### c-20260507-24 — Phase B coord system bugs (3 bugs)
- **Claim**: 3 bugs phát hiện khi setup Unity scene Phase B:
  1. `arenaDiagonal=28.28f` (cũ) → `35.36f` (= 25 × √2 match v3 random env)
  2. `Physics.CheckSphere` block hoàn toàn → `OverlapSphere` + slide projection (match Python env)
  3. **Turn rotation flipped** (Python right-hand CCW vs Unity left-hand CW): negate turn arg trong `transform.Rotate(0, -turn × ..., 0)`
- **Sources**: `Assets/AI/Scripts/MovementAgent.cs`, user screenshot 7/5/2026
- **Used by**: [[bugs/unity-integration-bugs]], [[systems/movement-ai]]
- **Status**: fixed, commits `174151d` (bug 1+2) và `ffcfd72` (bug 3)
- **Notes**: Bug 3 là CRITICAL — model output `turn=+0.5` nghĩa là CCW (rẽ trái) trong Python convention, Unity transform.Rotate dương = CW → rẽ phải đâm obstacle → kẹt.

### c-20260507-25 — Phase A tokenization mismatch (CRITICAL)
- **Claim**: C# whitespace tokenizer không match Python underthesea. Vocab có multi-word entries như `"ăn cơm"`, `"thủ trưởng"` (chứa space). Whitespace split → cả 2 thành UNK → ~30-50% tokens UNK → model fail.
- **Sources**: `Assets/AI/Scripts/NPCDialogueBrain.cs::Encode()`, `intent_classifier_meta.json` vocab
- **Used by**: [[bugs/unity-integration-bugs]], [[systems/sentis-chat]]
- **Fix**: greedy longest-match từ vocab keys (precompute `_maxMultiWordLen` trong ParseMeta). Commit `4214dc2`.
- **Status**: fixed, approximation underthesea (không hoàn hảo nhưng đủ tốt cho 8 intent)

### c-20260507-26 — Unity 6 Input System Package only
- **Claim**: Project ProjectSettings có `activeInputHandler: 1` (Input System Package only, không legacy). Hậu quả: `Input.GetKeyDown(KeyCode.Return)` silent fail, `StandaloneInputModule` không hoạt động đúng.
- **Sources**: `ProjectSettings/ProjectSettings.asset`, package `com.unity.inputsystem 1.14.2`
- **Used by**: [[technical/unity-integration]], [[bugs/unity-integration-bugs]]
- **Fix**:
  - Replace `Input.GetKeyDown` bằng `UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame`
  - Replace `StandaloneInputModule` bằng `InputSystemUIInputModule`
  - Wrap với `#if ENABLE_INPUT_SYSTEM` cho cross-compat
- **Status**: fixed, commit `b1a0000`

### c-20260507-27 — 1-click Editor menu setup
- **Claim**: `Assets/AI/Editor/AITestSceneBuilder.cs` provide 3 menu items để build pre-built test scenes. Editor tự tạo Layers + Tags trong TagManager.asset, full Canvas UI hierarchy cho Phase A, full 3D scene cho Phase B. User mở scene là thấy mọi GameObject trong Hierarchy.
- **Sources**: `Assets/AI/Editor/AITestSceneBuilder.cs`
- **Used by**: [[technical/unity-integration]]
- **Status**: deployed, scenes commit ở `7b7f228`

### c-20260507-23 — Resume protocol cho overnight_loop_machine2
- **Claim**: `overnight_loop_machine2.py` được patch để resume từ state.json sau restart: `seed_a = 5000 + state["iter"]`, `seed_b = 7000 + state["iter"]`, `hp_idx = state["iter"]`. Đảm bảo HP cycle position không reset về h1 sau mỗi restart, seed không trùng. Critical cho safety khi cần kill+restart giữa chừng (như đã làm 14:03 để skip Phase A).
- **Sources**: `overnight_loop_machine2.py:250-253`, commit `ff5affc`
- **Used by**: [[decisions/two-machine-parallel]]
- **Status**: active
- **Notes**: Test thực tế: restart từ state.iter=2 → loop start iter 3 với hp_idx=2 → h3_deepfocus đầu tiên ✓

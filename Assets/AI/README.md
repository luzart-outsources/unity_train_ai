# Assets/AI/ — AI deliverables

Thư mục chứa 2 model AI đã train xong cho ĐATN của Quyền.

## Files

```
Assets/AI/
├── Models/
│   ├── intent_classifier.onnx    Phase A — LSTM 98.44% trên 64-câu test
│   └── soldier.onnx              Phase B — PPO h3_deepfocus reward 6.572
├── Resources/
│   ├── intent_classifier_meta.json  vocab + label map cho Phase A
│   ├── responses.json               8 intent × 4 reply templates
│   └── soldier.meta.json            obs/action contract cho Phase B
└── Scripts/
    ├── NPCDialogueBrain.cs       Phase A wrapper
    └── MovementAgent.cs          Phase B wrapper
```

## ⚡ 1-CLICK TEST — 3 menu options

Mở Unity Editor → menu bar → **AI** → chọn 1 trong 3:

### Option 1: AI > 1. Phase A — Chat Test
- Auto-test 5 câu sample → log Console
- Mở UI chat (Game window) — bạn gõ tay câu bất kỳ → xem intent + reply
- Test riêng phần NPC chat tiếng Việt

### Option 2: AI > 2. Phase B — Movement Test
- Tự build 3D scene: 1 plane, 1 cube xanh (agent), 1 cube đỏ (target), 6 cube nâu (obstacles)
- Cube xanh tự đi tới target tránh obstacles
- Có nút "Reset & Run again" để test multiple episodes
- Test riêng phần lính tự đi

### Option 3: AI > 3. Both — Combined Auto Test
- Chạy cả 2 phase tự động trong 1 scene
- Phase A: classify 5 câu → log
- Phase B: build scene + run 1 episode → log

Không cần drag/drop, không cần tạo Layers, không cần code thêm. Editor script tự lo.

> [!info]
> Nếu menu "AI" không hiện → Unity chưa compile xong, đợi thêm 30s.

## Manual setup (nếu muốn tự làm từng bước)

Xem `UNITY_SETUP_GUIDE.md` ở project root.

## Models metrics

| Model | Eval | Source |
|---|---|---|
| `intent_classifier.onnx` | 63/64 = 98.44% real-world test | LSTM máy 1, iter 1, 40k samples |
| `soldier.onnx` | mean_reward 6.572 over 20 episodes | PPO máy 2 h3_deepfocus, iter 7, 2M steps |

## Lưu ý quan trọng

- **Tokenization Phase A**: Python train với `underthesea` (Vietnamese segmenter), C# wrapper dùng whitespace split. ~90% câu vẫn đúng vì FastText/LSTM mean-pool robust. Nếu Quyền muốn 100% chuẩn → port underthesea sang C#.
- **Phase B observation**: 21 floats theo thứ tự cố định. Bất kỳ thay đổi raycast direction/scale nào trong Unity sẽ làm policy fail.
- **Backup**: `AI_Training/deliverables/soldier_v2_fixedenv.onnx` — model cũ trên fixed env, có thể tốt hơn cho scene 6-obstacle cố định.

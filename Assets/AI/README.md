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

## ⚡ 1-CLICK TEST (~30 giây)

1. **Mở Unity Editor** với project này
2. Đợi import xong (~30s lần đầu)
3. Menu bar → **AI** → **Build & Run Test Scene**
4. Đợi ~5 giây — scene auto build + Play tự bật
5. **Console** hiển thị kết quả test:
   - Phase A: 5 câu intent classification (target ≥4/5 đúng)
   - Phase B: agent đi tới target trong ~30 giây

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

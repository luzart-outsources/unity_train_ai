# Unity Setup Guide — Test 2 model AI

Hướng dẫn step-by-step để bạn (hoặc Quyền) tự test 2 model AI trong Unity 6.

## Yêu cầu

- ✅ Unity 6 (`6000.2.8f1`) đã cài
- ✅ Project mở được — `D:\OutSources\Unity_AI\TrainAI_Unity\`
- ✅ Package `com.unity.ai.inference 2.6.1` (đã có trong `Packages/manifest.json`)
- ✅ Files trong `Assets/AI/` (đã chuẩn bị sẵn)

---

## Phần 1 — Test Phase A (NPC Chat tiếng Việt)

### 1.1. Tạo scene test mới

1. Unity Editor → File → **New Scene** → chọn **Basic (Built-in)** → Save với tên `Assets/Scenes/Test_NPCChat.unity`
2. Trong Hierarchy, click chuột phải → **Create Empty** → đổi tên thành **Commander**

### 1.2. Gắn NPCDialogueBrain

1. Chọn GameObject `Commander`
2. Trong Inspector → **Add Component** → gõ "NPCDialogueBrain" → chọn

3. Drag các asset từ Project window vào Inspector của `NPCDialogueBrain`:
   - **Model Asset** ← drag `Assets/AI/Models/intent_classifier.onnx`
   - **Meta Json** ← drag `Assets/AI/Resources/intent_classifier_meta.json`
   - **Responses Json** ← drag `Assets/AI/Resources/responses.json`

4. (Tùy chọn) trong Inspector:
   - **Backend**: GPUCompute (nhanh) hoặc CPU (compatible)
   - **Min Confidence**: 0.40 (default OK)
   - **Fallback Intent**: OUT_OF_SCOPE (default OK)

### 1.3. Tạo UI test đơn giản

1. Hierarchy → click chuột phải → **UI** → **Canvas**
2. Trong Canvas → click chuột phải → **UI** → **Input Field - TextMeshPro** (nếu hỏi import TMP Essentials → click Import)
3. Trong Canvas → **UI** → **Text - TextMeshPro** (đổi tên thành **ReplyText**)
4. Tạo GameObject mới (Empty) trong Canvas, đặt tên **ChatTester**
5. Tạo file `Assets/AI/Scripts/ChatTester.cs`:

```csharp
using UnityEngine;
using TMPro;

public class ChatTester : MonoBehaviour
{
    public NPCDialogueBrain brain;
    public TMP_InputField input;
    public TMP_Text replyText;

    void Start()
    {
        if (input != null) input.onSubmit.AddListener(OnSubmit);
    }

    void OnSubmit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var (intent, conf) = brain.Classify(text);
        string reply = brain.Respond(text);
        replyText.text = $"<b>Intent:</b> {intent} ({conf*100:F1}%)\n<b>Reply:</b> {reply}";
        input.text = "";
        input.ActivateInputField();
    }
}
```

6. Gắn `ChatTester` lên GameObject ChatTester. Drag `Commander` vào field **brain**, drag InputField vào **input**, drag ReplyText vào **replyText**.

### 1.4. Test

1. Click **Play** (Ctrl+P)
2. Click vào InputField, gõ:
   - `"Mấy giờ ăn cơm"` → expect intent `HOI_GIO_AN`
   - `"Hôm nay có lịch gì"` → `HOI_LICH`
   - `"Wifi yếu quá"` → `OUT_OF_SCOPE`
   - `"Cho em xin nghỉ"` → `XIN_PHEP`
   - `"Báo cáo đại đội đầy đủ"` → `BAO_CAO`
3. Bấm Enter → ReplyText hiện intent + reply.

> [!tip]
> Reply có placeholder như `{scheduled_today}` không được fill = bình thường — đó là chỗ Quyền implement `IRuntimeContext` để map sang game state thực tế. Xem `NPCDialogueBrain.cs::DummyContext` cho ví dụ.

### 1.5. Troubleshooting Phase A

| Vấn đề | Fix |
|---|---|
| Compile error: `Unity.InferenceEngine` not found | Check Packages → `com.unity.ai.inference` version ≥ 2.6.0. Reinstall nếu cần. |
| `ModelAsset` field empty | Drag `intent_classifier.onnx` từ Project window. Nếu file không hiện as ModelAsset → restart Unity. |
| TextAsset không hiển thị `.json` files | Unity tự import `.json` thành TextAsset trong Resources/. Đợi Unity refresh sau khi paste files. |
| Reply toàn `OUT_OF_SCOPE` | Test với câu rõ keyword như "Mấy giờ ăn cơm" trước. Nếu vẫn fail → check meta.json đúng file. |
| Tensor disposal warning | Đảm bảo `OnDestroy()` gọi `_worker?.Dispose()`. Code mẫu đã có. |

---

## Phần 2 — Test Phase B (Lính tự đi)

### 2.1. Tạo Layers + Tags

1. Unity Editor → Edit → **Project Settings** → **Tags and Layers**
2. **Layers**:
   - Layer 6: `Obstacle`
   - Layer 7: `Target`
3. **Tags**:
   - `Target` (cho Target GameObject)
   - `Obstacle` (cho mỗi obstacle)

### 2.2. Tạo scene test

1. File → **New Scene** → **3D Built-in** → save `Assets/Scenes/Test_Movement.unity`
2. Trong Hierarchy:
   - **Plane** (Floor): Create → 3D Object → Plane. Scale (3, 1, 3). Position (0, 0, 0).
   - **Cube Agent**: Create → 3D Object → Cube. Đổi tên `Agent`. Position (-8, 0.5, -8). Scale (1, 1, 1).
   - **Cube Target**: Create → 3D Object → Cube. Đổi tên `Target`. Position (8, 0.5, 8). Scale (1, 1, 1). Đổi material → màu đỏ. Set Tag = `Target`, Layer = `Target`.
   - **Cubes Obstacle x 6**: tạo 6 cube random position trong (-7..7, 0.5, -7..7). Scale (1.5, 1, 1.5). Tag = `Obstacle`, Layer = `Obstacle`.

### 2.3. Gắn MovementAgent

1. Chọn GameObject `Agent`
2. Inspector → **Add Component** → "MovementAgent"
3. Drag Models:
   - **Model Asset** ← drag `Assets/AI/Models/soldier.onnx`
4. Drag Scene refs:
   - **Target** ← drag GameObject Target
   - **Obstacle Layer** ← chọn Layer "Obstacle" trong dropdown
   - **Target Layer** ← chọn Layer "Target"
5. Tuning (giữ defaults):
   - Max Speed: 3.5
   - Max Turn Rad Per Sec: 3.14
   - Ray Max Dist: 10
   - Decision Dt: 0.1

### 2.4. Test

1. Click **Play**
2. Cube Agent sẽ tự di chuyển → tránh obstacles → tiến tới Target
3. Khi Agent chạm Target → episode xong (Agent đứng im hoặc reset tùy logic)

### 2.5. Troubleshooting Phase B

| Vấn đề | Fix |
|---|---|
| Agent đứng im | Check raycast layers: Obstacle + Target layers có set đúng không. Check `target` Inspector ref. |
| Agent đi xoắn ốc, không tới đích | Raycast direction CCW vs CW khác nhau giữa Python và Unity. Code sample đã handle (`-right` thay vì `right`). Nếu vẫn xoắn → check Agent forward axis (mặc định Z+ trong Unity). |
| Agent đụng obstacle suốt | Có thể `obstacle_size` trong scene quá to so với train (training: 1.0-2.5m). Scale obstacle về 1.5 max. |
| ONNX inference error | Check shape: input "obs" phải [1, 21] float. Nếu Unity log "shape mismatch" → ComputeObservation bug. |

---

## Phần 3 — Verify metrics (sanity check)

Sau khi setup xong cả 2 scene, để verify đúng model expected metrics:

### 3.1. Phase A — test 5 câu

Gõ 5 câu trong UI test, expect:

| Input | Expected Intent | Confidence |
|---|---|---|
| "Mấy giờ thì ăn cơm" | HOI_GIO_AN | ~99% |
| "Phòng học ở đâu vậy" | HOI_VI_TRI | ~99% |
| "Em xin phép về quê" | XIN_PHEP | ~99% |
| "Wifi yếu quá" | OUT_OF_SCOPE | ~99% |
| "Súng AK47 dùng thế nào" | HOI_KIEN_THUC | ~99% |

5/5 đúng → model OK.

### 3.2. Phase B — test 1 episode

1. Play scene
2. Cube Agent đi tới Target trong < 30 giây (~500 step × 0.1s = 50s timeout)
3. Reach target = success

Nếu Agent không đi tới được trong 50s → có thể seed scene khó, hoặc raycast bug. Reset scene + thử lại 3 lần.

---

## Phần 4 — Tích hợp vào game thật của Quyền

Sau khi 2 scene test pass:

1. **Phase A**: Quyền tạo Commander NPC trong scene game. Implement `IRuntimeContext.Get()` để fill placeholder (`scheduled_today`, `meal_time`, `place`...) từ DaySystem/PlayerNearestPlace.
2. **Phase B**: Thay cube agent bằng prefab lính. Schedule system gán target cho từng lính theo giờ (06:00 đi nhà ăn, 07:00 lên lớp...). MovementAgent tự di chuyển.

Chi tiết: xem `.wiki/wiki/entities/commander-npc.md` và `.wiki/wiki/entities/soldier-npc.md`.

---

## Phần 5 — Nếu test fail

Nếu fail và bạn không chắc nguyên nhân, **gửi log Unity Console + screenshot Inspector** cho tôi để debug. Common issues:

| Symptom | Likely cause |
|---|---|
| Compile error namespace | InferenceEngine version cũ → upgrade |
| ModelAsset = null | File chưa import → Refresh Project |
| Tensor shape error | Code Python vs C# obs layout mismatch |
| Agent crash | Raycast layer mask sai |

Backup model: `AI_Training/deliverables/soldier_v2_fixedenv.onnx` (smaller, fixed env training, có thể tốt hơn cho scene đơn giản 6 obstacles).

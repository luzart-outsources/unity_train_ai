---
title: Unity Integration (Sentis 2.6)
category: technical
tags: [unity, sentis, inference-engine, onnx, csharp]
sources: [raw/gdd/context_v2.md, raw/technical/handoff_morning.md]
created: 2026-05-07
updated: 2026-05-07
---

# Unity Integration — Loading ONNX qua InferenceEngine 2.6

Unity 6 đã rename `com.unity.sentis` thành `com.unity.ai.inference`. Project `manifest.json` đã có:
```json
"com.unity.ai.inference": "2.6.1"
```

Namespace mới: `Unity.InferenceEngine` (KHÔNG còn `Unity.Sentis`).

## Phase A — NPC Dialogue Brain

File deliverable: `deliverables/NPCDialogueBrain.cs`. Setup trong Unity:

1. Tạo folder `Assets/AI/`
2. Drag `intent_classifier.onnx` vào → Unity import thành `ModelAsset`
3. Drag `intent_classifier_meta.json` + `responses.json` vào → `TextAsset`
4. Tạo GameObject "Commander", Add Component → `NPCDialogueBrain`
5. Inspector kéo 3 asset vào tương ứng `modelAsset` / `metaJson` / `responsesJson`
6. Implement `IRuntimeContext` (game state lookup)
7. Gọi `commanderBrain.Respond("Mấy giờ ăn cơm?")`

### Key API patterns (Sentis 2.x)

```csharp
using Unity.InferenceEngine;

// Load
_model  = ModelLoader.Load(modelAsset);
_worker = new Worker(_model, BackendType.GPUCompute);   // hoặc CPU

// Inference
using var input = new Tensor<int>(new TensorShape(1, maxLen), ids);
_worker.Schedule(input);
var logitsT = _worker.PeekOutput("logits") as Tensor<float>;
var logits  = logitsT.DownloadToArray();   // copy về CPU

// Cleanup (BẮT BUỘC)
void OnDestroy() => _worker?.Dispose();
```

> [!warning] Tensor disposal
> Mọi `Tensor<T>` phải được Dispose. Dùng `using` hoặc explicit `tensor.Dispose()`. Quên Dispose → leak GPU memory, Unity sẽ crash sau vài phút.

> [!info] Sentis 2.x vs 1.x
> Sentis 1.x dùng `IWorker WorkerFactory.CreateWorker(...)`. Sentis 2.x đơn giản hóa thành `new Worker(model, backend)`. Code cũ trên forum có thể không compile — kiểm tra version.

## Phase B — Movement Agent

File deliverable: `deliverables/MovementAgent.cs`. Setup:

1. Drag `soldier.onnx` vào `Assets/AI/`
2. Tạo Layer "Obstacle" + "Target" trong Layer settings
3. Tạo scene test:
   - Floor plane Y=0
   - Cube agent có Component `MovementAgent`
   - Cube target tag "Target" trên Layer "Target"
   - 3-6 cube obstacle tag "Obstacle" trên Layer "Obstacle"
4. Inspector:
   - `modelAsset` = soldier.onnx
   - `target` = transform của target cube
   - `obstacleLayer` = "Obstacle"
   - `targetLayer` = "Target"
5. Play → cube agent tự đi tới target, vòng tránh obstacle

### Observation pipeline

`MovementAgent.ComputeObservation()` build vector 21 floats theo đúng layout `soldier.meta.json`. Critical: 8 raycast theo thứ tự **CCW từ forward**, normalize bằng `ray_max_dist=10m` đúng như training. Bất kỳ sai số layout nào → policy hỏng.

```csharp
for (int i = 0; i < 8; i++) {
    float angle = (i / 8f) * 2f * Mathf.PI;
    Vector3 dir = Mathf.Cos(angle) * forward + Mathf.Sin(angle) * (-right);
    // -right là vì CCW positive trong numpy, Unity right là CW
    if (Physics.Raycast(pos, dir, out hit, 10f, obstacleLayer | targetLayer)) {
        outBuf[i]     = hit.distance / 10f;
        outBuf[i + 8] = ((1 << hit.collider.gameObject.layer) & targetLayer) != 0 ? 1f : 0f;
    } else {
        outBuf[i]     = 1f;
        outBuf[i + 8] = 0f;
    }
}
```

### Decision rate

10 Hz (mỗi 0.1s) — match dt training. Trong `FixedUpdate` accumulate `Time.fixedDeltaTime`, gọi inference khi >= 0.1s.

> [!tip] Performance
> Inference mỗi 100ms nhẹ tênh (model ~80KB, 21-dim input). Không cần threading. Đặt `BackendType.CPU` cho NPC nền, `GPUCompute` chỉ khi có hàng trăm agent.

## Common pitfalls

| Lỗi | Nguyên nhân | Fix |
|---|---|---|
| `using Unity.Sentis` không tìm thấy | Unity 6 dùng `InferenceEngine` | Đổi sang `using Unity.InferenceEngine;` |
| ONNX không load | File không phải opset 15 | Re-export với `--opset 15` |
| Action toàn 0 | Tensor input chưa scale đúng | Verify từng index theo meta.json |
| Lính đi xoắn ốc / kẹt | Coord system mismatch | Xem [[bugs/unity-integration-bugs]] (4 bugs Phase B) |
| Phase A classify sai | Tokenization whitespace vs underthesea | Xem [[bugs/unity-integration-bugs]] bug 4 |
| `Input.GetKeyDown` không hoạt động | Unity 6 Input System only | Dùng `Keyboard.current.enterKey.wasPressedThisFrame` |
| `StandaloneInputModule` bug | Cần `InputSystemUIInputModule` | Editor builder tự dùng nếu `ENABLE_INPUT_SYSTEM` |
| Compile error tensor API | Sentis version cũ | Update `com.unity.ai.inference` lên 2.6+ |

## 1-click test setup (Editor menu)

Setup `Assets/AI/Editor/AITestSceneBuilder.cs` cung cấp 3 menu items để build test scene tự động:

```
AI / 1. Phase A — Chat Test
   → Tạo scene với Canvas UI đầy đủ:
     - Background, Header (Title + Status), ScrollView (chat history),
       InputRow (InputField + Send Button), EventSystem
     - Commander GameObject với NPCDialogueBrain + PhaseAChatUI
       (assets pre-assigned, refs wired)
   → Auto sanity test 5 câu, hiển thị bubbles có sub-info "expect ABC ✓/✗"
   → User gõ thêm câu, Enter submit

AI / 2. Phase B — Movement Test
   → Tạo scene 3D với:
     - Floor, Target (đỏ), Agent (xanh, MovementAgent attached),
       6 Obstacles (nâu), Camera top-down, StatusMonitor
   → Tags + Layers (Obstacle=6, Target=7) tự create vào TagManager.asset
   → Click Play: agent di chuyển tới target, status UI hiện distance/timer

AI / 3. Both — Combined
   → 1 scene chạy cả 2
```

Sau khi click menu, scene mở ra trong Editor với Hierarchy đầy đủ, click Play.

## Inspector verify checklist

Sau khi builder build xong, click GameObject và verify:

### Commander (Phase A)
- NPCDialogueBrain: Model Asset = `intent_classifier.onnx`, Meta Json + Responses Json không null
- PhaseAChatUI: titleText, statusText, contentParent, scrollRect, inputField, sendButton đều có ref

### Agent (Phase B)
- MovementAgent:
  - Model Asset = `soldier.onnx`
  - Target = transform của Target GameObject
  - Obstacle Layer = LayerMask "Obstacle" (= 64 nếu layer 6)
  - Target Layer = LayerMask "Target" (= 128 nếu layer 7)
  - Arena Diagonal = 35.36 (KHÔNG phải 28.28!)

### EventSystem
- Component = `InputSystemUIInputModule` (KHÔNG phải StandaloneInputModule)

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[systems/movement-ai]]
- [[entities/commander-npc]]
- [[entities/soldier-npc]]
- [[technical/training-pipeline]]

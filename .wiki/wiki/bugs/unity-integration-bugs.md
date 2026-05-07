---
title: Unity Integration Bugs (4 bugs from coord/tokenization mismatch)
category: bugs
tags: [unity, integration, coord-system, tokenization, input-system]
sources: []
created: 2026-05-07
updated: 2026-05-07
---

# Unity Integration Bugs

4 bugs phát hiện khi setup Unity scene để test 2 ONNX model. Tất cả đều do mismatch giữa Python training environment và Unity runtime convention.

## Bug 1: `arenaDiagonal` sai 25% (Phase B)

### Symptoms
Agent (cube xanh) không di chuyển hợp lý — đôi lúc tăng tốc lúc gần target, slow down khi xa, behavior khác training.

### Root cause
```
Python nav_env.py:
  self.diag = arena_max * sqrt(2) if randomize else arena_size * sqrt(2)
  
  v3 random env: arena_max = 25 → diag = 35.36
```

```csharp
// Unity MovementAgent.cs (cũ, sai)
public float arenaDiagonal = 28.28f;  // = 20 * sqrt(2)
```

Distance feature (obs[20]) normalize bằng `arenaDiagonal`. Sai 25% → model nhận sai signal về "đang gần bờ map" — ảnh hưởng quyết định turn/thrust.

### Fix
```csharp
public float arenaDiagonal = 35.36f;  // = 25 * sqrt(2) — match arena_max v3
```

## Bug 2: Collision blocks fully (Phase B)

### Symptoms
Agent đụng obstacle = đứng im mãi (kẹt). User screenshot: nhiều "ghost trails" cube xanh quanh cluster obstacles, không đến target.

### Root cause
```csharp
// CŨ
if (!Physics.CheckSphere(next, 0.5f, obstacleLayer))
    transform.position = next;
// → Nếu next overlap obstacle: KHÔNG move. Mà policy có thể command thrust forward
//   liên tục → next luôn overlap → kẹt forever.
```

Python env có behavior khác — slide ra cạnh obstacle:
```python
ox, oy = new_pos[0] - cx, new_pos[1] - cy
if abs(ox) > abs(oy):
    new_pos[0] = cx + sign(ox) * (h + r + 0.01)
else:
    new_pos[1] = cy + sign(oy) * (h + r + 0.01)
```

### Fix — port slide projection sang Unity
```csharp
Collider[] hits = Physics.OverlapSphere(next, agentRadius, obstacleLayer);
if (hits.Length > 0)
{
    var ob = hits[0];
    Vector3 obCenter = ob.bounds.center;
    float halfX = ob.bounds.extents.x;
    float halfZ = ob.bounds.extents.z;
    float ox = next.x - obCenter.x;
    float oz = next.z - obCenter.z;
    if (Mathf.Abs(ox) > Mathf.Abs(oz))
        next.x = obCenter.x + Mathf.Sign(ox) * (halfX + agentRadius + 0.01f);
    else
        next.z = obCenter.z + Mathf.Sign(oz) * (halfZ + agentRadius + 0.01f);
}
transform.position = next;  // luôn move
```

## Bug 3: Turn direction flipped (Phase B — left-hand vs right-hand) ⭐ CRITICAL

### Symptoms
Agent có move (ghost trails) nhưng rẽ ngược hướng → đâm vào obstacle này tới obstacle khác → kẹt cluster ở giữa map.

### Root cause
**Coordinate system mismatch giữa Python (right-hand math) và Unity (left-hand 3D)**:

```
Python                                Unity
─────                                 ─────
2D, right-hand                        3D top-down, left-hand
heading += turn × dt                  transform.Rotate(0, turn × dt × RAD2DEG, 0)
positive turn = CCW                   positive Y-rotation = CW từ trên xuống
                                      (looking from +Y down)
```

Khi model output `turn = +0.5`:
- Python interpretation: rẽ trái (CCW) — tránh obstacle bên phải
- Unity interpretation: rẽ phải (CW) — đâm thẳng obstacle bên phải!

### Fix — negate turn
```csharp
// CŨ:  transform.Rotate(0, turn * maxTurn * dt * RAD2DEG, 0, Space.World);
// MỚI: 
transform.Rotate(0f, -turn * maxTurnRadPerSec * dt * Mathf.Rad2Deg, 0f, Space.World);
```

Verify mapping after fix:
- Python heading=0 → Unity rotation=0 → forward = Unity +Z ✓
- Python heading=+π/2 (CCW 90°) → Python forward = +y → Unity forward = -X
- Unity Rotate(0, -90, 0) → forward goes +Z to -X ✓ MATCH

## Bug 4: Tokenization mismatch (Phase A) ⭐ CRITICAL

### Symptoms
Phase A model classify sai nhiều câu — sanity 5 câu test có thể chỉ 1-3 đúng (so với 5/5 expect).

### Root cause
Python train với `underthesea` (Vietnamese word segmenter):
```python
underthesea.word_tokenize("Mấy giờ ăn cơm")
→ ["mấy", "giờ", "ăn cơm"]   ← "ăn cơm" là 1 multi-word token!
```

Vocab có entries như `"ăn cơm"`, `"thủ trưởng"`, `"báo cáo"` — chứa space.

C# wrapper (cũ) split whitespace đơn giản:
```csharp
var tokens = text.Split(' ');  // ["mấy", "giờ", "ăn", "cơm"]
ids[i] = vocab.TryGetValue(tokens[i], out int id) ? id : unkId;
// "ăn" và "cơm" không có riêng trong vocab → cả 2 → UNK
```

Result: ~30-50% tokens là UNK → model fail.

### Fix — greedy longest-match
```csharp
int[] Encode(string text) {
    var words = text.Lowercase().StripPunct().Split(' ');
    int wi = 0, idIdx = 0;
    while (wi < words.Length && idIdx < maxLen) {
        // Thử ghép N..1 words liên tiếp, lấy longest match trong vocab
        int matchedSpan = 0;
        int matchedId = unkId;
        for (int span = Math.Min(maxMultiWordLen, words.Length - wi); span >= 1; span--) {
            string candidate = string.Join(" ", words, wi, span);
            if (vocab.TryGetValue(candidate, out int id)) {
                matchedSpan = span;
                matchedId = id;
                break;
            }
        }
        ids[idIdx++] = matchedSpan > 0 ? matchedId : unkId;
        wi += Math.Max(matchedSpan, 1);
    }
}
```

`maxMultiWordLen` precompute trong ParseMeta (từ vocab keys với space).

Approximation underthesea: không hoàn hảo (sai khi gặp từ ghép mới) nhưng đủ tốt cho 8 intent classify.

## Bug 5 (UX, not functional): UI Phase A IMGUI

User feedback: "UI ngu quá, phải setup sẵn như vật thể".

### Fix
Đổi từ IMGUI (`OnGUI()`) sang Canvas-based Unity UI:
- Editor builder tạo full Canvas hierarchy: Background + Header + ScrollView + InputRow
- All elements visible/inspectable trong Hierarchy trước Play
- Chat bubbles spawn runtime với `Image` + `Text` + `LayoutGroup`
- Auto-focus input + auto-scroll bottom
- Use OS Arial font (support Vietnamese diacritics)

## Bug 6 (Unity 6): Input System mismatch

Project có `activeInputHandler: 1` (Input System Package only). Legacy `Input.GetKeyDown` silent fail.

### Fix
- Replace `Input.GetKeyDown(KeyCode.Return)` bằng `Keyboard.current.enterKey.wasPressedThisFrame`
- Replace `StandaloneInputModule` bằng `InputSystemUIInputModule`
- Wrap với `#if ENABLE_INPUT_SYSTEM` cho cross-compat

## Lessons learned

1. **Coord system mapping** (Python 2D right-hand → Unity 3D left-hand) là source of bugs phổ biến nhất khi port RL policy
2. **Quadratic complexity hidden trong list comprehension** + Quadratic mappings (left-hand vs right-hand) đều đáng debug carefully khi scale
3. **Tokenization** trong serialization khoảng cách giữa Python (rich segmenter) và C# (basic split) cần explicit port — multi-word vocabulary entries không tự động hoạt động
4. **Unity 6 default = Input System Package only** — code Unity tutorial cũ (Input Manager based) sẽ silent fail
5. **Pre-build scene > runtime spawn** cho test scenes — user inspect được trong Editor

## Backlinks

- [[systems/movement-ai]] — coord mapping
- [[systems/sentis-chat]] — tokenization
- [[technical/unity-integration]] — full integration guide
- [[entities/soldier-npc]] — consumer Phase B
- [[entities/commander-npc]] — consumer Phase A

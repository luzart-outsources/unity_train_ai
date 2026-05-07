---
title: Commander NPC
category: entities
tags: [npc, commander, sentis, dialogue]
sources: [raw/gdd/context_v2.md]
created: 2026-05-07
updated: 2026-05-07
---

# Commander NPC (Sĩ quan chỉ huy)

NPC duy nhất có dialogue trong game demo. Đứng cạnh sân điều lệnh / phòng chỉ huy, người chơi tới gần → bấm E mở dialogue UI → gõ tiếng Việt → NPC phân loại intent + reply.

## Behavior

1. **Idle**: stand-and-look-around animation (Quyền lo)
2. **OnInteract**: open dialogue UI, focus text input
3. **OnSubmit**: gọi `NPCDialogueBrain.Respond(userText)` → trả về reply string
4. **OnReply**: hiển thị reply trong UI bubble, nói voice ASR (optional, không bắt buộc cho ĐATN)

## AI dependency

Hoàn toàn phụ thuộc [[systems/sentis-chat|Sentis Chat]]. Khi load scene, `Awake()` của `NPCDialogueBrain` load:
- `intent_classifier.onnx` (ModelAsset)
- `intent_classifier_meta.json` (TextAsset, parse vocab + label map)
- `responses.json` (TextAsset, parse intent → list[reply template])

Code mẫu Quyền: `deliverables/NPCDialogueBrain.cs`.

## Runtime context (Quyền implement)

`responses.json` có placeholder. Quyền implement `IRuntimeContext.Get(key)` trả về string thực tế từ game state:

```csharp
public class GameContext : IRuntimeContext {
    public string Get(string key) {
        switch (key) {
            case "scheduled_today": return DaySystem.GetTodaySchedule();
            case "meal_time":       return DaySystem.GetMealTime();
            case "place":           return Player.NearestPlaceName();
            case "topic":           return LessonSystem.CurrentTopic();
            // ...
            default: return null;
        }
    }
}
```

> [!warning] Common bug
> Nếu `responses.json` có `{scheduled_today}` mà context.Get trả null → wrapper điền `[scheduled_today]` (placeholder visible). Test integration phải verify ALL placeholder keys.

## UX flow đề xuất

1. Player ấn E → fade-in dialogue panel
2. Type "Mấy giờ ăn cơm?" → submit (Enter)
3. Loading 30ms (inference) → reply hiện ra
4. Esc hoặc bấm Tab để đóng

Không cần TTS, không cần animation phức tạp.

## Confidence threshold

`NPCDialogueBrain.minConfidence = 0.40f` — nếu top softmax prob < 40% → fallback `OUT_OF_SCOPE`. Tránh trường hợp model "đoán bừa" với confidence thấp gây hiểu nhầm.

## Backlinks

- [[overview]]
- [[systems/sentis-chat]]
- [[technical/unity-integration]]

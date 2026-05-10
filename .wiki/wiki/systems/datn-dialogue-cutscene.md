---
title: Dialogue & Cutscene (DATN)
category: systems
tags: [datn, dialogue, cutscene, scriptableobject, socollection]
sources: [Assets/Scripts/Dialogue/, Assets/Scripts/Cutscene/]
created: 2026-05-11
updated: 2026-05-11
---

# Dialogue & Cutscene — DATN

Hai system riêng nhưng tightly coupled: Cutscene chạy chuỗi action, một loại action là `SpeechAction` đẩy line vào DialogueManager. Cả hai dùng ScriptableObject + condition check qua [[systems/datn-blackboard-save|GameBlackboard]].

## Dialogue

### DialogueManager (singleton)

Queue-based. Khi NPC interact:
```csharp
DialogueManager.Instance.StartDialogue(DialogueLine[] lines, Action onEnd);
```

Flow:
1. Disable `PlayerController` (lock input)
2. Show dialogue panel UI
3. Coroutine: với mỗi line, type-out hiệu ứng từng ký tự
4. Player bấm Space/E → next line
5. Hết queue → fade panel, callback `onEnd`, re-enable Player

### DialogueLine

```csharp
class DialogueLine {
    string speaker;          // "Đại đội trưởng"
    [TextArea] string text;
    DialogueCondition[] conditions;  // optional gating
}
```

Designer tạo array `DialogueLine[]` trong inspector của `CharacterData` hoặc Cutscene.

### DialogueCondition

Wrap `BlackboardCondition`. Cho phép branching đơn giản:
- "Chỉ nói câu này nếu chưa unlock NPC X"
- "Nếu festival đang chạy → câu khác"

Không phải dialogue tree đầy đủ — game chỉ chạy linear queue, condition để filter.

## Cutscene

### Cutscene (ScriptableObject)

```csharp
class Cutscene : ScriptableObject, IBlackboardConditional {
    SoCollection<CutsceneAction> action;     // chuỗi action
    BlackboardCondition[] conditions;         // điều kiện trigger
    bool recurring;                            // re-trigger nếu match lại
}
```

Designer tạo cutscene asset, drop list action vào, set condition.

### CutsceneManager (singleton)

Khi `onLocationLoad` fire:
1. `Resources.LoadAll<Cutscene>("Cutscenes/" + location)` 
2. Filter theo `conditions` → cutscene có score cao nhất chạy
3. Lock player + pause TimeManager + pause NPC behavior
4. Foreach action → `action.Run(callback)` đợi xong → next
5. Hết cutscene → fire `onCutsceneEnd`, mark `Cutscene_<id>_played` trong Blackboard
6. Resume

### CutsceneAction (abstract base)

Subclass:

| Action | Tác dụng |
|---|---|
| `SpeechAction` | Push DialogueLine vào DialogueManager, đợi `onDialogueEnd` |
| `ActorAction` | Move 1 NPC tới vị trí, hoặc thay animation |
| `SceneChangeAction` | Gọi `SceneTransitionManager.SwitchLocation` |
| `UnlockCharacterAction` | Set blackboard `Character_<name>_unlocked = true` |
| (custom) | Quyền có thể subclass thêm: PlaySFX, FadeMusic, ... |

### SoCollection<CutsceneAction>

Generic SO collection từ `NullTale/SoCollection`. Cho phép field `SoCollection<T>` trong `Cutscene.cs` mà Unity vẫn serialise đúng list of polymorphic SO.

→ Đây là package phải add vào `Packages/manifest.json`:
```
"www.nulltale.socollection": "https://github.com/NullTale/SoCollection.git"
```

Nếu không có → compile error syntax `SoCollection<...>` (đã gặp bug này khi import lần đầu, đã fix).

## Cutscene → Dialogue → Action chain

Ví dụ cutscene "Quyền gặp đại đội trưởng lần đầu":

```
Cutscene.action = [
  ActorAction(Captain, walkTo, doorPos),
  SpeechAction(Captain, "Chào em..."),
  SpeechAction(Player, "Em xin chào thủ trưởng"),
  SpeechAction(Captain, "Ngày mai 5h tập trung sân lớn"),
  UnlockCharacterAction(Captain),  // mở dialogue thường ngày
]
Cutscene.conditions = [
  BlackboardCondition("FirstDay", true),
  BlackboardCondition("Cutscene_meet_captain_played", false),
]
```

→ Cutscene chỉ chạy 1 lần ở Day 1 location đại đội.

## Cho ĐATN

Dialogue + Cutscene rất relevant cho ĐATN quân đội:
- Cutscene "ngày 1 nhập trại" — recurring=false, BlackboardCondition `Day=1`
- Cutscene "đại đội trưởng giao nhiệm vụ" cho mỗi sáng — recurring=true với điều kiện hour=06:00
- DialogueManager type-out hiệu ứng làm UI quân đội cảm xúc hơn

> [!info] Sentis chat khác Dialogue ở đâu?
> [[systems/sentis-chat]] (Phase A AI) là *dynamic* chat — player tự gõ câu hỏi, AI predict intent, chọn template response. DialogueManager (DATN) là *scripted* — dev viết tay từng line. Hai system orthogonal: cùng UI panel, khác producer của line. Quyền sẽ cần wrapper "khi nào dùng Sentis vs scripted Dialogue".

## Liên quan

- [[systems/datn-blackboard-save]] — condition check + history flag
- [[systems/datn-scene-locations]] — onLocationLoad trigger cutscene
- [[systems/datn-npc-festivals]] — UnlockCharacterAction
- [[systems/sentis-chat]] (orthogonal) — AI dynamic chat song song với scripted

---
## Backlinks
- [[sources/datn-game-repo]]
- [[technical/datn-architecture]]
- [[index]]

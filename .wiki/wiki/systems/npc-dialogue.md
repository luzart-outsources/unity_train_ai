---
title: NPC Dialogue System
category: systems
tags: [npc, dialogue, onnx, sentis, intent]
sources: [docs/superpowers/specs/2026-05-12-trainai-gdd-tech-design.md, AI_Training/deliverables/intent_classifier_meta.json, AI_Training/deliverables/responses.json, AI_Training/deliverables/NPCDialogueBrain.cs]
created: 2026-05-12
updated: 2026-05-12
---

# NPC Dialogue System

Vietnamese intent classifier (LSTM ONNX) → response template filler. Per [[claims#c-20260512-07]] + [[claims#c-20260512-09]].

## Pipeline

```
UIDialogue.Send(input)
  → DialogueRequestMsg{npc, input}
DialogueService.Reply(npc, input)
  → npc.dialogueStrategy.Reply(input, ctx)            // Strategy SO
      ↓
      tokens = VietnameseTokenizer.Encode(input, maxLen=32)
      result = IntentClassifier.Predict(tokens)
      intent = (result.score < threshold) ? OUT_OF_SCOPE : result.intent
      filled = ResponseFiller.Fill(intent, ctx)
      ↓
UIDialogue.Append("[NPC]: " + filled)
```

## Components

### VietnameseTokenizer (plain C#)

Loads `vocab` from `intent_classifier_meta.json`. Word split: longest-match against vocab keys (some vocab entries are multi-word, e.g., `"báo cáo"`, `"thủ trưởng"`). Unknown token → `<unk>=1`. Pad → `<pad>=0`.

### IntentClassifier

```csharp
public IntentResult Predict(int[] tokenIds) {
    using var input = new Tensor<int>(new TensorShape(1, MaxLen), tokenIds);
    _worker.Schedule(input);
    using var output = (Tensor<float>) _worker.PeekOutput();
    var probs = Softmax(output.DownloadToArray());
    return new IntentResult { intent = ArgMax(probs), score = probs.Max(), top2 = TopK(probs, 2) };
}
```

8 intents (per [[claims#c-20260512-09]]): `HOI_LICH`, `HOI_GIO_AN`, `HOI_VI_TRI`, `HOI_KIEN_THUC`, `BAO_CAO`, `XIN_PHEP`, `TAM_BIET`, `OUT_OF_SCOPE`.

### ResponseFiller

Loads `responses.json` (TextAsset). Template strings contain placeholders:

| Placeholder | Source |
|---|---|
| `{scheduled_today}` | `QuestRouter.GetTodaySummary()` |
| `{meal_time}` | `TimeConfigSO` |
| `{minutes_to_meal}` | `GameClock.MinutesUntil(mealTime)` |
| `{place}`, `{direction}`, `{distance}`, `{block}` | `AreaDB.NearestTo(player)` + heuristic |
| `{topic}`, `{chapter}`, `{summary}` | parsed from input (best-effort) or `"điều đó"` fallback |
| `{hocTap}`, `{renLuyen}` | `PlayerStateRSO` |

Picks random template per intent.

## Strategy SO

```csharp
[CreateAssetMenu(menuName="Game/Dialogue/Onnx")]
public class OnnxDialogueSO : DialogueStrategySO {
    [SerializeField, Range(0,1)] float confidenceThreshold = 0.5f;
    [SerializeField] IntentId fallback = IntentId.OUT_OF_SCOPE;
    public override async UniTask<string> Reply(string input, NpcContext ctx) { ... }
}

public class CannedDialogueSO : DialogueStrategySO {
    [SerializeField] List<string> lines;
    public override UniTask<string> Reply(string _, NpcContext __)
        => UniTask.FromResult(lines[Random.Range(0, lines.Count)]);
}
```

## Confidence threshold

Default `0.5`. If classifier score < threshold → fallback intent (default `OUT_OF_SCOPE`) → response like "Tôi không hiểu rõ. Đồng chí hỏi lại đi." See [[claims#c-20260512-10]] for template format.

## Known risk

> [!warning] Verify generalization
> Prior LSTM (different training set) was 1/8 correct on real-world inputs (handoff v2). Current `intent_classifier.onnx` (val_acc 98.44%) claims improvement; verify in Phase 5 with real Vietnamese player inputs. See [[open-questions#q-20260512-04]].

## Backlinks
- [[entities/npc-commander]]
- [[systems/sentis-runtime]]
- [[decisions/d03-strategy-swap-ai]]
- [[sources/intent-classifier-meta]]
- [[sources/responses-json]]
